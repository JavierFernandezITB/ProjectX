using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
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
        var authPacket = new Packet((byte)PacketType.Auth, $"LOGIN {usernameField.text} {passwordField.text}");
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
        var authPacket = new Packet((byte)PacketType.Auth, $"REGISTER {usernameField.text} {passwordField.text} test@itb.cat");
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

        var authPacket = new Packet((byte)PacketType.Auth, $"TLOGIN {token}");
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
        string[] response = responsePacket.Data.Split(" ");
        if (response.Length < 2)
        {
            Debug.LogError("Invalid response from server.");
            return;
        }

        string responseType = response[0];
        string responseStatus = response[1];

        if (responseStatus == "OK")
        {
            HandleAuthSuccess(responseType, response);
        }
        else
        {
            HandleAuthFailure(responseType);
        }
    }

    private void HandleAuthSuccess(string responseType, string[] response)
    {
        switch (responseType)
        {
            case "REGISTER":
                Debug.Log("Registration successful! Please log in.");
                return;

            case "LOGIN":
                SaveAuthToken(response[2]);
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

    private void SetupAccount(string[] response)
    {
        isClientAuthenticated = true;

        if (response.Length >= 5)
        {
            accountData.playerId = Convert.ToInt32(response[3]);
            accountData.username = response[4];
            playerData.playerId = accountData.playerId;

            Debug.Log($"Account setup complete. PlayerID: {accountData.playerId}, Username: {accountData.username}");
        }
        else
        {
            Debug.LogError("Insufficient data to setup account.");
        }

        UpdatePlayerData();
        gameManager.serverSocket = serverSocket;
        gameManager.StartMainGameLoop();
    }

    public void UpdatePlayerData()
    {
        var playerDataPacket = new Packet((byte)PacketType.Action, "GetPlayerData");
        playerDataPacket.Send(serverSocket);

        Packet playerDataResponse = Packet.Receive(serverSocket);

        string[] splittedData = playerDataResponse.Data.Split(" ");

        playerData.playerId = int.Parse(splittedData[0]);
        playerData.lightCurrency = int.Parse(splittedData[1]);
        playerData.premiumCurrency = int.Parse(splittedData[2]);
        playerData.masteryPoints = int.Parse(splittedData[3]);
        playerData.specialSkillCharge = float.Parse(splittedData[4]);
        playerData.specialShieldCharge = float.Parse(splittedData[5]);
    }
}
