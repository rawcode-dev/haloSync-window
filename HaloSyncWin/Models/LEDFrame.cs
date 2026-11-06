using System;

namespace HaloSyncWin.Models
{
    public enum FrameSource
    {
        ScreenCapture,
        Effect,
        Calibration,
        Black
    }

    /// <summary>
    /// Represents an immutable frame of LED colors destined for output.
    /// </summary>
    public sealed class LEDFrame
    {
        public LEDColor[] Colors { get; }
        public DateTime Timestamp { get; }
        public FrameSource Source { get; }

        public int LedCount => Colors.Length;

        public LEDFrame(LEDColor[] colors, FrameSource source = FrameSource.ScreenCapture)
        {
            Colors = colors ?? throw new ArgumentNullException(nameof(colors));
            Timestamp = DateTime.UtcNow;
            Source = source;
        }

        public LEDFrame(int count, FrameSource source = FrameSource.ScreenCapture)
        {
            Colors = new LEDColor[count];
            Timestamp = DateTime.UtcNow;
            Source = source;
        }

        public static LEDFrame Black(int count)
        {
            var colors = new LEDColor[count];
            for (int i = 0; i < count; i++)
            {
                colors[i] = LEDColor.Black;
            }
            return new LEDFrame(colors, FrameSource.Black);
        }
    }
}

