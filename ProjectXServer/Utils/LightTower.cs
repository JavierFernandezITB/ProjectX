using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectXServer.Utils
{
    public class LightTower
    {
        public int PlayerId { get; set; }
        public int TowerNum { get; set; }
        public DateTime InitDate { get; set; }
        public float Multiplier { get; set; }
        public int BaseAmount { get; set; }

        public LightTower(int playerId, int towerNum, DateTime initDate, float multiplier, int baseAmount)
        {
            PlayerId = playerId;
            TowerNum = towerNum;
            InitDate = initDate;
            Multiplier = multiplier;
            BaseAmount = baseAmount;
        }
    }
}
