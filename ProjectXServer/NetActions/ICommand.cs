using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectXServer.NetActions
{
    interface ICommand
    {
        void Execute(ServerMessage message);
    }
}
