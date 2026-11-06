using System;
using HaloSyncWin.Models;

namespace HaloSyncWin.Network.Protocols
{
    /// <summary>
    /// Distributed Display Protocol (DDP) encoder.
    /// Fast, low-overhead 10-byte binary header followed by packed RGB triplets.
    /// Encodes directly into pre-allocated memory buffers for zero-allocation performance.
    /// </summary>
    public sealed class DDPProtocol : ILEDOutputProtocol
    {
        public string Name => "DDP";
        public ushort DefaultPort => 4048;

        public int Encode(LEDFrame frame, float brightness, ColorOrder order, byte sequenceNumber, byte[] buffer)
        {
            int dataLen = frame.Colors.Length * 3;
            int totalLen = 10 + dataLen;

            if (buffer.Length < totalLen)
            {
                throw new ArgumentException("Buffer is too small for DDP frame encoding.", nameof(buffer));
            }

            // Header (10 bytes)
            buffer[0] = 0x41;                               // Flags: VER=1, PUSH=1
            buffer[1] = sequenceNumber;                     // Sequence number (1-15)
            buffer[2] = 0x01;                               // Data type: RGB
            buffer[3] = 0x01;                               // Device ID: 1
            buffer[4] = 0x00;                               // Offset (4 bytes big-endian)
            buffer[5] = 0x00;
            buffer[6] = 0x00;
            buffer[7] = 0x00;
            buffer[8] = (byte)((dataLen >> 8) & 0xFF);      // Length Hi
            buffer[9] = (byte)(dataLen & 0xFF);             // Length Lo

            // Payload
            int offset = 10;
            var colors = frame.Colors;
            for (int i = 0; i < colors.Length; i++)
            {
                var scaled = colors[i].Scaled(brightness);
                var (c0, c1, c2) = scaled.ToBytes(order);
                buffer[offset++] = c0;
                buffer[offset++] = c1;
                buffer[offset++] = c2;
            }

            return totalLen;
        }
    }
}

