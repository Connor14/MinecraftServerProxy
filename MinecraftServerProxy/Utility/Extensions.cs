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
        /// <summary>
        /// Reads the data available in the NetworkStream into a byte[].
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        // Based on:
        //      https://stackoverflow.com/a/26058713/1984712
        //      https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.networkstream.dataavailable?view=netframework-4.8
        //      https://stackoverflow.com/questions/19387979/get-length-of-data-available-in-networkstream
        public static byte[] ReadBytes(this NetworkStream stream)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte[] buffer = new byte[1024];

                // While there is data available in the NetworkStream
                do
                {
                    // Read the data into the temporary buffer
                    // Blocks until some data is available
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    // Write the buffer into the MemoryStream using the number of bytes read to prevent writing duplicate data
                    memoryStream.Write(buffer, 0, bytesRead);
                } while (stream.DataAvailable);

                return memoryStream.ToArray();
            }
        }

        public static void WriteBytes(this NetworkStream stream, byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void Skip(this byte[] buffer, int count, ref int offset)
        {
            offset += count;
        }

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

        // Sourced from: https://wiki.vg/Protocol#VarInt_and_VarLong
        public static long ReadNextVarLong(this byte[] buffer, ref int offset)
        {
            int numRead = 0;
            long result = 0;
            byte read;
            do
            {
                read = buffer[offset + numRead];
                int value = (read & 0b01111111);
                result |= (value << (7 * numRead));

                numRead++;
                if (numRead > 10)
                {
                    throw new Exception("VarLong is too big");
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

        public static UInt16 ReadNextUInt16(this byte[] buffer, ref int offset)
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
