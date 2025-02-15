using Newtonsoft.Json.Linq;
using ProjectXServer.Database;
using ProjectXServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectXServer.NetActions
{
    internal class AddFriendCommand : ICommand
    {
        public async void Execute(ServerMessage message)
        {
            Console.WriteLine("[ACTION] Executing AddFriend");
            Console.WriteLine($"[ACTION] Executed by: {message.Client.Account.Id}");

            Dictionary<string, object> paramsDict = new Dictionary<string, object>() {
                { "status" , "" }
            };

            int targetId = (int)message.Parameters["target"];
            Account searchResult = await DB.GenerateAccountObject(targetId);

            if (searchResult != null && targetId != message.Client.Account.Id && !message.Client.Account.Friends.Contains(targetId))
            {
                message.Client.Account.Friends.Add(targetId);
                await DB.SaveAccountData(message.Client.Account);
                paramsDict["status"] = searchResult.Id.ToString();
            }
            else
                paramsDict["status"] = "BAD";

            Dictionary<string, object> responseData = new Dictionary<string, object>() {
                { "action", "AddFriend" },
                { "params", paramsDict }
            };

            Packet response = new Packet((byte)PacketType.ActionResult, JObject.FromObject(responseData));
            response.Send(message.Client.Socket);
        }
    }
}
