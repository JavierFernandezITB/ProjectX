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
            Console.WriteLine($"[ACTION] Parameters: {string.Join(", ", message.Parameters)}");
            string[] position = message.Parameters[0].Split("|");
            Vector3 mousePosition = new Vector3(float.Parse(position[0]), float.Parse(position[1]), float.Parse(position[2]));

            List<CollectableLight> collectedLights = new List<CollectableLight>();

            foreach (string lightUuid in message.Parameters.Skip(1))
            {

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
                        else
                        {
                            //Console.WriteLine($"CollectableLight at {collectableLight.Position} is too far to pick up.");
                        }
                    }
                    else
                    {
                        //Console.WriteLine($"CollectableLight with UUID {uuid} not found.");
                    }
                }
                else
                {
                    //Console.WriteLine($"Invalid UUID format: {lightUuid}");
                }
            }

            string soloDataPack = "";
            if (collectedLights.Count > 0)
            {
                foreach (CollectableLight light in collectedLights)
                {
                    Console.WriteLine(soloDataPack);
                    soloDataPack += light.UUID.ToString() + " ";
                }
            }
            else
                soloDataPack = "NONE";

            Console.WriteLine("LOOLOLOLLOL:" + soloDataPack);
            Packet responsePacket = new Packet((byte)PacketType.ActionResult, $"{soloDataPack}");
            responsePacket.Send(message.Client.Socket);
        }
    }
}
