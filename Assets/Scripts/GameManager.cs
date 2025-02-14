using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Packet;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    public TcpClient serverSocket;

    public LightTowerDatabaseSO lightTowersDatabase;
    public GameObject lightPrefab;
    public List<CollectableLightStateManager> spawnedLights = new List<CollectableLightStateManager>();

    public void StartMainGameLoop()
    {
        StartCoroutine(GameManagerMainLoop());
    }

    public IEnumerator GameManagerMainLoop()
    {
        // One time functions .-.

        Debug.Log("and again :skull:");
        lightTowersDatabase.unlockedTowers = new List<LightTowerSO>();
        UpdateTowersDatabase();

        // well well well...

        while (serverSocket.Connected)
        {
            Dictionary<string, object> paramsDict = new Dictionary<string, object>()
            {
            };

            Dictionary<string, object> getPlayerLights = new Dictionary<string, object>() {
                { "action", "GetPlayerLights" },
                { "params", paramsDict }
            };

            var playerLightsPacket = new Packet((byte)PacketType.Action, JObject.FromObject(getPlayerLights));
            playerLightsPacket.Send(serverSocket);
            Packet response = Packet.Receive(serverSocket);
            Dictionary<string, object> responseParams = response.Data["params"].ToObject<Dictionary<string, object>>();
            List<Dictionary<string, string>> lightsDataDict = responseParams["lightsDataDict"].ConvertTo<List<Dictionary<string, string>>>();

            if (lightsDataDict.Count != 0)
            {
                foreach (Dictionary<string, string> entry in lightsDataDict)
                {
                    string lightUuid = entry["uuid"];

                    bool lightExists = spawnedLights.Exists(l =>
                    {
                        return l.UUID == Guid.Parse(lightUuid);
                    });

                    if (!lightExists)
                    {
                        Vector3 lightPosition = new Vector3(float.Parse(entry["lightPosX"]), float.Parse(entry["lightPosY"]), float.Parse(entry["lightPosZ"]));
                        GameObject lightInstance = Instantiate(lightPrefab);
                        lightInstance.transform.position = lightPosition;
                        CollectableLightStateManager lightComponent = lightInstance.GetComponent<CollectableLightStateManager>();
                        lightComponent.UUID = Guid.Parse(lightUuid);
                        spawnedLights.Add(lightComponent);
                    }
                }
            }
            yield return new WaitForSeconds(5);
        }
    }

    public void CollectLights(Vector3 position)
    {
        Collider[] hitColliders = Physics.OverlapSphere(position, 5f);
        List<CollectableLightStateManager> toCollect = new List<CollectableLightStateManager>();

        if (!(hitColliders.Length > 0))
            return;

        foreach (Collider collider in hitColliders)
        {
            // Check if the collider's object has the correct name
            CollectableLightStateManager CLSM = collider.GetComponent<CollectableLightStateManager>();
            if (CLSM != null)
            {
                toCollect.Add(CLSM);
            }
        }



        Dictionary<string, object> paramsDict = new Dictionary<string, object>()
        {
            { "mousePosX", position.x},
            { "mousePosY", position.y},
            { "mousePosZ", position.z},
            { "uuidList", new List<string>() }
        };

        foreach (CollectableLightStateManager CLSM in toCollect)
        {
            paramsDict["uuidList"].ConvertTo<List<string>>().Add(CLSM.UUID.ToString());
        }

        Dictionary<string, object> collectLights = new Dictionary<string, object>() {
            { "action", "CollectLights" },
            { "params", paramsDict }
        };

        Packet collectionPacket = new Packet((byte)Packet.PacketType.Action, JObject.FromObject(collectLights));
        collectionPacket.Send(serverSocket);
        Packet collectionPacketResponse = Packet.Receive(serverSocket);
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
                    CollectableLightStateManager matchingLight = toCollect.FirstOrDefault(light => light.UUID == uuidGuid);

                    // If a matching light is found, perform any actions (e.g., collecting the light).
                    if (matchingLight != null)
                    {
                        Console.WriteLine($"Light with UUID {matchingLight.UUID} collected!");
                        StartCoroutine(MoveLightTowardsTarget(matchingLight.gameObject, position));
                    }
                    else
                    {
                        Console.WriteLine("Not found.");
                        foreach (var light in toCollect)
                        {
                            Debug.Log(light.UUID);
                        }
                        return;
                    }
                }
                else
                {
                    Console.WriteLine($"Invalid UUID format: {uuid}");
                }
            }
            GameObject.Find("/NetworkManager").GetComponent<Client>().UpdatePlayerData();
        }
    }

    public IEnumerator MoveLightTowardsTarget(GameObject light, Vector3 targetPosition)
    {
        float moveSpeed = 3f;
        float step = moveSpeed * Time.deltaTime;

        while (Vector3.Distance(light.transform.position, targetPosition) > 0.1f)
        {
            light.transform.position = Vector3.MoveTowards(light.transform.position, targetPosition, step);
            yield return null;
        }

        Destroy(light);
    }

    public void UpdateTowersDatabase()
    {
        Dictionary<string, object> paramsDict = new Dictionary<string, object>()
        {
        };

        Dictionary<string, object> getLightTowers = new Dictionary<string, object>() {
            { "action", "GetLightTowers" },
            { "params", paramsDict }
        };

        Packet getLightTowersPacket = new Packet((byte)PacketType.Action, JObject.FromObject(getLightTowers));
        getLightTowersPacket.Send(serverSocket);

        Packet lightTowersPacketResult = Packet.Receive(serverSocket);
        Dictionary<string, object> responseParams = lightTowersPacketResult.Data["params"].ToObject<Dictionary<string, object>>();

        List<Dictionary<string, string>> towersData = responseParams["towersDataDict"].ConvertTo<List<Dictionary<string, string>>>();
        Debug.Log(lightTowersPacketResult.Data);
        foreach (Dictionary<string, string> entry in towersData)
        {
            LightTowerSO lightTowerSO = ScriptableObject.CreateInstance<LightTowerSO>();
            lightTowerSO.TowerNum = int.Parse(entry["towerNum"]);
            lightTowerSO.InitDate = DateTime.Parse(entry["initDate"]);
            lightTowerSO.Multiplier = float.Parse(entry["multiplier"]);
            lightTowerSO.BaseAmount = int.Parse(entry["baseAmount"]);
            Debug.Log($"{lightTowerSO.TowerNum}");
            Debug.Log($"{lightTowerSO.InitDate}");
            Debug.Log($"{lightTowerSO.Multiplier}");
            Debug.Log($"{lightTowerSO.BaseAmount}");
            lightTowersDatabase.unlockedTowers.Add(lightTowerSO);
        }
    }
}
