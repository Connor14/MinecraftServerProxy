using MinecraftServerProxy.Utility;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftServerProxy.Packets
{
    public class Packet
    {
        public ReadOnlySequence<byte> CompletePacket { get; }

        public Packet(ReadOnlySequence<byte> completePacket)
        {
            CompletePacket = completePacket;
        }
    }
}
