using System;

namespace HaloSyncWin.Models
{
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    public enum ControllerProtocol
    {
        DDP,
        UDPRaw,
        WLEDJSON
    }

    public class DeviceInfo
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "WLED Controller";
        public string ProductName { get; set; } = "WLED";
        public string Address { get; set; } = string.Empty;
        public ushort Port { get; set; } = 4048;
        public string? MdnsHostname { get; set; }
        public int LedCount { get; set; } = 113;
        public string FirmwareVersion { get; set; } = "Unknown";
        public ControllerProtocol ActiveProtocol { get; set; } = ControllerProtocol.DDP;
        public ConnectionStatus ConnectionStatus { get; set; } = ConnectionStatus.Disconnected;
        public double? LatencyMs { get; set; }
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

        public ushort DefaultPort => ActiveProtocol switch
        {
            ControllerProtocol.DDP => 4048,
            ControllerProtocol.UDPRaw => 21324,
            ControllerProtocol.WLEDJSON => 80,
            _ => 4048
        };
    }
}

