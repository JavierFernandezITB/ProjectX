using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class Player
{
    public TcpClient socket;
    public int playerId;
    public string username;

    public Player(int playerid, string _username)
    {
        playerId = playerid;
        username = _username;
    }

    public Player(TcpClient client, int playerid, string _username)
    {
        socket = client;
        playerId = playerid;
        username = _username;
    }
}
