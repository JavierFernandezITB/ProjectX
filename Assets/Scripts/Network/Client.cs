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
    public TcpClient serverSocket;
    public bool isClientAuthenticated = false;
    public AccountSO accountData;
    public PlayerSO playerData;

    public GameObject loginPanel;
    public InputField usernameField;
    public InputField passwordField;
    public UnityEngine.UI.Button loginButton;
    public UnityEngine.UI.Button registerButton;
    public Packet authPacket;

    void Awake()
    {
        loginButton.onClick.AddListener(AccountLogin);
        registerButton.onClick.AddListener(AccountRegister);
    }

    void Start()
    {
        StartConnection();
    }

    private void StartConnection()
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
        Debug.Log(serverSocket.Connected);
        Debug.Log("Crafting regular login packet.");
        authPacket = new Packet((byte)PacketType.Auth, $"LOGIN {usernameField.text} {passwordField.text}"); // Login user with credentials.
        HandleAuthResponse();
    }

    private void AccountRegister()
    {
        if (!serverSocket.Connected)
            return;
        Debug.Log(serverSocket.Connected);
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

    private void SetupAccountObject(int pid, string uname)
    {
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
                    StartConnection();
                    return;
                case "LOGIN":
                    SaveAuthToken(response[2]);
                    //CraftPlayerObject(Convert.ToInt32(response[3]), response[4]);
                    break;
                case "TLOGIN":
                    //CraftPlayerObject(Convert.ToInt32(response[2]), response[3]);
                    break;
                default:
                    break;
            }
            Debug.Log("Login successful.");
            //Debug.Log($"PID: {localPlayer.playerId}");
            //Debug.Log($"USERNAME: {localPlayer.username}");
            loginPanel.SetActive(false);
            //StartCoroutine(NetworkCoroutine());
        }
        else
        {
            Debug.LogError($"Authentication failed. Type: {response[0]}");
            if (response[0] == "TLOGIN")
                    File.Delete("./authTokenFile");
            Debug.Log("Reconnecting...");
            StartConnection();
        }
    }

    // goofy ass test
    private IEnumerator NetworkCoroutine()
    {
        Debug.Log("IM ALIVE");
        while (true)
        {
            Debug.Log($"HE'S ALIVE? {serverSocket.Connected}");
            yield return new WaitForSeconds(1);
        }
    }
}
