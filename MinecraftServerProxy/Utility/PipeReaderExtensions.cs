using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftServerProxy.Utility
{
    public static class PipeReaderExtensions
    {
        // Based on: https://blog.marcgravell.com/2018/07/pipe-dreams-part-2.html
        public static async Task<ReadOnlySequence<byte>> ReadPacketAsync(this PipeReader reader, CancellationToken cancellationToken = default)
        {
            // Continue to read until we find our properly formatted message
            while (true)
            {
                var read = await reader.ReadAsync(cancellationToken);

                if (read.IsCanceled)
                    throw new OperationCanceledException("Read canceled");

                var buffer = read.Buffer;

                // If we found a complete packet, return it
                if (TryReadPacket(buffer, out ReadOnlySequence<byte> packet, out SequencePosition consumedTo))
                {
                    // Once AdvanceTo is run, we cannot use our the original buffer (or anything that came from it - like a slice)
                    reader.AdvanceTo(consumedTo);

                    return packet;
                }

                // Otherwise, continue reading
                reader.AdvanceTo(buffer.Start, buffer.End);

                if (read.IsCompleted)
                    throw new Exception("Reader completed");
            }
        }

        /// <summary>
        /// Tries to read a complete packet from the buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="packet">A copy of the packet from the buffer.</param>
        /// <param name="consumedTo"></param>
        /// <returns></returns>
        // Based on: https://blog.marcgravell.com/2018/07/pipe-dreams-part-2.html
        private static bool TryReadPacket(in ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> packet, out SequencePosition consumedTo)
        {
            var reader = new SequenceReader<byte>(buffer);

            // ==============================
            // NOTE: Since this code is only used for reading the HandshakePacket, we don't need to worry about other special cases yet
            // ==============================

            // Check to see if the first byte is the Legacy Ping (which uses a non-standard format)
            //if (reader.TryPeek(out byte peekedFirstByte))
            //{
            //    if (peekedFirstByte == 0xFE)
            //    {
            //        Console.WriteLine("Legacy Ping");

            //        if (reader.Length < 2)
            //        {
            //            packet = default;
            //            consumedTo = default;
            //            return false;
            //        }

            //        reader.TryRead(out byte firstByte);
            //        reader.TryRead(out byte secondByte);

            //        // Make a copy of the packet so we can send it off later
            //        packet = new ReadOnlySequence<byte>(new byte[] { firstByte, secondByte });
            //        consumedTo = buffer.GetPosition(2, buffer.Start); // Using buffer.Start seems to make a difference compared to completePacketLength alone...

            //        return true;
            //    }
            //}

            // Try to parse the length of the PacketID + Data
            // If we can't get the packet length, we fail to parse the frame
            if (!reader.TryReadVarInt(out int packetLength, out int bytesReadForLength))
            {
                // NOTE: At the moment, the only way we can gracefully fail to read a VarInt is if the SequenceReader doesn't have enough data to finish reading it
                // If the VarInt is too big (greater than 5 bytes), we will get an exception
                packet = default;
                consumedTo = default;
                return false;
            }

            // According to https://wiki.vg/Protocol, we can only read a maximum of 3 bytes for the packetLength
            if (bytesReadForLength > 3)
            {
                throw new Exception("Read too many bytes for packet length");
            }

            // If the packet length is < 0 somehow, this is an invalid packet
            if (packetLength < 0)
            {
                throw new Exception("Packet length was less than 0");
            }

            // Packet length including the packet length integer's bytes
            int completePacketLength = bytesReadForLength + packetLength;

            // Maximum packet length according to https://wiki.vg/Protocol 
            if (completePacketLength > 2_097_151)
            {
                throw new Exception("Too many bytes in packet");
            }

            // If the total length of the buffer is less than the complete packet length, we don't have enough data yet
            if (buffer.Length < completePacketLength)
            {
                packet = default;
                consumedTo = default;
                return false;
            }

            // Get the portion of the buffer that is our complete packet
            var packetCopy = buffer.Slice(0, completePacketLength);

            // Make a copy of the packet so we can send it off later
            var arr = packetCopy.ToArray();
            packet = new ReadOnlySequence<byte>(arr);

            // Using buffer.Start seems to make a difference compared to complete PacketLength alone...? Maybe I'm remembering wrong.
            consumedTo = buffer.GetPosition(completePacketLength, buffer.Start);

            return true;
        }

        /// <summary>
        /// Similar to CopyToAsync but uses a custom CopyToAsync implementation to check if the PipeWriter is completed after a write.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task LinkToAsync(this PipeReader source, PipeWriter destination, CancellationToken cancellationToken = default)
        {
            return CopyToAsync(source, destination, cancellationToken);
        }

        // Source: https://github.com/dotnet/runtime/blob/main/src/libraries/System.IO.Pipelines/src/System/IO/Pipelines/PipeReader.cs
        // Slightly modified from original so IsCompleted could be checked on the write result.
        private static Task CopyToAsync(PipeReader source, PipeWriter destination, CancellationToken cancellationToken = default)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            return CopyToAsyncCore(source, destination, async (destination, memory, cancellationToken) =>
            {
                FlushResult result = await destination.WriteAsync(memory, cancellationToken).ConfigureAwait(false);

                if (result.IsCanceled)
                {
                    throw new OperationCanceledException("Write canceled");
                }

                // ADDED RETURN VALUE
                return result;
            },
            cancellationToken);
        }

        // Source: https://github.com/dotnet/runtime/blob/main/src/libraries/System.IO.Pipelines/src/System/IO/Pipelines/PipeReader.cs
        // Slightly modified from original so IsCompleted could be checked on the write result.
        private static async Task CopyToAsyncCore<TStream>(PipeReader source, TStream destination, Func<TStream, ReadOnlyMemory<byte>, CancellationToken, ValueTask<FlushResult>> writeAsync, CancellationToken cancellationToken)
        {
            while (true)
            {
                ReadResult result = await source.ReadAsync(cancellationToken).ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = result.Buffer;
                SequencePosition position = buffer.Start;
                SequencePosition consumed = position;

                try
                {
                    if (result.IsCanceled)
                    {
                        throw new OperationCanceledException("Read canceled");
                    }

                    while (buffer.TryGet(ref position, out ReadOnlyMemory<byte> memory))
                    {
                        // ADDED RETURN VALUE
                        var writeResult = await writeAsync(destination, memory, cancellationToken).ConfigureAwait(false);

                        consumed = position;

                        // ADDED
                        if (writeResult.IsCompleted)
                        {
                            break;
                        }
                    }

                    // The while loop completed succesfully, so we've consumed the entire buffer.
                    consumed = buffer.End;

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
                finally
                {
                    // Advance even if WriteAsync throws so the PipeReader is not left in the
                    // currently reading state
                    source.AdvanceTo(consumed);
                }
            }
        }
    }
}
