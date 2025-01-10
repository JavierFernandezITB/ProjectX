using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Network/Player", order = 2)]
public class PlayerSO : ScriptableObject
{
    public int playerId;
    public int lightCurrency;
    public int premiumCurrency;
    public int masteryPoints;
    public int specialSkillCharge;
    public int specialShieldCharge;
}
