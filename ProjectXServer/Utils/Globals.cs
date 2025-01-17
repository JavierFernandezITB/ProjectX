using System;
using System.Net;

namespace ProjectXServer.Utils
{
    internal class Globals
    {
        public static int port = 18800;
        public static IPAddress address = IPAddress.Any;
        public static int maxPlayers = 32;
        public static DateTime passwordExpiresAt = DateTime.UtcNow.AddYears(999);
        public static int maxLightReward = 150;
        public static int minLightReward = 50;
        public static float baseCollectionRange = 5f;
    }
}
