using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AccountData", menuName = "Network/Account", order = 1)]
public class AccountSO : ScriptableObject
{
    public int playerId;
    public string username;
    public int[] friends;
}