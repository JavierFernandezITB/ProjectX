using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UIElements;

public class EntityService : ServicesReferences
{
    public GameObject lightPrefab;
    public List<CollectableLight> spawnedLights = new List<CollectableLight>();

    private void OnEnable()
    {
        networkService.LightReceived += OnLightReceived;
        touchManagerService.CollectLight += OnLightCollected;
    }

    private void OnDisable()
    {
        networkService.LightReceived -= OnLightReceived;
        touchManagerService.CollectLight -= OnLightCollected;
    }

    private void OnLightReceived(Dictionary<string, string> lightData)
    {
        string lightUuid = lightData["uuid"];

        bool lightExists = spawnedLights.Exists(l =>
        {
            return l.UUID == Guid.Parse(lightUuid);
        });

        if (!lightExists)
        {
            CollectableLight light = new CollectableLight();
            light.UUID = Guid.Parse(lightUuid);

            GameObject lightInstance = Instantiate(lightPrefab);
            Vector3 lightPosition = new Vector3(float.Parse(lightData["lightPosX"]), float.Parse(lightData["lightPosY"]), float.Parse(lightData["lightPosZ"]));
            lightInstance.transform.position = lightPosition;
            light.lightGameObject = lightInstance;

            spawnedLights.Add(light);
        }
    }

    private void OnLightCollected(List<CollectableLight> lightsToCollect)
    {
        Packet collectionPacketResponse = Packet.Receive(networkService.localClient.serverSocket);
        Dictionary<string, object> responseParams = collectionPacketResponse.Data["params"].ToObject<Dictionary<string, object>>();
        List<string> uuidsList = responseParams["uuidsList"].ConvertTo<List<string>>();

        if (uuidsList.Contains("NONE"))
            return;
        else
        {
            foreach (string uuid in uuidsList)
            {
                if (Guid.TryParse(uuid, out Guid uuidGuid))
                {
                    Debug.Log($"Trying to find {uuid}");
                    CollectableLight matchingLight = lightsToCollect.FirstOrDefault(light => light.UUID == uuidGuid);

                    // If a matching light is found, perform any actions (e.g., collecting the light).
                    if (matchingLight != null)
                    {
                        Console.WriteLine($"Light with UUID {matchingLight.UUID} collected!");
                        Destroy(matchingLight.lightGameObject);
                    }
                }
                else
                {
                    Console.WriteLine($"Invalid UUID format: {uuid}");
                }
            }
        }
    }
}
