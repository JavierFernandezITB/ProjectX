using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EntityService : ServicesReferences
{
    public int selectedTowerNum;
    public GameObject lightPrefab;
    public GameObject towerMenuPrefab;
    public GameObject currentTowerMenuObject;
    public List<CollectableLight> spawnedLights = new List<CollectableLight>();
    public List<LightTower> lightTowers = new List<LightTower>();
    public GameObject aiAnimalPrefab;
    public NavMeshAgent aiAnimalAgent;

    private void Awake()
    {
        base.GetServices();
        base.Persist<EntityService>();
    }

    private void Start()
    {
        SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
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

    private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Collection_Lvl" && aiAnimalAgent == null)
        {
            aiAnimalAgent = Instantiate(aiAnimalPrefab).GetComponent<NavMeshAgent>();
            StartCoroutine(aiAgentCoroutine());
        }
    }

    private IEnumerator aiAgentCoroutine()
    {
        yield return new WaitForSeconds(3f);
        while (aiAnimalAgent != null)
        {
            yield return new WaitForSeconds(1f);
            if (spawnedLights.Count > 0)
            {
                // Find the closest light
                GameObject closestLight = FindClosestLight();

                if (closestLight != null)
                {
                    Debug.Log("Alerta por subnormal.");
                    // Set the destination to the closest light's position
                    aiAnimalAgent.SetDestination(closestLight.transform.position);

                    yield return new WaitUntil(() =>
                    {
                        if (closestLight == null)
                            return true;
                        return aiAnimalAgent.remainingDistance <= aiAnimalAgent.stoppingDistance && !aiAnimalAgent.pathPending;
                    });

                    // Check if the light still exists
                    if (closestLight == null)
                    {
                        Debug.Log("The light was destroyed or picked up before arrival.");
                        continue; // Skip to the next iteration
                    }

                    Debug.Log("Arrived at the closest light!");
                    yield return new WaitForSeconds(5f); // Optional delay before interacting

                    try
                    {
                        // Convert the light's position to screen space
                        Vector3 pos = Camera.main.WorldToScreenPoint(closestLight.transform.position);
                        touchManagerService.TouchPressCallback(pos);
                    }
                    catch
                    {
                        Debug.Log("The light got destroyed or already picked up.");
                    }
                }
            }
        }
    }

    private GameObject FindClosestLight()
    {
        GameObject closestLight = null;
        float closestDistance = Mathf.Infinity; // Initialize with a very large value

        foreach (var lightData in spawnedLights)
        {
            if (lightData.lightGameObject != null)
            {
                // Calculate the distance between the AI agent and the light
                float distance = Vector3.Distance(aiAnimalAgent.transform.position, lightData.lightGameObject.transform.position);

                // Check if this light is closer than the previous closest light
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestLight = lightData.lightGameObject;
                }
            }
        }

        return closestLight;
    }

    private void OnLightTowerReceived(LightTower towerObject)
    {
        Debug.Log(towerObject.TowerNum);
        StartCoroutine(AddTowerInList(towerObject));
    }

    private IEnumerator AddTowerInList(LightTower towerObject)
    {
        GameObject mapObject = GameObject.Find($"/LightTower {towerObject.TowerNum}");
        while (mapObject == null)
        {
            yield return new WaitForSeconds(.1f);
            mapObject = GameObject.Find($"/LightTower {towerObject.TowerNum}");
        }

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
        networkService.UpdatePlayerData();
    }
}
