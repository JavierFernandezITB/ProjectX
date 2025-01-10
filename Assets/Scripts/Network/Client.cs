using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static Packet;

public class Client : MonoBehaviour
{
    public static TcpClient serverSocket;
    public static bool isClientAuthenticated = false;
    public static Player localPlayer;

    public static GameObject loginPanel;
    public static InputField usernameField;
    public static InputField passwordField;
    public static UnityEngine.UI.Button loginButton;
    public static UnityEngine.UI.Button registerButton;
    public static Packet authPacket;

    void Awake()
    {
        loginPanel = GameObject.Find("/Canvas/Panel/LOGIN_PANEL");
        usernameField = loginPanel.transform.GetChild(1).gameObject.GetComponent<InputField>();
        passwordField = loginPanel.transform.GetChild(2).gameObject.GetComponent<InputField>();
        loginButton = loginPanel.transform.GetChild(3).gameObject.GetComponent<UnityEngine.UI.Button>();
        registerButton = loginPanel.transform.GetChild(4).gameObject.GetComponent<UnityEngine.UI.Button>();
        loginButton.onClick.AddListener(AccountLogin);
        registerButton.onClick.AddListener(AccountRegister);
    }

    void Start()
    {
        Debug.Log("Starting connection with the server.");
        serverSocket = new TcpClient("127.0.0.1", 18800);

        if (serverSocket.Connected)
        {
            Debug.Log("Client connected to server. Waiting for auth.");
            // Attempt token login.
            if (File.Exists("./authTokenFile"))
                AccountTokenLogin();
        }
    }

    private void AccountLogin()
    {
        if (!serverSocket.Connected)
            return;
        Debug.Log("Crafting regular login packet.");
        authPacket = new Packet((byte)PacketType.Auth, $"LOGIN {usernameField.text} {passwordField.text}"); // Login user with credentials.
        HandleAuthResponse();
    }

    private void AccountRegister()
    {
        if (!serverSocket.Connected)
            return;
        Debug.Log("Crafting account register packet.");
        authPacket = new Packet((byte)PacketType.Auth, $"REGISTER {usernameField.text} {passwordField.text} test@itb.cat");
        HandleAuthResponse();
    }

    private void AccountTokenLogin()
    {
        if (!serverSocket.Connected)
            return;
        Debug.Log("Crafting token login packet.");
        string token = GetSavedAuthToken();
        authPacket = new Packet((byte)PacketType.Auth, $"TLOGIN {token}"); // Login user with auth token.
        HandleAuthResponse();
    }

    private void SaveAuthToken(string token)
    {
        using (StreamWriter sw = new StreamWriter("./authTokenFile"))
        {
            sw.Write(token);
            sw.Close();
        }
    }

    private string GetSavedAuthToken()
    {
        using (StreamReader sw = new StreamReader("./authTokenFile"))
        {
            string token = sw.ReadToEnd();
            if (token.StartsWith("PXAT_"))
                return token;
            return string.Empty;
        }
    }

    private void CraftPlayerObject(int pid, string uname)
    {
        localPlayer = new Player(pid, uname);
        isClientAuthenticated = true;
    }

    private void HandleAuthResponse()
    {
        Debug.Log("Sending auth packet.");
        authPacket.Send(serverSocket);
        Debug.Log("Sent! Waiting for response...");
        Packet authPacketResponse = Packet.Receive(serverSocket);
        string[] response = authPacketResponse.Data.Split(" ");
        if (response[1] == "OK")
        {
            switch (response[0])
            {
                case "REGISTER":
                    Debug.Log("Register successful! Try logging in.");
                    return;
                    break;
                case "LOGIN":
                    SaveAuthToken(response[2]);
                    CraftPlayerObject(Convert.ToInt32(response[3]), response[4]);
                    break;
                case "TLOGIN":
                    CraftPlayerObject(Convert.ToInt32(response[2]), response[3]);
                    break;
                default:
                    break;
            }
            Debug.Log("Login successful.");
            Debug.Log($"PID: {localPlayer.playerId}");
            Debug.Log($"USERNAME: {localPlayer.username}");
        }
        else
        {
            Debug.LogError($"Authentication failed. Type: {response[0]}");
            if (response[0] == "TLOGIN")
                    File.Delete("./authTokenFile");
        }
    }
}
