using HaloSyncWin.Models;

namespace HaloSyncWin.Network.Protocols
{
    public interface ILEDOutputProtocol
    {
        string Name { get; }
        ushort DefaultPort { get; }
        int Encode(LEDFrame frame, float brightness, ColorOrder order, byte sequenceNumber, byte[] buffer);
    }
}

