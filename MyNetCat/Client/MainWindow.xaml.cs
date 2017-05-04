#region Usings

using System;
using System.ComponentModel;
using System.Net;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using LibNetCat;
using Microsoft.Win32;

#endregion

namespace Client
{
    /// <summary>
    ///     Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly LibNetCat.Client _client;
        private readonly Transfert _transfert;

        public MainWindow()
        {
            var p = new Connexion();
            p.ShowDialog();
            var serv = IPAddress.Parse(p.Addr.Text);
            var port = int.Parse(p.Port.Text);
            _client = new LibNetCat.Client(serv, port);
            _client.Disconnected += _client_Disconnected;
            _client.MessageReceived += ClientOnMessageReceived;
            _client.PongEvent += ClientOnPongEvent;
            _client.TransfertProgress += ClientOnTransfertProgress;
            InitializeComponent();
            _transfert = new Transfert();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }

        private void ClientOnTransfertProgress(object sender, TransfertEvent transfertEvent)
        {
            Dispatcher.Invoke(() =>
            {
                if (transfertEvent.Done)
                {
                    _transfert.Hide();
                    return;
                }
                _transfert.ProgressBar.Value = transfertEvent.Progress;
                if (!_transfert.IsActive)
                    _transfert.Show();
            });
        }

        private void ClientOnPongEvent(object sender, EventArgs eventArgs)
        {
            WriteInfo("Pong !");
        }

        private void ClientOnMessageReceived(object sender, MessageEvent messageEvent)
        {
            WriteLine(messageEvent.Content);
        }

        private void _client_Disconnected(object sender, EventArgs e)
        {
            WriteInfo("Disconnected...");
        }

        private void WriteLine(string str)
        {
            Dispatcher.Invoke(() => { Terminal.Inlines.Add($"{str}\n"); });
        }

        private void WriteInfo(string str)
        {
            Dispatcher.Invoke(() => { Terminal.Inlines.Add(new Run($"{str}\n") {Foreground = Brushes.Gray}); });
        }

        private void Ping(object sender, RoutedEventArgs e)
        {
            _client.Ping();
        }


        private void Send(object sender, RoutedEventArgs e)
        {
            _client.SendMessage(Input.Text);
            Input.Text = "";
        }

        private void Input_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Send(sender, e);
        }

        private void SendFile(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();

            // Display OpenFileDialog by calling ShowDialog method 
            var result = dlg.ShowDialog();

            // Get the selected file _name and display in a TextBox 
            if (result == true)
            {
                var worker = new BackgroundWorker();
                worker.DoWork += (o, args) => _client.SendFile(dlg.FileName);
                worker.RunWorkerAsync();
            }
        }
    }
}