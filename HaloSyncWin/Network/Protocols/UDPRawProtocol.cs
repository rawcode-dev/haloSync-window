using System;
using HaloSyncWin.Models;

namespace HaloSyncWin.Network.Protocols
{
    /// <summary>
    /// Raw UDP protocol encoder. Simple 3-byte-per-LED format with 0 header bytes.
    /// Compatible with basic WLED / Adalight / DRGB setups.
    /// </summary>
    public sealed class UDPRawProtocol : ILEDOutputProtocol
    {
        public string Name => "UDP Raw";
        public ushort DefaultPort => 21324;

        public int Encode(LEDFrame frame, float brightness, ColorOrder order, byte sequenceNumber, byte[] buffer)
        {
            int totalLen = frame.Colors.Length * 3;

            if (buffer.Length < totalLen)
            {
                throw new ArgumentException("Buffer is too small for raw UDP frame encoding.", nameof(buffer));
            }

            int offset = 0;
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

