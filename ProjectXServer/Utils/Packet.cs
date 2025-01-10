using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Wasm;
using System.Text;
using System.Threading.Tasks;

namespace ProjectXServer.Utils
{

    internal enum PacketType // remember to recast type before comparing.
    { 
        Auth,
    }

    internal class Packet
    {
        public byte PacketId;
        public string Data;

        public Packet(byte packetId, string data) 
        {
            PacketId = packetId;
            Data = data;
        }

        public byte[] Serialize()
        {
            using (MemoryStream memstream = new MemoryStream())
            { 
                // write the packet id
                memstream.WriteByte(PacketId);

                // save the packet data size.
                byte[] dataLength = BitConverter.GetBytes(Data.Length);
                memstream.Write(dataLength, 0, dataLength.Length);

                // save the data as an array of bytes.
                byte[] dataArray = Encoding.UTF8.GetBytes(Data);
                memstream.Write(dataArray, 0, dataArray.Length);

                return memstream.ToArray();
            }
        }

        public static Packet Deserialize(NetworkStream nstream)
        {
            // get the packet id.
            byte packetid = (byte)nstream.ReadByte();

            // get the received packet data length.
            byte[] lengthBytes = new byte[4];
            nstream.Read(lengthBytes, 0, 4);
            int dataLength = BitConverter.ToInt32(lengthBytes, 0);

            // get the packet data.
            byte[] dataBytes = new byte[dataLength];
            nstream.Read(dataBytes, 0, dataLength);
            string data = Encoding.UTF8.GetString(dataBytes);

            Console.WriteLine("[PACKET] Packet deserialized: PacketID -> {0} | Data -> {1}", packetid, data);

            return new Packet(packetid, data);
        }

        public void Send(TcpClient receiver)
        {
            NetworkStream stream = receiver.GetStream();

            byte[] packetData = Serialize();
            stream.Write(packetData, 0, packetData.Length);
        }

        public static Packet Receive(TcpClient sender)
        {
            NetworkStream stream = sender.GetStream();

            return Deserialize(stream);
        }
    }
}
