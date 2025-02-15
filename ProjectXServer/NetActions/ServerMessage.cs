using ProjectXServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ProjectXServer.NetActions
{
    internal class ServerMessage
    {
        public ConnectedClient Client;
        public string Action;
        public Dictionary<string, object> Parameters;
    }
}
