using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

public class Packet
{
    internal enum PacketType // remember to recast type before comparing.
    {
        Auth,
        Action,
        ActionResult
    }
    public byte PacketId;
    public JObject Data;

    public Packet(byte packetId, JObject data)
    {
        PacketId = packetId;
        Data = data;
    }

    public byte[] Serialize()
    {
        using (MemoryStream memstream = new MemoryStream())
        {
            // Escribir el PacketId
            memstream.WriteByte(PacketId);

            // Serializar los datos JSON a string
            string jsonData = Data.ToString();
            byte[] dataBytes = Encoding.UTF8.GetBytes(jsonData);

            // Guardar el tama�o de los datos
            byte[] dataLength = BitConverter.GetBytes(dataBytes.Length);
            memstream.Write(dataLength, 0, dataLength.Length);

            // Escribir los datos JSON
            memstream.Write(dataBytes, 0, dataBytes.Length);

            return memstream.ToArray();
        }
    }

    public static Packet Deserialize(NetworkStream nstream)
    {
        // Obtener el PacketId
        byte packetid = (byte)nstream.ReadByte();

        // Leer el tama�o de los datos
        byte[] lengthBytes = new byte[4];
        nstream.Read(lengthBytes, 0, 4);
        int dataLength = BitConverter.ToInt32(lengthBytes, 0);

        // Leer los datos JSON
        byte[] dataBytes = new byte[dataLength];
        nstream.Read(dataBytes, 0, dataLength);
        string jsonData = Encoding.UTF8.GetString(dataBytes);

        // Convertir el string JSON a JToken (puede ser JObject o JArray)
        JObject json = JObject.Parse(jsonData);

        Debug.Log($"[PACKET] Packet deserialized: PacketID -> {packetid} | Data -> {json}");

        return new Packet(packetid, json);
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
