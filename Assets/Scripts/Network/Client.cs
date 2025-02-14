using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Xml.Linq;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UI;
using static Packet;

public class Client : MonoBehaviour
{
    public TcpClient serverSocket;
    public bool isClientAuthenticated = false;
    public AccountSO accountData;
    public PlayerSO playerData;

    public GameObject loginPanel;
    public GameObject uiPanel;
    public GameManager gameManager;
    public GameObject baseMap;
    public InputField usernameField;
    public InputField passwordField;
    public UnityEngine.UI.Button loginButton;
    public UnityEngine.UI.Button registerButton;

    private const string ServerIP = "127.0.0.1";
    private const int ServerPort = 18800;
    private const string AuthTokenFilePath = "./authTokenFile";

    void Awake()
    {
        loginButton.onClick.AddListener(AccountLogin);
        registerButton.onClick.AddListener(AccountRegister);
        gameManager = GameObject.Find("/GameManager").GetComponent<GameManager>();
    }

    void Start()
    {
        StartConnection();
    }

    private void StartConnection()
    {
        Debug.Log("Attempting to connect to the server...");
        serverSocket = new TcpClient(ServerIP, ServerPort);

        if (serverSocket.Connected)
        {
            Debug.Log("Connected to server. Checking authentication...");
            if (File.Exists(AuthTokenFilePath))
            {
                AccountTokenLogin();
            }
        }
        else
        {
            Debug.LogError("Failed to connect to server.");
        }
    }

    private void AccountLogin()
    {
        if (!serverSocket.Connected)
        {
            Debug.LogError("Not connected to the server.");
            return;
        }

        Debug.Log("Crafting login packet...");
        Dictionary<string, string> loginData = new Dictionary<string, string>() {
            { "action", "LOGIN" },
            { "username", usernameField.text },
            { "passwd", passwordField.text }
        };
        var authPacket = new Packet((byte)PacketType.Auth, JObject.FromObject(loginData));
        SendAndHandleAuthResponse(authPacket);
    }

    private void AccountRegister()
    {
        if (!serverSocket.Connected)
        {
            Debug.LogError("Not connected to the server.");
            return;
        }

        Debug.Log("Crafting registration packet...");
        Dictionary<string, string> registerData = new Dictionary<string, string>() {
            { "action", "REGISTER" },
            { "username", usernameField.text },
            { "passwd", passwordField.text },
            { "email", "test@itb.cat" }
        };
        var authPacket = new Packet((byte)PacketType.Auth, JObject.FromObject(registerData));
        SendAndHandleAuthResponse(authPacket);
    }

    private void AccountTokenLogin()
    {
        if (!serverSocket.Connected)
        {
            Debug.LogError("Not connected to the server.");
            return;
        }

        Debug.Log("Crafting token login packet...");
        string token = GetSavedAuthToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No valid token found.");
            return;
        }

        Dictionary<string, string> tloginData = new Dictionary<string, string>() {
            { "action", "TLOGIN" },
            { "token", token }
        };

        var authPacket = new Packet((byte)PacketType.Auth, JObject.FromObject(tloginData));
        SendAndHandleAuthResponse(authPacket);
    }

    private void SaveAuthToken(string token)
    {
        try
        {
            File.WriteAllText(AuthTokenFilePath, token);
            Debug.Log("Authentication token saved.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save auth token: {ex.Message}");
        }
    }

    private string GetSavedAuthToken()
    {
        try
        {
            if (File.Exists(AuthTokenFilePath))
            {
                string token = File.ReadAllText(AuthTokenFilePath);
                if (token.StartsWith("PXAT_"))
                {
                    return token;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to read auth token: {ex.Message}");
        }

        return string.Empty;
    }

    private void SendAndHandleAuthResponse(Packet authPacket)
    {
        try
        {
            Debug.Log("Sending authentication packet...");
            authPacket.Send(serverSocket);

            Debug.Log("Waiting for server response...");
            Packet responsePacket = Packet.Receive(serverSocket);
            ProcessAuthResponse(responsePacket);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during authentication: {ex.Message}");
        }
    }

    private void ProcessAuthResponse(Packet responsePacket)
    {
        string responseType = (string)responsePacket.Data["action"];
        string responseStatus = (string)responsePacket.Data["response"];

        if (responseStatus == "OK")
        {
            HandleAuthSuccess(responseType, responsePacket);
        }
        else
        {
            HandleAuthFailure(responseType);
        }
    }

    private void HandleAuthSuccess(string responseType, Packet response)
    {
        Debug.Log(responseType);
        switch (responseType)
        {
            case "REGISTER":
                Debug.Log("Registration successful! Please log in.");
                return;

            case "LOGIN":
                SaveAuthToken((string)response.Data["token"]);
                SetupAccount(response);
                break;

            case "TLOGIN":
                SetupAccount(response);
                break;

            default:
                Debug.LogError("Unexpected success response type.");
                return;
        }

        Debug.Log("Authentication successful.");
        loginPanel.SetActive(false);
        //uiPanel.SetActive(false);
        baseMap.SetActive(true);
    }

    private void HandleAuthFailure(string responseType)
    {
        Debug.LogError($"Authentication failed. Type: {responseType}");
        if (responseType == "TLOGIN")
        {
            File.Delete(AuthTokenFilePath);
        }

        Debug.Log("Retrying connection...");
        StartConnection();
    }

    private void SetupAccount(Packet response)
    {
        accountData.playerId = (int)response.Data["accountid"];
        accountData.username = (string)response.Data["username"];
        playerData.playerId = accountData.playerId;

        Debug.Log($"Account setup complete. PlayerID: {accountData.playerId}, Username: {accountData.username}");

        UpdatePlayerData();

        isClientAuthenticated = true;
        gameManager.serverSocket = serverSocket;
        gameManager.StartMainGameLoop();
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
        playerDataPacket.Send(serverSocket);

        Packet playerDataResponse = Packet.Receive(serverSocket);
        Dictionary<string, object> responseParams = playerDataResponse.Data["params"].ToObject<Dictionary<string, object>>();

        playerData.playerId = (int)responseParams["playerId"];
        playerData.lightCurrency = (int)responseParams["lightPoints"];
        playerData.premiumCurrency = (int)responseParams["premPoints"];
        playerData.masteryPoints = (int)responseParams["masteryPoints"];
        playerData.specialSkillCharge = (float)responseParams["currentSpecialSkillCharge"];
        playerData.specialShieldCharge = (float)responseParams["currentSpecialShieldCharge"];
    }
}
