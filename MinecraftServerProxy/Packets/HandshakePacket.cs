using MinecraftServerProxy.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftServerProxy.Packets
{
    public class HandshakePacket : Packet
    {
        public int ProtocolVersion { get; set; }
        public string ServerAddress { get; set; }
        public int Port { get; set; }
        public int NextState { get; set; }

        public HandshakePacket(byte[] bytes) : base(bytes)
        {
            ProtocolVersion = bytes.ReadNextVarInt(ref Offset); // Protocol Version

            ServerAddress = bytes.ReadNextString(ref Offset); // Server Addresss

            Port = bytes.ReadNextUInt16(ref Offset); // Server Port
            NextState = bytes.ReadNextVarInt(ref Offset); // Next State
        }

    }
}
