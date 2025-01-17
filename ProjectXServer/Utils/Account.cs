using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ProjectXServer.Utils
{
    public class Account
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; }
        public List<int> Friends { get; set; }
        public DateTime CreatedAt { get; set; }

        // Constructor to initialize the Account object
        public Account(int id, string username, string email, DateTime createdAt)
        {
            Id = id;
            Username = username;
            Email = email;
            CreatedAt = createdAt;
            Friends = new List<int>(); // Initialize friends list
        }
    }

}
