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

public class Client
{
    // Public variables.
    public TcpClient serverSocket;

    // Events.
    public event Action ConnectionEstablished;
    public event Action ConnectionFailed;
    public event Action<Packet> AuthenticationSuccess;
    public event Action AuthenticationFailed;


    // Private variables.
    private string ServerIP;
    private int ServerPort;
    private string AuthTokenPath;

    public Client(string serverIp, int serverPort, string authTokenPath)
    {
        ServerIP = serverIp;
        ServerPort = serverPort;
        AuthTokenPath = authTokenPath;
    }

    private void StartConnection()
    {
        Debug.Log("Attempting to connect to the server...");
        serverSocket = new TcpClient(ServerIP, ServerPort);

        if (serverSocket.Connected)
        {
            ConnectionEstablished?.Invoke();
        }
        else
        {
            ConnectionFailed?.Invoke();
        }
    }

    private void AccountLogin()
    {
        Dictionary<string, string> loginData = new Dictionary<string, string>() {
            { "action", "LOGIN" },
            { "username", "test" },
            { "passwd", "test" }
        };
        var authPacket = new Packet((byte)PacketType.Auth, JObject.FromObject(loginData));
        SendAndHandleAuthResponse(authPacket);
    }

    private void AccountRegister()
    {
        Dictionary<string, string> registerData = new Dictionary<string, string>() {
            { "action", "REGISTER" },
            { "username", "test" },
            { "passwd", "test" },
            { "email", "test@itb.cat" }
        };
        var authPacket = new Packet((byte)PacketType.Auth, JObject.FromObject(registerData));
        SendAndHandleAuthResponse(authPacket);
    }


    /*
        Debug.Log("Attempting to log in.");
        if (File.Exists(AuthTokenFilePath))
        {
            AccountTokenLogin();
        }
    */
    public void AccountTokenLogin()
    {
        string token = GetSavedAuthToken();

        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No valid token found.");
            AuthenticationFailed?.Invoke();
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
            File.WriteAllText(AuthTokenPath, token);
            Debug.Log("Authentication token saved.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save auth token: {ex.Message}");
            AuthenticationFailed?.Invoke();
        }
    }

    private string GetSavedAuthToken()
    {
        try
        {
            if (File.Exists(AuthTokenPath))
            {
                string token = File.ReadAllText(AuthTokenPath);
                if (token.StartsWith("PXAT_"))
                {
                    return token;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to read auth token: {ex.Message}");
            AuthenticationFailed?.Invoke();
        }

        return string.Empty;
    }

    private void SendAndHandleAuthResponse(Packet authPacket)
    {
        try
        {
            authPacket.Send(serverSocket);
            Packet responsePacket = Packet.Receive(serverSocket);
            ProcessAuthResponse(responsePacket);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during authentication: {ex.Message}");
            AuthenticationFailed?.Invoke();
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
        switch (responseType)
        {
            case "REGISTER":
                Debug.Log("Registration successful! Please log in.");
                return;

            case "LOGIN":
                SaveAuthToken((string)response.Data["token"]);
                //SetupAccount(response);
                break;

            case "TLOGIN":
                //SetupAccount(response);
                break;

            default:
                Debug.LogError("Unexpected success response type.");
                return;
        }

        Debug.Log("Authentication successful.");
        AuthenticationSuccess?.Invoke(response);
    }

    private void HandleAuthFailure(string responseType)
    {
        Debug.LogError($"Authentication failed. Type: {responseType}");
        if (responseType == "TLOGIN")
        {
            File.Delete(AuthTokenPath);
        }
        AuthenticationFailed?.Invoke();
    }
    /*s
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
    */
}
