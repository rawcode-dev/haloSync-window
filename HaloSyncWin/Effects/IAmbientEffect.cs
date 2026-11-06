using System;
using HaloSyncWin.Models;

namespace HaloSyncWin.Effects
{
    public record WLEDHardwareConfig(int FxId, int? PaletteId = null, bool UsesSolidColor = false);

    public interface IAmbientEffect
    {
        string Id { get; }
        string Name { get; }
        string Icon { get; }
        WLEDHardwareConfig HardwareConfig { get; }
        LEDFrame Next(int ledCount, double time, float brightness);
    }
}

