using ProjectXServer.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ProjectXServer.NetActions
{
    internal class GetPlayerDataCommand : ICommand
    {
        public void Execute(ServerMessage message)
        {
            Console.WriteLine("[ACTION] Executing GetPlayerDataCommand");
            Console.WriteLine($"[ACTION] Executed by: {message.Client.Account.Id}");
            Console.WriteLine($"[ACTION] Parameters: {string.Join(", ", message.Parameters)}");
            Packet response = new Packet((byte)PacketType.ActionResult, $"{message.Client.Player.Id} {message.Client.Player.LightPoints} {message.Client.Player.PremPoints} {message.Client.Player.MasteryPoints} {message.Client.Player.CurrentSpecialSkillCharge} {message.Client.Player.CurrentSpecialShieldCharge}");
            response.Send(message.Client.Socket);
        }
    }
}
