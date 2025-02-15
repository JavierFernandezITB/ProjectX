using Newtonsoft.Json.Linq;
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

    private void Awake()
    {
        base.GetServices();
        base.Persist<EntityService>();
    }

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
        // Receive packet from server
        Packet collectionPacketResponse = Packet.Receive(networkService.localClient.serverSocket);

        // Deserialize the "params" dictionary safely
        Dictionary<string, object> responseParams = collectionPacketResponse.Data["params"].ToObject<Dictionary<string, object>>();

        // Safely extract "uuidsList"
        List<string> uuidsList = (responseParams["uuidsList"] as JArray)?.ToObject<List<string>>() ?? new List<string>();

        // If "NONE" is in the list, stop execution
        if (uuidsList.Contains("NONE"))
            return;

        foreach (string uuid in uuidsList)
        {
            if (Guid.TryParse(uuid, out Guid uuidGuid))
            {
                // Find matching light in lightsToCollect list
                CollectableLight matchingLight = lightsToCollect.FirstOrDefault(light => light.UUID == uuidGuid);

                if (matchingLight != null)
                {
                    Console.WriteLine($"Light with UUID {matchingLight.UUID} collected!");
                    Destroy(matchingLight.lightGameObject);
                    spawnedLights.Remove(matchingLight);
                }
            }
        }
    }
}
