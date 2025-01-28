using ProjectXServer.Utils;
using ProjectXServer.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectXServer.NetActions
{
    internal class CollectLightTowersCommand : ICommand
    {
        public async void Execute(ServerMessage message)
        {
            Console.WriteLine("[ACTION] Executing CollectLightTowers");
            Console.WriteLine($"[ACTION] Executed by: {message.Client.Account.Id}");
            Console.WriteLine($"[ACTION] Parameters: {string.Join(", ", message.Parameters)}");

            int towerId = int.Parse(message.Parameters[0]);
            LightTower playerLightTowerObject = message.Client.Player.unlockedLightTowers.FirstOrDefault(tower => tower.TowerNum == towerId);
            if (playerLightTowerObject != null)
            {
                TimeSpan elapsedTime = DateTime.Now - playerLightTowerObject.InitDate;
                int reward = (int)(elapsedTime.TotalMinutes * (playerLightTowerObject.BaseAmount * playerLightTowerObject.Multiplier));
                Console.WriteLine(reward);
                playerLightTowerObject.InitDate = DateTime.Now;
                message.Client.Player.LightPoints += reward;
            }
            await DB.SaveTowerData(message.Client.Player);
            Packet responsePacket = new Packet((byte)PacketType.ActionResult, $"{playerLightTowerObject.InitDate}");
            responsePacket.Send(message.Client.Socket);
        }
    }
}
