using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ProjectXServer.Utils;

namespace ProjectXServer
{
    internal class Client
    {
        public static void StartClient()
        { 
            TcpClient serverSocket = new TcpClient("127.0.0.1", Globals.port);

            string user = "test1";
            string password = "test";
            string email = "test@gmail.com";
            string authToken = "PXAT_3T7FOK62B32DPP5M8L409EBSU4M9H33M0P8834MS6V22K6WE5PW3T1FR6CBXH7Z2";

            Console.WriteLine("[CLIENT] Client connected to server. Sending auth packet.");
            //Packet authPacket = new Packet((byte)PacketType.Auth, $"REGISTER {user} {password} {email}"); // Register user.
            //Packet authPacket = new Packet((byte)PacketType.Auth, $"LOGIN {user} {password}"); // Login user with credentials.
            Packet authPacket = new Packet((byte)PacketType.Auth, $"TLOGIN {authToken}"); // Login user with auth token.
            authPacket.Send(serverSocket);
            Console.WriteLine("[CLIENT] Sent! Waiting for response...");
            Packet authPacketResponse = Packet.Receive(serverSocket);
            string[] response = authPacketResponse.Data.Split(" ");
            if (response[0] == "OK")
                Console.WriteLine($"[CLIENT] Successfully logged in as ID: {response[1]} with NAME: {response[2]}");
        }
    }
}
