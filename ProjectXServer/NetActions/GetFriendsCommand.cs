using ProjectXServer.Utils;
using ProjectXServer.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ProjectXServer.NetActions
{
    internal class GetFriendsCommand : ICommand
    {
        public async void Execute(ServerMessage message)
        {
            Console.WriteLine("[ACTION] Executing GetFriends");
            Console.WriteLine($"[ACTION] Executed by: {message.Client.Account.Id}");

            Dictionary<string, object> paramsDict = new Dictionary<string, object>() {
                { "addedFriends" , new List<Dictionary<string,string>>() }
            };

            if (paramsDict.TryGetValue("addedFriends", out object responseFriends) && responseFriends is List<Dictionary<string, string>> responseFriendsList)
            {
                foreach (int friendId in message.Client.Account.Friends)
                {
                    Account friendData = await DB.GenerateAccountObject(friendId);
                    if (friendData.Friends.Contains(message.Client.Account.Id))
                    {
                        Dictionary<string, string> friendDataDict = new Dictionary<string, string>() {
                            { "friendId", friendId.ToString() },
                            { "username", friendData.Username }
                        };
                        responseFriendsList.Add(friendDataDict);
                    }
                }
            }

            Dictionary<string, object> responseData = new Dictionary<string, object>() {
                { "action", "GetFriends" },
                { "params", paramsDict }
            };

            Packet response = new Packet((byte)PacketType.ActionResult, JObject.FromObject(responseData));
            response.Send(message.Client.Socket);
        }
    }
}
