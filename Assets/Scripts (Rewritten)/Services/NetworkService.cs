using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using static Packet;

public class NetworkService : ServicesReferences
{
    // Public variables.
    public Client localClient;
    public Player localPlayer;
    public Account localAccount;

    // Events.
    public event Action<Dictionary<string, string>> LightReceived;
    public event Action<LightTower> TowerReceived;

    // Private variables.
    private const string AuthTokenFilePath = "./authTokenFile";

    private void Awake()
    {
        base.GetServices();
        base.Persist<NetworkService>();

        localClient = new Client("127.0.0.1", 18800, AuthTokenFilePath);
        // Connection is started in OnEnable.
    }

    private void OnEnable()
    {
        localClient.ConnectionEstablished += OnClientConnected;
        localClient.ConnectionFailed += OnClientConnectionFailed;
        localClient.AuthenticationSuccess += OnClientSuccessfullyAuthenticated;
        localClient.AuthenticationFailed += OnClientFailedAuthentication;

        // here lol
        localClient.StartConnection();
    }

    private void OnDisable()
    {
        localClient.ConnectionEstablished -= OnClientConnected;
        localClient.ConnectionFailed -= OnClientConnectionFailed;
        localClient.AuthenticationSuccess -= OnClientSuccessfullyAuthenticated;
        localClient.AuthenticationFailed -= OnClientFailedAuthentication;
    }

    private void OnClientConnected()
    {
        Debug.Log("Connected to the server successfully.");
        localClient.AccountTokenLogin();
    }

    private void OnClientConnectionFailed()
    {
        Debug.Log("Failed to stablish connection with the server. Is the server online?");
        Debug.Log("Retrying to stablish connection in 5 seconds...");
        StartCoroutine(StartClientCoroutine());
    }

    private void OnClientSuccessfullyAuthenticated(Packet response)
    {
        Debug.Log("Client authenticated successfully.");
        localAccount = new Account((int)response.Data["accountid"], (string)response.Data["username"]);
        localPlayer = new Player();
        UpdatePlayerData();

        // Start network loop!
        StartCoroutine(NetworkLoop());
    }

    private void OnClientFailedAuthentication()
    {
        Debug.Log("Authentication failed.");
    }

    private IEnumerator StartClientCoroutine()
    {
        yield return new WaitForSeconds(5);
        localClient = new Client("127.0.0.1", 18800, AuthTokenFilePath);
    }

    // goofy ass name
    private IEnumerator NetworkLoop()
    {
        RequestLightTowers();
        while (localClient.serverSocket.Connected)
        {
            RequestCollectableLights();

            yield return new WaitForSeconds(1);
        }
    }

    private void RequestLightTowers()
    {
        Dictionary<string, object> paramsDict = new Dictionary<string, object>()
        {
        };

        Dictionary<string, object> getLightTowers = new Dictionary<string, object>() {
            { "action", "GetLightTowers" },
            { "params", paramsDict }
        };

        Packet getLightTowersPacket = new Packet((byte)PacketType.Action, JObject.FromObject(getLightTowers));
        getLightTowersPacket.Send(localClient.serverSocket);

        Packet lightTowersPacketResult = Packet.Receive(localClient.serverSocket);
        Dictionary<string, object> responseParams = lightTowersPacketResult.Data["params"].ToObject<Dictionary<string, object>>();

        JArray lightTowersDataArray = lightTowersPacketResult.Data["params"]["towersDataDict"] as JArray;
        List<Dictionary<string, string>> towersData = lightTowersDataArray?.ToObject<List<Dictionary<string, string>>>();

        Debug.Log(lightTowersPacketResult.Data);
        foreach (Dictionary<string, string> entry in towersData)
        {
            LightTower tower = new LightTower();
            tower.TowerNum = int.Parse(entry["towerNum"]);
            tower.InitDate = DateTime.Parse(entry["initDate"]);
            tower.Multiplier = float.Parse(entry["multiplier"]);
            tower.BaseAmount = int.Parse(entry["baseAmount"]);
            TowerReceived?.Invoke(tower);
        }
    }

    private void RequestCollectableLights()
    {
        Dictionary<string, object> paramsDict = new Dictionary<string, object>()
        {
        };

        Dictionary<string, object> getPlayerLights = new Dictionary<string, object>() {
            { "action", "GetPlayerLights" },
            { "params", paramsDict }
        };

        var playerLightsPacket = new Packet((byte)PacketType.Action, JObject.FromObject(getPlayerLights));
        playerLightsPacket.Send(localClient.serverSocket);

        Packet response = Packet.Receive(localClient.serverSocket);

        // Deserialize params dictionary
        Dictionary<string, object> responseParams = response.Data["params"].ToObject<Dictionary<string, object>>();

        // Extract and convert "lightsDataDict"
        JArray lightsDataArray = response.Data["params"]["lightsDataDict"] as JArray;
        List<Dictionary<string, string>> lightsDataDict = lightsDataArray?.ToObject<List<Dictionary<string, string>>>();

        if (lightsDataDict.Count != 0)
        {
            foreach (Dictionary<string, string> entry in lightsDataDict)
            {
                LightReceived?.Invoke(entry);
            }
        }
    }

    public void UpdatePlayerData()
    {
        Dictionary<string, object> paramsDict = new Dictionary<string, object>()
        {
        };

        Dictionary<string, object> getPlayerData = new Dictionary<string, object>() {
            { "action", "GetPlayerData" },
            { "params", paramsDict }
        };
        var playerDataPacket = new Packet((byte)PacketType.Action, JObject.FromObject(getPlayerData));
        playerDataPacket.Send(localClient.serverSocket);

        Packet playerDataResponse = Packet.Receive(localClient.serverSocket);
        Dictionary<string, object> responseParams = playerDataResponse.Data["params"].ToObject<Dictionary<string, object>>();

        localPlayer.playerId = Convert.ToInt32(responseParams["playerId"]);
        localPlayer.lightCurrency = Convert.ToInt32(responseParams["lightPoints"]);
        localPlayer.premiumCurrency = Convert.ToInt32(responseParams["premPoints"]);
        localPlayer.masteryPoints = Convert.ToInt32(responseParams["masteryPoints"]);
        localPlayer.specialSkillCharge = Convert.ToSingle(responseParams["currentSpecialSkillCharge"]);
        localPlayer.specialShieldCharge = Convert.ToSingle(responseParams["currentSpecialShieldCharge"]);
    }
}
