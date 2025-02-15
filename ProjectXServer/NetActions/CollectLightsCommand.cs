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
    internal class CollectLightsCommand : ICommand
    {
        public void Execute(ServerMessage message)
        {
            Console.WriteLine("[ACTION] Executing CollectLights");
            Console.WriteLine($"[ACTION] Executed by: {message.Client.Account.Id}");

            Vector3 mousePosition = new Vector3(
                Convert.ToSingle(message.Parameters["mousePosX"]),
                Convert.ToSingle(message.Parameters["mousePosY"]),
                Convert.ToSingle(message.Parameters["mousePosZ"])
            );

            List<CollectableLight> collectedLights = new List<CollectableLight>();

            // ESTO ES PARA QUE EL CSHARP NO HAGA KABOOM CUANDO LE DECIMOS QUE EN VERDAD UN OBJETO ES UNA LISTA >:(
            if (message.Parameters.TryGetValue("uuidList", out object paramUuids))
            {
                List<string> paramUuidsList = (paramUuids as JArray)?.ToObject<List<string>>();
                foreach (string lightUuid in paramUuidsList)
                {
                    Console.WriteLine(lightUuid);
                    // Parse the UUID from the string
                    if (Guid.TryParse(lightUuid, out Guid uuid))
                    {
                        // Find the CollectableLight in the player's collectableLights list
                        CollectableLight collectableLight = message.Client.Player.collectableLights
                            .FirstOrDefault(light => light.UUID == uuid);

                        if (collectableLight != null)
                        {
                            float distance = Vector3.Distance(collectableLight.Position, mousePosition);

                            if (distance <= 5f)
                            {
                                Console.WriteLine($"CollectableLight at {collectableLight.Position} is within range and can be picked up.");

                                message.Client.Player.LightPoints += collectableLight.Reward;
                                message.Client.Player.collectableLights.Remove(collectableLight);
                                collectedLights.Add(collectableLight);
                            }
                        }
                    }
                }
            }

            List<string> collectedUuids = new List<string>();

            if (collectedLights.Count > 0)
            {
                foreach (CollectableLight light in collectedLights)
                {
                    collectedUuids.Add(light.UUID.ToString());
                }
            }
            else
            {
                collectedUuids.Add("NONE");
            }

            Dictionary<string, object> paramsDict = new Dictionary<string, object>
            {
                { "uuidsList", collectedUuids }
            };


            Dictionary<string, object> responseData = new Dictionary<string, object>() {
                { "action", "CollectLights" },
                { "params", paramsDict }
            }; 

            Packet responsePacket = new Packet((byte)PacketType.ActionResult, JObject.FromObject(responseData));
            responsePacket.Send(message.Client.Socket);
        }
    }
}
