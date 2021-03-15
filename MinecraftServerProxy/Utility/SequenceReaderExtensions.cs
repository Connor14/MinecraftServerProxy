using System;
using System.Buffers;
using System.Text;

namespace MinecraftServerProxy.Utility
{
    public static class SequenceReaderExtensions
    {
        // Sourced from: https://wiki.vg/Protocol#VarInt_and_VarLong
        public static bool TryReadVarInt(this ref SequenceReader<byte> reader, out int value, out int bytesRead)
        {
            int numRead = 0;
            int result = 0;

            byte read;
            do
            {
                if (!reader.TryRead(out read))
                {
                    //throw new Exception("Could not read from SequenceReader");
                    value = default;
                    bytesRead = default;
                    return false;
                }

                int val = (read & 0b01111111);
                result |= (val << (7 * numRead));

                numRead++;
                if (numRead > 5)
                {
                    throw new Exception("VarInt is too big");
                }

            } while ((read & 0b10000000) != 0);

            value = result;
            bytesRead = numRead;
            return true;
        }

        // Based on: https://github.com/Ktlo/MCSHub/blob/master/MinecraftServerHub/Packet/Packet.cs#L85
        public static bool TryReadString(this ref SequenceReader<byte> reader, out string value, out int bytesRead)
        {
            if (!reader.TryReadVarInt(out int stringSize, out int bytesReadForStringSize))
            {
                value = null;
                bytesRead = bytesReadForStringSize;
                return false;
            }

            if (reader.UnreadSequence.Length < stringSize)
            {
                value = null;
                bytesRead = bytesReadForStringSize;
                return false;
            }

            var str = reader.UnreadSequence.Slice(0, stringSize);
            reader.Advance(stringSize);

            value = Encoding.UTF8.GetString(str);
            bytesRead = bytesReadForStringSize + stringSize;
            return true;
        }

        public static bool TryReadUShort(this ref SequenceReader<byte> reader, out ushort value, out int bytesRead)
        {
            if (reader.UnreadSequence.Length < 2)
            {
                value = 0;
                bytesRead = 0;
                return false;
            }

            // This data is Big-Endian (most significant first)
            reader.TryRead(out byte first); // most significant
            reader.TryRead(out byte second); // least significant

            value = (ushort)((first << 8) + second);
            bytesRead = 2;
            return true;
        }
    }
}
