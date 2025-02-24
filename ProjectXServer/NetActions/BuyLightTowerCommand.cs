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
    internal class BuyLightTowerCommand : ICommand
    {
        public List<LightTower> purchasableLightTowers = new List<LightTower>();

        public async void Execute(ServerMessage message)
        {
            Console.WriteLine("[ACTION] Executing BuyLightTower");
            Console.WriteLine($"[ACTION] Executed by: {message.Client.Account.Id}");

            purchasableLightTowers = await DB.GetPurchasableLightTowers();

            int requestedPriceTowerNum = Convert.ToInt32(message.Parameters["towerNum"]);

            LightTower purchasableTowerObject = purchasableLightTowers.FirstOrDefault(tower => tower.TowerNum == requestedPriceTowerNum);
            LightTower playerOwnedTowerObject = message.Client.Player.unlockedLightTowers.FirstOrDefault(tower => tower.TowerNum == requestedPriceTowerNum);

            Dictionary<string, object> responseData = new Dictionary<string, object>() {
                    { "action", "BuyLightTower" },
                    { "status", "BAD" }
            };

            if (purchasableTowerObject != null && playerOwnedTowerObject == null)
            {
                int price = (int)(purchasableTowerObject.BaseAmount * purchasableTowerObject.Multiplier * purchasableTowerObject.TowerNum);
                if ((message.Client.Player.LightPoints - price) >= 0)
                {
                    message.Client.Player.LightPoints -= price;
                    await DB.SavePlayerData(message.Client.Player);
                    purchasableTowerObject.PlayerId = message.Client.Player.Id;
                    purchasableTowerObject.InitDate = DateTime.Now;
                    await DB.SaveTowerData(purchasableTowerObject);

                    responseData = new Dictionary<string, object>() {
                        { "action", "BuyLightTower" },
                        { "status", "OK" },
                    };
                }
            }

            Packet responsePacket = new Packet((byte)PacketType.ActionResult, JObject.FromObject(responseData));
            responsePacket.Send(message.Client.Socket);
        }
    }
}
