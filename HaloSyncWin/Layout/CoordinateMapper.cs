using System;
using System.Collections.Generic;
using System.Numerics;
using HaloSyncWin.Models;

namespace HaloSyncWin.Layout
{
    public static class CoordinateMapper
    {
        /// <summary>
        /// Maps a 9-segment physical LED layout into normalized 2D GPU coordinates [0.0, 1.0].
        /// Applies manual crop boundaries (0.0 to 0.5) to compensate for letterbox borders.
        /// </summary>
        public static Vector2[] Map(
            CustomLayout layout,
            int totalLeds,
            float cropTop = 0f,
            float cropBottom = 0f,
            float cropLeft = 0f,
            float cropRight = 0f)
        {
            var coordinates = new List<Vector2>(layout.TotalLEDs);

            float minX = Math.Clamp(cropLeft, 0f, 1f);
            float maxX = Math.Clamp(1.0f - cropRight, 0f, 1f);
            float minY = Math.Clamp(cropTop, 0f, 1f);
            float maxY = Math.Clamp(1.0f - cropBottom, 0f, 1f);

            var tl = new Vector2(minX, minY);
            var tr = new Vector2(maxX, minY);
            var br = new Vector2(maxX, maxY);
            var bl = new Vector2(minX, maxY);
            var bc = new Vector2(minX + (maxX - minX) / 2.0f, maxY);

            void AppendSegment(int count, Vector2 start, Vector2 end)
            {
                if (count <= 0) return;
                if (count == 1)
                {
                    coordinates.Add(start);
                    return;
                }

                for (int i = 0; i < count; i++)
                {
                    float fraction = (float)i / (count - 1);
                    float x = start.X + (end.X - start.X) * fraction;
                    float y = start.Y + (end.Y - start.Y) * fraction;
                    coordinates.Add(new Vector2(x, y));
                }
            }

            // Physical wiring sequence (counter-clockwise starting bottom-center)
            AppendSegment(layout.BottomLeft, bc, bl);
            AppendSegment(layout.BottomLeftCorner, bl, bl);
            AppendSegment(layout.Left, bl, tl);
            AppendSegment(layout.TopLeftCorner, tl, tl);
            AppendSegment(layout.Top, tl, tr);
            AppendSegment(layout.TopRightCorner, tr, tr);
            AppendSegment(layout.Right, tr, br);
            AppendSegment(layout.BottomRightCorner, br, br);
            AppendSegment(layout.BottomRight, br, bc);

            // Safety clamp to match totalLeds
            if (coordinates.Count > totalLeds)
            {
                return coordinates.GetRange(0, totalLeds).ToArray();
            }

            while (coordinates.Count < totalLeds)
            {
                coordinates.Add(coordinates.Count > 0 ? coordinates[^1] : bc);
            }

            return coordinates.ToArray();
        }
    }
}

