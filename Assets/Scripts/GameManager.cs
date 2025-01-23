using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;
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
            var playerLightsPacket = new Packet((byte)PacketType.Action, "GetPlayerLights");
            playerLightsPacket.Send(serverSocket);
            Packet response = Packet.Receive(serverSocket);
            if (response.Data != "EMPTY")
            {
                string[] receivedLights = response.Data.Split(" ");
                foreach (string light in receivedLights)
                {
                    string[] splittedData = light.Split("|");
                    if (splittedData.Length > 1)
                    {
                        string lightUuid = splittedData[0];

                        bool lightExists = spawnedLights.Exists(l =>
                        {
                            return l.UUID == Guid.Parse(lightUuid);
                        });

                        if (!lightExists)
                        {
                            Vector3 lightPosition = new Vector3(float.Parse(splittedData[1]), float.Parse(splittedData[2]), float.Parse(splittedData[3]));
                            GameObject lightInstance = Instantiate(lightPrefab);
                            lightInstance.transform.position = lightPosition;
                            CollectableLightStateManager lightComponent = lightInstance.GetComponent<CollectableLightStateManager>();
                            lightComponent.UUID = Guid.Parse(lightUuid);
                            spawnedLights.Add(lightComponent);
                        }
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

        string dataPack = $"{position.x}|{position.y}|{position.z} ";
        foreach (CollectableLightStateManager CLSM in toCollect)
        {
            dataPack += CLSM.UUID.ToString() + " ";
        }

        Packet collectionPacket = new Packet((byte)Packet.PacketType.Action, $"CollectLights {dataPack}");
        collectionPacket.Send(serverSocket);
        Packet collectionPacketResponse = Packet.Receive(serverSocket);
        if (collectionPacketResponse.Data == "NONE")
            return;
        else
        {
            string[] splittedUuids = collectionPacketResponse.Data.Split(" ");
            foreach (string uuid in splittedUuids)
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
        Packet getLightTowersPacket = new Packet((byte)PacketType.Action, "GetLightTowers");
        getLightTowersPacket.Send(serverSocket);
        Packet lightTowersPacketResult = Packet.Receive(serverSocket);
        string[] splittedData = lightTowersPacketResult.Data.Split("_");
        Debug.Log(lightTowersPacketResult.Data);
        foreach (string data in splittedData)
        {
            if (data.Length > 1)
            {
                Debug.Log(data);
                string[] dataArray = data.Split("|");
                LightTowerSO lightTowerSO = ScriptableObject.CreateInstance<LightTowerSO>();
                Debug.Log($"{int.Parse(dataArray[0])}");
                Debug.Log($"{DateTime.Parse(dataArray[1])}");
                Debug.Log($"{float.Parse(dataArray[2])}");
                Debug.Log($"{int.Parse(dataArray[3])}");
                lightTowerSO.TowerNum = int.Parse(dataArray[0]);
                lightTowerSO.InitDate = DateTime.Parse(dataArray[1]);
                lightTowerSO.Multiplier = float.Parse(dataArray[2]);
                lightTowerSO.BaseAmount = int.Parse(dataArray[3]);
                lightTowersDatabase.unlockedTowers.Add(lightTowerSO);
            }
        }
    }
}
