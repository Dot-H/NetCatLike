#region Usings

using System;
using System.Net;
using LibNetCat;

#endregion

namespace ClientCLI
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var client = new Client(IPAddress.Parse("127.0.0.1"));
            Console.CancelKeyPress += delegate { client.Disconnect(); };
            client.MessageReceived += delegate(object sender, MessageEvent e) { Console.WriteLine(e.Content); };
            client.Disconnected += delegate
            {
                Console.WriteLine("Le serveur s'est déconnecté...");
                Environment.Exit(0);
            };
            while (true)
                client.SendMessage(Console.ReadLine());
        }
    }
}