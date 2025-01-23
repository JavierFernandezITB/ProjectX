using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectXServer.Utils
{
    public class Player
    {
        // Database data.
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int LightPoints { get; set; }
        public int PremPoints { get; set; }
        public int MasteryPoints { get; set; }
        public float CurrentSpecialSkillCharge { get; set; }
        public float CurrentSpecialShieldCharge { get; set; }

        // Server-only volatile data.

        public List<LightTower> unlockedLightTowers = new List<LightTower>();
        public List<CollectableLight> collectableLights = new List<CollectableLight>();
        public int MaxCollectableLights = 10;
    }

}
