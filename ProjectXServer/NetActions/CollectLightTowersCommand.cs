using ProjectXServer.Utils;
using ProjectXServer.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ProjectXServer.NetActions
{
    internal class CollectLightTowersCommand : ICommand
    {
        public async void Execute(ServerMessage message)
        {
            Console.WriteLine("[ACTION] Executing CollectLightTowers");
            Console.WriteLine($"[ACTION] Executed by: {message.Client.Account.Id}");

            int towerId = (int)message.Parameters["towerId"] ;
            LightTower playerLightTowerObject = message.Client.Player.unlockedLightTowers.FirstOrDefault(tower => tower.TowerNum == towerId);
            if (playerLightTowerObject != null)
            {
                TimeSpan elapsedTime = DateTime.Now - playerLightTowerObject.InitDate;
                int reward = (int)(elapsedTime.TotalMinutes * (playerLightTowerObject.BaseAmount * playerLightTowerObject.Multiplier));
                playerLightTowerObject.InitDate = DateTime.Now;
                message.Client.Player.LightPoints += reward;
            }
            await DB.SaveTowerData(message.Client.Player);

            Dictionary<string, object> paramsDict = new Dictionary<string, object>() {
                { "serverInitDate", playerLightTowerObject.InitDate }
            };

            Dictionary<string, object> responseData = new Dictionary<string, object>() {
                { "action", "CollectLightTowers" },
                { "params", paramsDict }
            };

            Packet responsePacket = new Packet((byte)PacketType.ActionResult, JObject.FromObject(responseData));
            responsePacket.Send(message.Client.Socket);
        }
    }
}
