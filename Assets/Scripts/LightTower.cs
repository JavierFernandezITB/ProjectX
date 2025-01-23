using System;
using System.Collections;
using System.Collections.Generic;
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
        Packet collectTowerPacket = new Packet((byte)Packet.PacketType.Action, $"CollectLightTowers {lightTowerData.TowerNum}");
        collectTowerPacket.Send(client.serverSocket);
        // Discard the response packet for now.
        Packet collectTowerResponse = Packet.Receive(client.serverSocket);
        Debug.Log($"New Datetime: {collectTowerResponse.Data}");
        lightTowerData.InitDate = DateTime.Parse(collectTowerResponse.Data);

        // Update player data, just to get the new light currency value.
        client.UpdatePlayerData();
    }
}
