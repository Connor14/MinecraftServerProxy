using MinecraftServerProxy.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftServerProxy.Packets
{
    public class Packet
    {
        protected int Offset;

        public byte[] Bytes { get; set; }

        public int Length { get; set; }
        public int PacketID { get; set; }

        public Packet(byte[] bytes)
        {
            Bytes = bytes;
            Length = bytes.ReadNextVarInt(ref Offset); // Length of the packet
            PacketID = bytes.ReadNextVarInt(ref Offset); // The type of packet
        }

        /// <summary>
        /// Reads the byte[] and returns the Packet ID
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static int GetPacketID(byte[] bytes)
        {
            int offset = 0;

            int length = bytes.ReadNextVarInt(ref offset); // Length of the packet
            int packetID = bytes.ReadNextVarInt(ref offset); // The type of packet

            return packetID;
        }
    }
}
