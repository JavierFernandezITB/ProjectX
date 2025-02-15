using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProjectXServer.Database;
using ProjectXServer.NetActions;
using ProjectXServer.Utils;

namespace ProjectXServer
{
    internal class Server
    {
        public static TcpListener serverSocket;
        public static List<ConnectedClient> connectedClients = new List<ConnectedClient>();
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

        private static async void HandleAuthenticationStep(TcpClient client)
        {
            Console.WriteLine("[SERVER] Waiting for auth packet...");
            Packet authPacket = Packet.Receive(client);
            Console.WriteLine("[SERVER] Got auth packet!");

            if (authPacket.PacketId == (byte)PacketType.Auth && (string)authPacket.Data["action"] == "REGISTER")
            {
                string username = (string)authPacket.Data["username"];
                string hashedpass = (string)authPacket.Data["passwd"];
                string email = (string)authPacket.Data["email"];

                bool registerSuccessful = await DB.RegisterPlayer(username, hashedpass, email);

                if (registerSuccessful)
                {
                    Console.WriteLine("[SERVER] Register successful! Sending response.");
                    Dictionary<string, string> responseData = new Dictionary<string, string>() {
                        { "action", "REGISTER" },
                        { "response", "OK" }
                    };
                    Packet responsePacket = new Packet((byte)PacketType.Auth, JObject.FromObject(responseData));
                    responsePacket.Send(client);
                    HandleAuthenticationStep(client);
                }
                else
                {
                    Console.WriteLine("[SERVER] Register failed. Sending response.");
                    Dictionary<string, string> responseData = new Dictionary<string, string>() {
                        { "action", "REGISTER" },
                        { "response", "BAD" }
                    };
                    Packet responsePacket = new Packet((byte)PacketType.Auth, JObject.FromObject(responseData));
                    responsePacket.Send(client);
                }
            }
            else if (authPacket.PacketId == (byte)PacketType.Auth && (string)authPacket.Data["action"] == "LOGIN")
            {
                string username = (string)authPacket.Data["username"];
                string hashedpass = (string)authPacket.Data["passwd"];

                (string authToken, Account accountData) = await DB.LoginPlayer(username, hashedpass);

                if (authToken != null && accountData != null)
                {
                    Console.WriteLine("[SERVER] Login successful! Sending response.");
                    Dictionary<string, string> responseData = new Dictionary<string, string>() {
                        { "action", "LOGIN" },
                        { "status", "OK" },
                        { "token", authToken },
                        { "accountid", accountData.Id.ToString() },
                        { "username", accountData.Username.ToString() }
                    };
                    Packet responsePacket = new Packet((byte)PacketType.Auth, JObject.FromObject(responseData));
                    responsePacket.Send(client);

                    Player playerData = await DB.GetPlayerData(accountData.Id);

                    Thread netThread = new Thread(() => MainNetworkLoop(client, accountData, playerData));
                    netThread.Start();
                }
                else
                {
                    Console.WriteLine("[SERVER] Login failed. Sending response.");
                    Dictionary<string, string> responseData = new Dictionary<string, string>() {
                        { "action", "LOGIN" },
                        { "response", "BAD" }
                    };
                    Packet responsePacket = new Packet((byte)PacketType.Auth, JObject.FromObject(responseData));
                    responsePacket.Send(client);
                }
            }
            else if (authPacket.PacketId == (byte)PacketType.Auth && (string)authPacket.Data["action"] == "TLOGIN")
            {;
                string loginToken = (string)authPacket.Data["token"];

                Account accountData = await DB.LoginWithAuthToken(loginToken);

                if (accountData != null)
                {
                    Console.WriteLine("[SERVER] Token Login successful! Sending response.");
                    Dictionary<string, string> responseData = new Dictionary<string, string>()
                    {
                        { "action", "TLOGIN" },
                        { "response", "OK" },
                        { "accountid", accountData.Id.ToString() },
                        { "username", accountData.Username.ToString() }
                    };
                    Packet responsePacket = new Packet((byte)PacketType.Auth, JObject.FromObject(responseData));
                    responsePacket.Send(client);

                    Player playerData = await DB.GetPlayerData(accountData.Id);

                    Thread netThread = new Thread(() => MainNetworkLoop(client, accountData, playerData));
                    netThread.Start();
                }
                else
                {
                    Console.WriteLine("[SERVER] Token Login failed. Sending response.");
                    Dictionary<string, string> responseData = new Dictionary<string, string>() {
                        { "action", "TLOGIN" },
                        { "response", "BAD" }
                    };
                    Packet responsePacket = new Packet((byte)PacketType.Auth, JObject.FromObject(responseData));
                    responsePacket.Send(client);
                }
            }
        }

        private static void MainNetworkLoop(TcpClient socket, Account localAccount, Player localPlayer)
        {
            ConnectedClient connectedClient = new ConnectedClient();
            connectedClient.Socket = socket;
            connectedClient.Player = localPlayer;
            connectedClient.Account = localAccount;
            connectedClients.Add(connectedClient);

            while (socket.Connected)
            {
                Console.WriteLine($"[{localAccount.Id}] Waiting for client requests...");
                Packet receivedData = Packet.Receive(socket);
                string action = (string)receivedData.Data["action"];

                if (messageHandlers.TryGetValue(action, out ICommand command))
                {
                    ServerMessage message = new ServerMessage
                    {
                        Client = connectedClient,
                        Action = action,
                        Parameters = receivedData.Data["params"].ToObject<Dictionary<string, object>>()
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
                        Console.WriteLine($"Spanwed light! {client.Player.collectableLights.Count}");
                        Vector3 position = new Vector3(new Random().Next(-50, 50),-75,new Random().Next(-50, 50));
                        CollectableLight newCollectableLight = new CollectableLight(position);
                        client.Player.collectableLights.Add(newCollectableLight);
                    }
                }
            }
        }
    }
}
