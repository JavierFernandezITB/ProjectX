using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UI;

public class EntityService : ServicesReferences
{
    public int selectedTowerNum;
    public GameObject lightPrefab;
    public GameObject towerMenuPrefab;
    public GameObject currentTowerMenuObject;
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
        touchManagerService.InteractWithTower += OnLightTowerInteracted;
        touchManagerService.CollectLight += OnLightCollected;
    }

    private void OnDisable()
    {
        networkService.LightReceived -= OnLightReceived;
        networkService.TowerReceived -= OnLightTowerReceived;
        touchManagerService.InteractWithTower -= OnLightTowerInteracted;
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

    private int CalculateTowerRewards(LightTower lightTowerData)
    {
        TimeSpan elapsedTime = DateTime.Now - lightTowerData.InitDate;
        int reward = (int)(elapsedTime.TotalMinutes * (lightTowerData.BaseAmount * lightTowerData.Multiplier));
        return reward;
    }
    
    public void CloseTowerMenu()
    {
        touchManagerService.isInMenu = false;
        Destroy(currentTowerMenuObject);
    }

    public void UpdateTowerMenuData(LightTower lightTowerObject)
    {
        currentTowerMenuObject.transform.GetChild(4).GetComponent<TMP_Text>().text = CalculateTowerRewards(lightTowerObject).ToString();
        currentTowerMenuObject.transform.GetChild(9).gameObject.SetActive(false);
        int price = (int)(lightTowerObject.BaseAmount * lightTowerObject.Multiplier * lightTowerObject.TowerNum);
        currentTowerMenuObject.transform.GetChild(5).GetComponent<TMP_Text>().text = price.ToString();
        int levels = (int)((lightTowerObject.Multiplier - 1) / 0.05);
        currentTowerMenuObject.transform.GetChild(2).GetComponent<TMP_Text>().text = levels.ToString();
    }

    private void OnLightTowerInteracted(GameObject towerObject) 
    {
        Debug.Log(towerObject.name);
        currentTowerMenuObject = Instantiate(towerMenuPrefab);
        // Assuming the buttons have indices
        Button collectButton = currentTowerMenuObject.transform.GetChild(7).GetComponent<Button>();
        Button upgradeButton = currentTowerMenuObject.transform.GetChild(8).GetComponent<Button>();
        Button purchaseButton = currentTowerMenuObject.transform.GetChild(9).GetComponent<Button>();
        Button closeButton = currentTowerMenuObject.transform.GetChild(10).GetComponent<Button>();

        // Check if the buttons exist and add listeners
        if (collectButton != null)
        {
            collectButton.onClick.AddListener(() => networkService.RequestTowerCollection());
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(() => networkService.RequestUpgradeTower());
        }

        if (purchaseButton != null)
        {
            purchaseButton.onClick.AddListener(() => networkService.RequestPurchaseTower());
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseTowerMenu);
        }




        LightTower lightTowerObject = lightTowers.FirstOrDefault(tower => tower.towerGameObject == towerObject);

        selectedTowerNum = int.Parse(towerObject.name.Split(" ")[1]);

        if (lightTowerObject == null)
        {
            int price = networkService.RequestPurchasableTowerPrice(selectedTowerNum);
            currentTowerMenuObject.transform.GetChild(5).GetComponent<TMP_Text>().text = price.ToString();
            currentTowerMenuObject.transform.GetChild(4).GetComponent<TMP_Text>().text = "0";
            currentTowerMenuObject.transform.GetChild(2).GetComponent<TMP_Text>().text = "1";
            currentTowerMenuObject.transform.GetChild(9).gameObject.SetActive(true);
        }
        else
        {
            UpdateTowerMenuData(lightTowerObject);
        }

        touchManagerService.isInMenu = true;
        currentTowerMenuObject.SetActive(true);
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
