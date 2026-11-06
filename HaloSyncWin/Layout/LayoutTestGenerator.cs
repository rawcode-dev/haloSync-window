using System.Collections.Generic;
using HaloSyncWin.Models;

namespace HaloSyncWin.Layout
{
    public static class LayoutTestGenerator
    {
        public static readonly LEDColor BottomLeftColor = new LEDColor(1f, 0f, 0f);        // Red
        public static readonly LEDColor CornerColor     = new LEDColor(1f, 1f, 1f);        // White
        public static readonly LEDColor LeftColor       = new LEDColor(0f, 1f, 0f);        // Green
        public static readonly LEDColor TopColor        = new LEDColor(0f, 0f, 1f);        // Blue
        public static readonly LEDColor RightColor      = new LEDColor(1f, 1f, 0f);        // Yellow
        public static readonly LEDColor BottomRightColor= new LEDColor(0f, 1f, 1f);        // Cyan

        public static LEDColor[] Generate(CustomLayout layout, int totalLeds)
        {
            var colors = new List<LEDColor>(layout.TotalLEDs);

            void Append(int count, LEDColor color)
            {
                for (int i = 0; i < count; i++) colors.Add(color);
            }

            Append(layout.BottomLeft, BottomLeftColor);
            Append(layout.BottomLeftCorner, CornerColor);
            Append(layout.Left, LeftColor);
            Append(layout.TopLeftCorner, CornerColor);
            Append(layout.Top, TopColor);
            Append(layout.TopRightCorner, CornerColor);
            Append(layout.Right, RightColor);
            Append(layout.BottomRightCorner, CornerColor);
            Append(layout.BottomRight, BottomRightColor);

            if (colors.Count > totalLeds)
            {
                return colors.GetRange(0, totalLeds).ToArray();
            }

            while (colors.Count < totalLeds)
            {
                colors.Add(LEDColor.Black);
            }

            return colors.ToArray();
        }
    }
}

