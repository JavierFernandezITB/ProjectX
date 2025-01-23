using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LightTowerData", menuName = "Network/LightTowerData", order = 2)]
public class LightTowerSO : ScriptableObject
{
    public int TowerNum;
    public DateTime InitDate;
    public float Multiplier;
    public int BaseAmount;
}

