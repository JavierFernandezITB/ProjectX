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
    public List<LightTower> lightTowers = new List<LightTower>();

    private void Awake()
    {
        base.GetServices();
        base.Persist<EntityService>();
    }

    private void OnEnable()
    {
        networkService.LightReceived += OnLightReceived;
        networkService.TowerReceived += OnLightTowerReceived;
        touchManagerService.CollectTower += OnLightTowerCollected;
        touchManagerService.CollectLight += OnLightCollected;
    }

    private void OnDisable()
    {
        networkService.LightReceived -= OnLightReceived;
        networkService.TowerReceived -= OnLightTowerReceived;
        touchManagerService.CollectTower -= OnLightTowerCollected;
        touchManagerService.CollectLight -= OnLightCollected;
    }

    private void OnLightTowerReceived(LightTower towerObject)
    {
        Debug.Log(towerObject.TowerNum);
        GameObject mapObject = GameObject.Find($"/LightTower {towerObject.TowerNum}");
        if (mapObject)
        {
            Debug.Log("Found game object.");
            towerObject.towerGameObject = mapObject;
            lightTowers.Add(towerObject);
        }
    }

    private void OnLightTowerCollected(GameObject towerObject)
    {
        Debug.Log(towerObject.name);

        LightTower lightTowerObject = lightTowers.FirstOrDefault(tower => tower.towerGameObject == towerObject);

        if (lightTowerObject != null)
        {
            Debug.Log("Found the light tower object in the list!");
        }
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
        /*
        Dictionary<string, object> playerDataPacketDict = new Dictionary<string, object>()
        {
            { "action", "Patata" },
            { "params", new Dictionary<string, object>() }
        };

        Packet playerDataPacket = new Packet((byte)Packet.PacketType.Action, JObject.FromObject(playerDataPacketDict));
        playerDataPacket.Send(networkService.localClient.serverSocket);

        Packet playerDataPacketResult = Packet.Receive(networkService.localClient.serverSocket);
        Dictionary<string, object> playerDataParams = playerDataPacket.Data["params"].ToObject<Dictionary<string, object>>();
        int patatas = (int)playerDataParams["patatas"];
        */

        Packet collectionPacketResponse = Packet.Receive(networkService.localClient.serverSocket);

        Dictionary<string, object> responseParams = collectionPacketResponse.Data["params"].ToObject<Dictionary<string, object>>();

        List<string> uuidsList = (responseParams["uuidsList"] as JArray)?.ToObject<List<string>>() ?? new List<string>();

        if (uuidsList.Contains("NONE"))
            return;

        foreach (string uuid in uuidsList)
        {
            if (Guid.TryParse(uuid, out Guid uuidGuid))
            {
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
