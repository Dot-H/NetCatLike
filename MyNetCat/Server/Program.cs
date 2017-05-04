#region Usings

using System;

#endregion

namespace Server
{
    internal class Program
    {
        private static void PrintHelp()
        {
            Console.WriteLine("Usage: \"Server Port\"");
        }

        private static void Main(string[] args)
        {
            int port;
            try
            {
                port = int.Parse(args[0]);
            }
            catch (Exception)
            {
                PrintHelp();
                return;
            }
            var serv = new Server(port);
            Console.WriteLine("Le serveur à démarré.");
            Console.CancelKeyPress += delegate { serv.Disconnect(); };
            while (true)
                serv.SendMessage(Console.ReadLine());
        }
    }
}