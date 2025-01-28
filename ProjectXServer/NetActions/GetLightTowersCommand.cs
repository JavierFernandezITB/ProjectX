using ProjectXServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectXServer.Database;

namespace ProjectXServer.NetActions
{
    internal class GetLightTowersCommand : ICommand
    {
        public async void Execute(ServerMessage message)
        {
            Console.WriteLine("[ACTION] Executing GetLightTowers");
            Console.WriteLine($"[ACTION] Executed by: {message.Client.Account.Id}");
            Console.WriteLine($"[ACTION] Parameters: {string.Join(", ", message.Parameters)}");
            message.Client.Player.unlockedLightTowers = await DB.GetLightTowersByPlayer(message.Client.Player.Id);
            string dataString = "";
            foreach (LightTower tower in message.Client.Player.unlockedLightTowers)
            {
                dataString += $"{tower.TowerNum}|{tower.InitDate}|{tower.Multiplier}|{tower.BaseAmount}_";
            }
            Packet responsePacket = new Packet((byte)PacketType.ActionResult, dataString);
            responsePacket.Send(message.Client.Socket);
        }
    }
}
