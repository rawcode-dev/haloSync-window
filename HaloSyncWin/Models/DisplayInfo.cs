using System;

namespace HaloSyncWin.Models
{
    public class DisplayInfo
    {
        public string DeviceName { get; set; } = string.Empty; // e.g. \\.\DISPLAY1
        public string Name { get; set; } = "Main Display";
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;
        public int RefreshRate { get; set; } = 60;
        public bool IsPrimary { get; set; } = true;
        public int AdapterIndex { get; set; } = 0;
        public int OutputIndex { get; set; } = 0;

        public override string ToString() => $"{Name} ({Width}x{Height} @ {RefreshRate}Hz)";
    }
}

