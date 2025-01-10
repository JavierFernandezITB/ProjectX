using System;


namespace ProjectXServer
{
    internal class Program 
    {
        public static bool emulateClients = false;
        public static int emulatedClients = 4;

        public static void Main(string[] args)
        {
            Thread serverThread = new Thread(Server.StartServer);
            serverThread.Start();
            if (emulateClients)
            {
                for (int i = 0; i < emulatedClients; i++)
                {
                    Thread clientThread = new Thread(Client.StartClient);
                    clientThread.Start();
                }
            }
        }
    }
}