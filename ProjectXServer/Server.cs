using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ProjectXServer.Database;
using ProjectXServer.Utils;

namespace ProjectXServer
{
    internal class Server
    {
        public static TcpListener serverSocket;
        public static List<TcpClient> connectedClients = new List<TcpClient>();
        public static bool isCheckingForConnections = false;

        public static void StartServer()
        {
            serverSocket = new TcpListener(Globals.address, Globals.port);
            serverSocket.Start();
            Console.WriteLine("[SERVER] Server started listening at port {0}.", Globals.port);

            // Await for connection...
            AcceptConnections();
        }

        private static void AcceptConnections()
        {
            while (true)
            {
                if (connectedClients.Count < Globals.maxPlayers && isCheckingForConnections == false)
                {
                    serverSocket.BeginAcceptTcpClient(new AsyncCallback(HandleIncomingConnection), null);
                    isCheckingForConnections = true;
                }
            }
        }

        private static void HandleIncomingConnection(IAsyncResult ar)
        { 
            TcpClient client = serverSocket.EndAcceptTcpClient(ar);
            isCheckingForConnections = false;
            Console.WriteLine("[SERVER] Got incoming connection.");
            connectedClients.Add(client);
            HandleAuthenticationStep(client);
        }

        private static void HandleAuthenticationStep(TcpClient client)
        {
            Console.WriteLine("[SERVER] Waiting for auth packet...");
            Packet authPacket = Packet.Receive(client);
            Console.WriteLine("[SERVER] Got auth packet!");

            if (authPacket.PacketId == (byte)PacketType.Auth && authPacket.Data.StartsWith("REGISTER"))
            {
                string[] parsedData = authPacket.Data.Split(" ");
                string username = parsedData[1];
                string hashedpass = parsedData[2];
                string email = parsedData[3];

                bool registerSuccessful = DB.RegisterPlayer(username, hashedpass, email);

                if (registerSuccessful)
                {
                    Console.WriteLine("[SERVER] Register successful! Sending response.");
                    Packet responsePacket = new Packet((byte)PacketType.Auth, $"REGISTER OK");
                    responsePacket.Send(client);
                }
                else
                {
                    Console.WriteLine("[SERVER] Register failed. Sending response.");
                    Packet responsePacket = new Packet((byte)PacketType.Auth, $"REGISTER BAD");
                    responsePacket.Send(client);
                }
            }
            else if (authPacket.PacketId == (byte)PacketType.Auth && authPacket.Data.StartsWith("LOGIN"))
            {
                string[] parsedData = authPacket.Data.Split(" ");
                string username = parsedData[1];
                string hashedpass = parsedData[2];

                (string authToken, Player playerData) = DB.LoginPlayer(username, hashedpass);

                if (authToken != null && playerData != null)
                {
                    Console.WriteLine("[SERVER] Login successful! Sending response.");
                    Packet responsePacket = new Packet((byte)PacketType.Auth, $"LOGIN OK {authToken} {playerData.playerId} {playerData.username}");
                    responsePacket.Send(client);
                }
                else
                {
                    Console.WriteLine("[SERVER] Login failed. Sending response.");
                    Packet responsePacket = new Packet((byte)PacketType.Auth, $"LOGIN BAD");
                    responsePacket.Send(client);
                }
            }
            else if (authPacket.PacketId == (byte)PacketType.Auth && authPacket.Data.StartsWith("TLOGIN"))
            {
                string[] parsedData = authPacket.Data.Split(" ");
                string loginToken = parsedData[1];

                Player playerData = DB.LoginWithAuthToken(loginToken);

                if (playerData != null)
                {
                    Console.WriteLine("[SERVER] Token Login successful! Sending response.");
                    Packet responsePacket = new Packet((byte)PacketType.Auth, $"TLOGIN OK {playerData.playerId} {playerData.username}");
                    responsePacket.Send(client);
                }
                else
                {
                    Console.WriteLine("[SERVER] Token Login failed. Sending response.");
                    Packet responsePacket = new Packet((byte)PacketType.Auth, $"TLOGIN BAD");
                    responsePacket.Send(client);
                }
            }
        }
    }
}
