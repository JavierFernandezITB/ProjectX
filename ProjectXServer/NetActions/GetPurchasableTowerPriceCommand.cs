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
    internal class GetPurchasableTowerPriceCommand : ICommand
    {
        public List<LightTower> purchasableLightTowers = new List<LightTower>();

        public async void Execute(ServerMessage message)
        {
            Console.WriteLine("[ACTION] Executing GetPurchasableTowerPrice");
            Console.WriteLine($"[ACTION] Executed by: {message.Client.Account.Id}");

            purchasableLightTowers = await DB.GetPurchasableLightTowers();

            int requestedPriceTowerNum = Convert.ToInt32(message.Parameters["towerNum"]);

            LightTower purchasableTowerObject = purchasableLightTowers.FirstOrDefault(tower => tower.TowerNum == requestedPriceTowerNum);

            if (purchasableLightTowers != null)
            {
                int price = (int)(purchasableTowerObject.BaseAmount * purchasableTowerObject.Multiplier * purchasableTowerObject.TowerNum);
                Dictionary<string, object> paramsDict = new Dictionary<string, object>() {
                    { "towerPrice", price }
                };

                Dictionary<string, object> responseData = new Dictionary<string, object>() {
                    { "action", "GetPurchasableTowerPrice" },
                    { "params", paramsDict }
                };

                Packet responsePacket = new Packet((byte)PacketType.ActionResult, JObject.FromObject(responseData));
                responsePacket.Send(message.Client.Socket);
            }
        }
    }
}
