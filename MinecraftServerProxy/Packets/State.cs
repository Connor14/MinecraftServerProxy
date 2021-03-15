using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftServerProxy.Packets
{
    public enum State
    {
        Handshaking,
        Status,
        Login,
        Play
    }
}
