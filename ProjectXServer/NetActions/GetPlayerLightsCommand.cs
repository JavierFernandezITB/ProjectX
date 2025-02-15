using Newtonsoft.Json.Linq;
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

            // Correct dictionary initialization
            var lightsDataList = new List<Dictionary<string, string>>();

            Dictionary<string, object> paramsDict = new Dictionary<string, object>() {
                { "lightsDataDict", lightsDataList }
            };

            Dictionary<string, object> responseData = new Dictionary<string, object>() {
                { "action", "GetPlayerLights" },
                { "params", paramsDict }
            };

            // Directly use lightsDataList instead of TryGetValue
            foreach (CollectableLight light in message.Client.Player.collectableLights)
            {
                Dictionary<string, string> lightDataDict = new Dictionary<string, string>() {
                    { "uuid", light.UUID.ToString() },
                    { "lightPosX", light.Position.X.ToString() },
                    { "lightPosY", light.Position.Y.ToString() },
                    { "lightPosZ", light.Position.Z.ToString() }
                };
                lightsDataList.Add(lightDataDict);
            }

            Packet response = new Packet((byte)PacketType.ActionResult, JObject.FromObject(responseData));
            response.Send(message.Client.Socket);
        }

    }
}
