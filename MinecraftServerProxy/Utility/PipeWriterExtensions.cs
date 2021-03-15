using MinecraftServerProxy.Packets;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftServerProxy.Utility
{
    public static class PipeWriterExtensions
    {
        public static async Task WritePacketAsync(this PipeWriter writer, ReadOnlySequence<byte> packet, CancellationToken cancellationToken = default)
        {
            if (packet.IsEmpty)
            {
                return;
            }

            if (packet.IsSingleSegment)
            {
                await writer.WriteAsync(packet.First, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                foreach (var memory in packet)
                {
                    if (memory.IsEmpty)
                    {
                        continue;
                    }

                    await writer.WriteAsync(memory, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
