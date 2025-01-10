using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ProjectXServer.Utils
{
    internal class Account
    {
        public TcpClient socket;
        public int playerId;
        public string username;
        public string email;
        public DateTime createdAt;

        public Account(int playerid, string _username, string _email, DateTime _createdAt)
        {
            playerId = playerid;
            username = _username;
            email = _email;
            createdAt = _createdAt;
        }

        public Account(TcpClient client, int playerid, string _username, string _email, DateTime _createdAt)
        {
            socket = client;
            playerId = playerid;
            username = _username;
            email = _email;
            createdAt = _createdAt;
        }
    }
}
