#region Usings

using System;

#endregion

namespace LibNetCat
{
    public class MessageEvent : EventArgs
    {
        /// <summary>
        ///     Construit un message.
        /// </summary>
        /// <param name="msg">Le message qui a été reçu.</param>
        public MessageEvent(string msg)
        {
            Content = msg;
        }

        /// <summary>
        ///     Contenu du message.
        /// </summary>
        public string Content { get; private set; }
    }
}