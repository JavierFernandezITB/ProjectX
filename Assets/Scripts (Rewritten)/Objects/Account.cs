using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Account
{
    public int playerId;
    public string username;
    public int[] friends;

    public Account(int _playerId, string _username)
    {
        playerId = _playerId;
        username = _username;
    }
}
