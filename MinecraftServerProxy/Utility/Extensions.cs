using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace MinecraftServerProxy.Utility
{
    public static class Extensions
    {
        // Sourced from: https://wiki.vg/Protocol#VarInt_and_VarLong
        public static int ReadNextVarInt(this byte[] buffer, ref int offset)
        {
            int numRead = 0;
            int result = 0;
            byte read;

            do
            {
                read = buffer[offset + numRead];
                int value = (read & 0b01111111);
                result |= (value << (7 * numRead));

                numRead++;
                if (numRead > 5)
                {
                    throw new Exception("VarInt is too big");
                }
            } while ((read & 0b10000000) != 0);

            offset += numRead;

            return result;
        }

        // Based on: https://github.com/Ktlo/MCSHub/blob/master/MinecraftServerHub/Packet/Packet.cs#L85
        public static string ReadNextString(this byte[] buffer, ref int offset)
        {
            // Size of the following String
            int stringSize = buffer.ReadNextVarInt(ref offset);

            // The actual string
            byte[] stringBuffer = buffer.Skip(offset).Take(stringSize).ToArray();

            offset += stringSize;

            return Encoding.UTF8.GetString(stringBuffer);
        }

        public static ushort ReadNextUnsignedShort(this byte[] buffer, ref int offset)
        {
            byte[] ushortBuffer = new byte[2];

            // Data in the NetworkStream is Big-Endian and BitConverter is Little-Endian. 
            // Need to reverse the order of the Bytes to get a correct number
            ushortBuffer[0] = buffer[offset + 1];
            ushortBuffer[1] = buffer[offset];

            offset += 2;

            return BitConverter.ToUInt16(ushortBuffer, 0);
        }
    }
}
