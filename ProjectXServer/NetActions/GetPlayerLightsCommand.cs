using ProjectXServer.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ProjectXServer.NetActions
{
    internal class GetPlayerLightsCommand : ICommand
    {
        public void Execute(ServerMessage message)
        {
            Console.WriteLine("[ACTION] Executing GetPlayerLights");
            Console.WriteLine($"[ACTION] Executed by: {message.Client.Account.Id}");
            Console.WriteLine($"[ACTION] Parameters: {string.Join(", ", message.Parameters)}");
            string lightsData = string.Empty;
            foreach (CollectableLight light in message.Client.Player.collectableLights)
            {
                lightsData += $"{light.UUID}|{light.Position.X}|{light.Position.Y}|{light.Position.Z} ";
            }
            if (lightsData.Length <= 0)
                lightsData = "EMPTY";
            Console.WriteLine(lightsData);
            Packet response = new Packet((byte)PacketType.ActionResult, lightsData);
            response.Send(message.Client.Socket);
        }
    }
}
