using Newtonsoft.Json.Linq;
using ProjectXServer.Database;
using ProjectXServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectXServer.NetActions
{
    internal class UpgradeLightTowerCommand : ICommand
    {
        public async void Execute(ServerMessage message)
        {
            Console.WriteLine("[ACTION] Executing UpgradeLightTower");
            Console.WriteLine($"[ACTION] Executed by: {message.Client.Account.Id}");

            LightTower towerToUpgrade = message.Client.Player.unlockedLightTowers.FirstOrDefault(tower => tower.TowerNum == Convert.ToInt32(message.Parameters["towerNum"]));

            Dictionary<string, object> responseData = new Dictionary<string, object>() {
                        { "action", "UpgradeLightTower" },
                        { "status", "BAD" }
            };

            if (towerToUpgrade != null)
            {
                int price = (int)(towerToUpgrade.BaseAmount * towerToUpgrade.Multiplier * towerToUpgrade.TowerNum);
                if ((message.Client.Player.LightPoints - price) >= 0)
                {
                    message.Client.Player.LightPoints -= price;
                    await DB.SavePlayerData(message.Client.Player);
                    towerToUpgrade.Multiplier += 0.05f;
                    await DB.SaveTowerData(message.Client.Player);

                    Dictionary<string, object> paramsDict = new Dictionary<string, object>() {
                        { "multiplier", towerToUpgrade.Multiplier }
                    };

                    responseData = new Dictionary<string, object>() {
                        { "action", "UpgradeLightTower" },
                        { "status", "OK" },
                        { "params", paramsDict }
                    };
                }
            }

            Packet responsePacket = new Packet((byte)PacketType.ActionResult, JObject.FromObject(responseData));
            responsePacket.Send(message.Client.Socket);
        }
    }
}
