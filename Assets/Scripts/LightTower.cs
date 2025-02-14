using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public class LightTower : MonoBehaviour
{
    public LightTowerSO lightTowerData;
    public int towerReferenceNumber;
    public LightTowerDatabaseSO database;
    public bool unlocked = false;
    public Client client;

    void Start()
    {
        try
        {
            lightTowerData = database.unlockedTowers[towerReferenceNumber - 1];
            unlocked = true;
        }
        catch
        {
            unlocked = false;
        }

    }

    public int CalculateTowerRewards()
    {
        TimeSpan elapsedTime = DateTime.Now - lightTowerData.InitDate;
        int reward = (int)(elapsedTime.TotalMinutes * (lightTowerData.BaseAmount * lightTowerData.Multiplier));
        return reward;
    }
    public void CollectTowerRewards()
    {
        if (!unlocked)
            return;
        Debug.Log($"Expected Reward: {CalculateTowerRewards()}");

        Dictionary<string, object> paramsDict = new Dictionary<string, object>()
        {
            { "towerId", lightTowerData.TowerNum }
        };

        Dictionary<string, object> collectLightTowersData = new Dictionary<string, object>() {
            { "action", "CollectLightTowers" },
            { "params", paramsDict }
        };

        Packet collectTowerPacket = new Packet((byte)Packet.PacketType.Action, JObject.FromObject(collectLightTowersData));
        Dictionary<string, object> responseParams = collectTowerPacket.Data["params"].ToObject<Dictionary<string, object>>();
        collectTowerPacket.Send(client.serverSocket);

        // Discard the response packet for now.
        Packet collectTowerResponse = Packet.Receive(client.serverSocket);
        Debug.Log($"New Datetime: {collectTowerResponse.Data}");
        lightTowerData.InitDate = DateTime.Parse((string)responseParams["serverInitDate"]);

        // Update player data, just to get the new light currency value.
        client.UpdatePlayerData();
    }
}
