using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading.Tasks;

namespace ProjectXServer.Utils
{
    public class CollectableLight
    {
        public Guid UUID { get; set; }
        public Vector3 Position { get; set; }

        public int Reward { get; set; }

        public CollectableLight(Vector3 position)
        {
            UUID = Guid.NewGuid();
            Position = position;
            Reward = new Random().Next(Globals.minLightReward, Globals.maxLightReward);
        }
    }
}
