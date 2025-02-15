using ProjectXServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectXServer.Database;
using Newtonsoft.Json.Linq;

namespace ProjectXServer.NetActions
{
    internal class GetLightTowersCommand : ICommand
    {
        public async void Execute(ServerMessage message)
        {
            Console.WriteLine("[ACTION] Executing GetLightTowers");
            Console.WriteLine($"[ACTION] Executed by: {message.Client.Account.Id}");

            Dictionary<string, object> paramsDict = new Dictionary<string, object>() {
                { "towersDataDict", new List<Dictionary<string, string>>() }
            };

            Dictionary<string, object> responseData = new Dictionary<string, object>() {
                { "action", "GetLightTowers" },
                { "params", paramsDict }
            };

            message.Client.Player.unlockedLightTowers = await DB.GetLightTowersByPlayer(message.Client.Player.Id);
    
            if (paramsDict.TryGetValue("towersDataDict", out object responseTowersDataDict) && responseTowersDataDict is List<Dictionary<string, string>> responseTowersDataListDict)
            {
                foreach (LightTower tower in message.Client.Player.unlockedLightTowers)
                {
                    Dictionary<string, string> towerDataDict = new Dictionary<string, string>() {
                        { "towerNum", tower.TowerNum.ToString() },
                        { "initDate", tower.InitDate.ToString() },
                        { "multiplier", tower.Multiplier.ToString() },
                        { "baseAmount", tower.BaseAmount.ToString() }
                    };
                    responseTowersDataListDict.Add(towerDataDict);
                }
            }
            Packet responsePacket = new Packet((byte)PacketType.ActionResult, JObject.FromObject(responseData));
            responsePacket.Send(message.Client.Socket);
        }
    }
}
