using MinecraftServerProxy.Utility;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftServerProxy.Packets
{
    public class HandshakePacket : Packet
    {
        public int ProtocolVersion { get; }
        public string ServerAddress { get; }
        public ushort Port { get; }
        public State NextState { get; }


        public HandshakePacket(ReadOnlySequence<byte> completePacket, int protocolVersion, string serverAddress, ushort port, State nextState)
            : base(completePacket)
        {
            ProtocolVersion = protocolVersion;
            ServerAddress = serverAddress;
            Port = port;
            NextState = nextState;
        }

        public static HandshakePacket Create(ReadOnlySequence<byte> packet)
        {
            var sequenceReader = new SequenceReader<byte>(packet);

            // Standard uncompressed packet
            sequenceReader.TryReadVarInt(out int length, out int read1);
            sequenceReader.TryReadVarInt(out int packetID, out int read2);

            // Handshake
            sequenceReader.TryReadVarInt(out int protocolVersion, out int read3);
            sequenceReader.TryReadString(out string serverAddress, out int read4);
            sequenceReader.TryReadUShort(out ushort port, out int read5);
            sequenceReader.TryReadVarInt(out int nextState, out int read6);

            // Chop off the \0FML\0 or \0FML2\0 if it exsits
            var indexOfNullChar = serverAddress.IndexOf('\0');

            if (indexOfNullChar != -1)
            {
                serverAddress = serverAddress.Substring(0, indexOfNullChar);
            }

            return new HandshakePacket(packet, protocolVersion, serverAddress, port, (State)nextState);
        }
    }
}
