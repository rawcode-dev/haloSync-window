using System;
using System.Collections.Generic;
using HaloSyncWin.Models;
using SharpDX.DXGI;

namespace HaloSyncWin.Capture
{
    public static class DisplayDiscovery
    {
        public static List<DisplayInfo> GetDisplays()
        {
            var displays = new List<DisplayInfo>();

            try
            {
                using var factory = new Factory1();
                int adapterCount = factory.GetAdapterCount1();

                for (int a = 0; a < adapterCount; a++)
                {
                    using var adapter = factory.GetAdapter1(a);
                    int outputCount = adapter.GetOutputCount();

                    for (int o = 0; o < outputCount; o++)
                    {
                        using var output = adapter.GetOutput(o);
                        var desc = output.Description;

                        int width = Math.Abs(desc.DesktopBounds.Right - desc.DesktopBounds.Left);
                        int height = Math.Abs(desc.DesktopBounds.Bottom - desc.DesktopBounds.Top);

                        // If output is not attached to desktop, skip
                        if (!desc.IsAttachedToDesktop || width <= 0 || height <= 0)
                            continue;

                        // Display name cleaned up
                        string cleanName = desc.DeviceName.Replace(@"\\.\", "");
                        bool isPrimary = desc.DesktopBounds.Left == 0 && desc.DesktopBounds.Top == 0;

                        displays.Add(new DisplayInfo
                        {
                            DeviceName = desc.DeviceName,
                            Name = $"Display {displays.Count + 1} ({cleanName})",
                            Width = width,
                            Height = height,
                            RefreshRate = 60, // Default baseline, updated by capture if high-refresh
                            IsPrimary = isPrimary,
                            AdapterIndex = a,
                            OutputIndex = o
                        });
                    }
                }
            }
            catch { }

            if (displays.Count == 0)
            {
                // Fallback default
                displays.Add(new DisplayInfo
                {
                    DeviceName = @"\\.\DISPLAY1",
                    Name = "Primary Display",
                    Width = 1920,
                    Height = 1080,
                    RefreshRate = 60,
                    IsPrimary = true,
                    AdapterIndex = 0,
                    OutputIndex = 0
                });
            }

            return displays;
        }
    }
}

