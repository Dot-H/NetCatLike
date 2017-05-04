#region Usings

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

#endregion

namespace LibNetCat
{
    public class Client
    {
        #region Variables Privées

        /// <summary>
        ///     Le socket représentant la connexion
        /// </summary>
        private readonly Socket _sock;

        /// <summary>
        ///     Buffer de 2 octets dans lequel on reçoit les instructions
        /// </summary>
        private byte[] _messageType;

        /// <summary>
        ///     Buffer qui nous permet de recevoir les messages
        /// </summary>
        private byte[] _content;

        #endregion

        #region Events

        /// <summary>
        ///     Event envoyé en cas de déconnection.
        /// </summary>
        public event EventHandler Disconnected;

        /// <summary>
        ///     Event envoyé en cas de message reçu.
        /// </summary>
        public event EventHandler<MessageEvent> MessageReceived;

        /// <summary>
        ///     Parle de lui même
        /// </summary>
        public event EventHandler PongEvent;

        /// <summary>
        ///     Permet de suivre la progression d'un transfert de fichier
        /// </summary>
        public event EventHandler<TransfertEvent> TransfertProgress;

        #endregion

        #region Initialization

        /// <summary>
        ///     L'initialisation systématique est ici.
        /// </summary>
        private void Initialize()
        {
            _messageType = new byte[sizeof (ushort)];

            _sock.BeginReceive(_messageType, 0, _messageType.Length, SocketFlags.None, ReceiveDatas, _sock);
        }

        /// <summary>
        ///     Constructeur appelé lorsque le Socket est déjà créé.
        /// </summary>
        /// <param name="sock">Socket utilisé pour la communication.</param>
        public Client(Socket sock)
        {
            _sock = sock;
            Initialize();
        }

        /// <summary>
        ///     Ce constructeur créé un socket et le connecte.
        /// </summary>
        /// <param name="ip">L'addresse IP de connection</param>
        /// <param name="port">Port de connection.</param>
        public Client(IPAddress ip, int port = 4242)
        {
            _sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _sock.Connect(new IPEndPoint(ip, port));
            Initialize();
        }

        #endregion

        #region Méthodes Privées

        /// <summary>
        ///     Cette méthode récupère les instructions et dispatche vers des méthodes adéquoites.
        /// </summary>
        private void ReceiveDatas(IAsyncResult ar)
        {
            if (!ar.IsCompleted || !_sock.Connected)
                return;
            try
            {
                _sock.EndReceive(ar);
                switch ((Instructions) BitConverter.ToUInt16(_messageType, 0))
                {
                    case Instructions.Ping:
                        _sock.Send(BitConverter.GetBytes((ushort) Instructions.Pong));
                        break;
                    case Instructions.BasicMessage:
                        var msgSize = new byte[sizeof (long)];
                        _sock.Receive(msgSize, 0, msgSize.Length, SocketFlags.None);
                        _content = new byte[BitConverter.ToInt64(msgSize, 0)];
                        _sock.BeginReceive(_content, 0, _content.Length, SocketFlags.None, ReceiveBasicMessage, _sock);
                        return;
                    case Instructions.FileTransfert:
                        ReceiveFile();
                        break;
                    case Instructions.Pong:
                        if (PongEvent != null)
                            PongEvent(this, EventArgs.Empty);
                        break;
                    case Instructions.Disconnect:
                        Disconnect();
                        return;
                }
                _sock.BeginReceive(_messageType, 0, _messageType.Length, SocketFlags.None, ReceiveDatas, _sock);
            }
            catch (SocketException e)
            {
                if (e.NativeErrorCode != 10054)
                    throw e;
                if (Disconnected != null)
                    Disconnected(this, EventArgs.Empty);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        /// <summary>
        ///     Cette méthode récupère un fichier et l'écrit sur le dique dur.
        /// </summary>
        private void ReceiveFile()
        {
            long bitsWritten = 0;
            var buf = new byte[2];
            _sock.Receive(buf, 0, buf.Length, SocketFlags.None);
            long size = BitConverter.ToUInt16(buf, 0);

            buf = new byte[size];
            _sock.Receive(buf, 0, buf.Length, SocketFlags.None);
            var filename = Encoding.UTF8.GetString(buf);

            var progress = new TransfertEvent(filename);
            if (TransfertProgress != null)
                TransfertProgress(this, progress);

            buf = new byte[8];
            _sock.Receive(buf, 0, buf.Length, SocketFlags.None);
            size = BitConverter.ToInt64(buf, 0);

            var bw = new BinaryWriter(File.Create(filename));
            buf = new byte[128];

            while (bitsWritten < size - buf.Length)
            {
                _sock.Receive(buf, 0, buf.Length, SocketFlags.None);
                bw.Write(buf, 0, buf.Length);
                bitsWritten += buf.Length;
                progress.Progress = bitsWritten/(double) size;
                if (TransfertProgress != null)
                    TransfertProgress(this, progress);
            }

            _sock.Receive(buf, 0, (int) (size - bitsWritten), SocketFlags.None);
            bw.Write(buf, 0, (int) (size - bitsWritten));
            progress.Progress = 1;
            progress.Done = true;
            if (TransfertProgress != null)
                TransfertProgress(this, progress);
            bw.Close();
        }

        /// <summary>
        ///     Cette méthode récupère un message.
        /// </summary>
        private void ReceiveBasicMessage(IAsyncResult ar)
        {
            if (!ar.IsCompleted)
                return;
            _sock.EndReceive(ar);
            if (MessageReceived != null)
                MessageReceived(this, new MessageEvent(Encoding.UTF8.GetString(_content)));
            _sock.BeginReceive(_messageType, 0, _messageType.Length, SocketFlags.None, ReceiveDatas, _sock);
        }

        /// <summary>
        ///     Cette méthode envoie de manière asynchrone un buffer.
        /// </summary>
        /// <param name="buf">Le buffer à envoyer</param>
        private void SendData(byte[] buf)
        {
            AsyncCallback end = delegate(IAsyncResult ar) { _sock.EndSend(ar); };
            _sock.BeginSend(buf, 0, buf.Length, SocketFlags.None, end, _sock);
        }

        #endregion

        #region Méthodes publiques

        /// <summary>
        ///     Cette méthode envoie un fichier au correspondant.
        /// </summary>
        /// <param name="filename">Nom du fichier à ouvrir.</param>
        public void SendFile(string filename)
        {
            var br = new BinaryReader(File.OpenRead(filename));
            filename = Path.GetFileName(filename);
            _sock.Send(BitConverter.GetBytes((ushort) Instructions.FileTransfert));
            var buf = Encoding.UTF8.GetBytes(filename);
            _sock.Send(BitConverter.GetBytes((ushort) buf.Length));
            _sock.Send(buf);

            var size = br.BaseStream.Length;
            _sock.Send(BitConverter.GetBytes(size));

            var progress = new TransfertEvent(filename, false);
            if (TransfertProgress != null)
                TransfertProgress(this, progress);

            while (br.BaseStream.Position < size - 128)
            {
                buf = br.ReadBytes(128);
                _sock.Send(buf);
                progress.Progress = br.BaseStream.Position/(double) size;
                if (TransfertProgress != null)
                    TransfertProgress(this, progress);
            }
            buf = br.ReadBytes((int) (size - br.BaseStream.Position));
            _sock.Send(buf);
            progress.Done = true;
            if (TransfertProgress != null)
                TransfertProgress(this, progress);
            br.Close();
        }

        /// <summary>
        ///     Cette méthode déconnecte le client.
        /// </summary>
        public void Disconnect()
        {
            if (_sock.Connected)
            {
                _sock.Send(BitConverter.GetBytes((ushort) Instructions.Disconnect));
                _sock.Shutdown(SocketShutdown.Both);
            }
            _sock.Close(1);
            if (Disconnected != null)
                Disconnected(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Envoie un message.
        /// </summary>
        /// <param name="msg">Message à envoyer</param>
        public void SendMessage(string msg)
        {
            if (msg == null)
                return;
            SendData(BitConverter.GetBytes((ushort) Instructions.BasicMessage));
            SendData(BitConverter.GetBytes((long) msg.Length));
            SendData(Encoding.UTF8.GetBytes(msg));
        }

        /// <summary>
        ///     Envoie une requette Ping.
        /// </summary>
        public void Ping()
        {
            SendData(BitConverter.GetBytes((ushort) Instructions.Ping));
        }

        #endregion
    }
}