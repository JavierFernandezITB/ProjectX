using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ProjectXServer.Database;
using ProjectXServer.NetActions;
using ProjectXServer.Utils;

namespace ProjectXServer
{
    internal class Server
    {
        public static TcpListener serverSocket;
        public static List<connectedClient> connectedClients = new List<connectedClient>();
        public static bool isCheckingForConnections = false;
        public static Dictionary<string, NetActions.ICommand> messageHandlers;
        private static Timer tickTimer;

        public static void StartServer()
        {
            messageHandlers = new Dictionary<string, NetActions.ICommand>
            {
                { "GetPlayerData", new NetActions.GetPlayerDataCommand() },
                { "GetPlayerLights", new NetActions.GetPlayerLightsCommand() },
                { "CollectLights", new NetActions.CollectLightsCommand() },
                { "GetLightTowers", new NetActions.GetLightTowersCommand() },
                { "CollectLightTowers", new NetActions.CollectLightTowersCommand() },
            };

            serverSocket = new TcpListener(Globals.address, Globals.port);
            serverSocket.Start();
            Console.WriteLine("[SERVER] Server started listening at port {0}.", Globals.port);

            tickTimer = new Timer(TickLogic, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

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
                    HandleAuthenticationStep(client);
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

                (string authToken, Account accountData) = DB.LoginPlayer(username, hashedpass);

                if (authToken != null && accountData != null)
                {
                    Console.WriteLine("[SERVER] Login successful! Sending response.");
                    Packet responsePacket = new Packet((byte)PacketType.Auth, $"LOGIN OK {authToken} {accountData.Id} {accountData.Username}");
                    responsePacket.Send(client);

                    Player playerData = DB.GetPlayerData(accountData.Id);

                    Thread netThread = new Thread(() => MainNetworkLoop(client, accountData, playerData));
                    netThread.Start();
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

                Account accountData = DB.LoginWithAuthToken(loginToken);

                if (accountData != null)
                {
                    Console.WriteLine("[SERVER] Token Login successful! Sending response.");
                    Packet responsePacket = new Packet((byte)PacketType.Auth, $"TLOGIN OK 0 {accountData.Id} {accountData.Username}");
                    responsePacket.Send(client);

                    Player playerData = DB.GetPlayerData(accountData.Id);

                    Thread netThread = new Thread(() => MainNetworkLoop(client, accountData, playerData));
                    netThread.Start();
                }
                else
                {
                    Console.WriteLine("[SERVER] Token Login failed. Sending response.");
                    Packet responsePacket = new Packet((byte)PacketType.Auth, $"TLOGIN BAD");
                    responsePacket.Send(client);
                }
            }
        }

        private static void MainNetworkLoop(TcpClient socket, Account localAccount, Player localPlayer)
        {
            connectedClient connectedClient = new connectedClient();
            connectedClient.Socket = socket;
            connectedClient.Player = localPlayer;
            connectedClient.Account = localAccount;
            connectedClients.Add(connectedClient);

            while (socket.Connected)
            {
                Console.WriteLine($"[{localAccount.Id}] Waiting for client requests...");
                Packet received = Packet.Receive(socket);
                string[] parsedData = received.Data.Split(" ");
                string action = parsedData[0];

                if (messageHandlers.TryGetValue(action, out ICommand command))
                {
                    ServerMessage message = new ServerMessage
                    {
                        Client = connectedClient,
                        Action = action,
                        Parameters = parsedData.Skip(1).ToArray()
                    };

                    command.Execute(message);
                }
                else
                {
                    Console.WriteLine($"[{localAccount.Id}] Unknown action: {action}");
                }
            }

            Console.WriteLine($"[{localAccount.Id}] Disconnected!");
        }

        private static void TickLogic(object state)
        {
            foreach (var client in connectedClients)
            {
                if (client.Socket.Connected)
                {
                    // Spawn one light for the player.
                    if (client.Player.collectableLights.Count < client.Player.MaxCollectableLights)
                    {
                        Vector3 position = new Vector3(new Random().Next(-50, 50),3,new Random().Next(-50, 50));
                        CollectableLight newCollectableLight = new CollectableLight(position);
                        client.Player.collectableLights.Add(newCollectableLight);
                    }
                }
            }
        }
    }
}
