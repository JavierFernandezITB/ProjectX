using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LightTowerDatabase", menuName = "Network/LightTowerDatabase", order = 2)]
public class LightTowerDatabaseSO : ScriptableObject
{
    public List<LightTowerSO> unlockedTowers;
}

