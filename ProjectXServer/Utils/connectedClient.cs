using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ProjectXServer.Utils
{
    internal class connectedClient
    {
        public TcpClient Socket;
        public Account Account;
        public Player Player;
    }
}
