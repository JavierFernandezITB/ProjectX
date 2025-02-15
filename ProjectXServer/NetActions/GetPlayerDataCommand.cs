using ProjectXServer.Utils;
using ProjectXServer.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ProjectXServer.NetActions
{
    internal class GetPlayerDataCommand : ICommand
    {
        public async void Execute(ServerMessage message)
        {
            Console.WriteLine("[ACTION] Executing GetPlayerDataCommand");
            Console.WriteLine($"[ACTION] Executed by: {message.Client.Account.Id}");

            Dictionary<string, object> paramsDict = new Dictionary<string, object>() {
                { "playerId", message.Client.Player.Id },
                { "lightPoints", message.Client.Player.LightPoints },
                { "premPoints", message.Client.Player.PremPoints },
                { "masteryPoints", message.Client.Player.MasteryPoints },
                { "currentSpecialSkillCharge", message.Client.Player.CurrentSpecialSkillCharge },
                { "currentSpecialShieldCharge", message.Client.Player.CurrentSpecialShieldCharge },
            };

            Dictionary<string, object> responseData = new Dictionary<string, object>() {
                { "action", "GetPlayerData" },
                { "params", paramsDict }
            };
            
            await DB.SavePlayerData(message.Client.Player);
            Packet response = new Packet((byte)PacketType.ActionResult, JObject.FromObject(responseData));
            response.Send(message.Client.Socket);
        }
    }
}
