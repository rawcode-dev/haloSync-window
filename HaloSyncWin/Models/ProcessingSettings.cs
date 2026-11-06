using System;
using System.Numerics;

namespace HaloSyncWin.Models
{
    public class ProcessingSettings
    {
        public int SamplingDepth { get; set; } = 3;
        public int SamplesPerZone { get; set; } = 16;
        public float Gamma { get; set; } = 2.2f;
        public Vector3 WhiteBalance { get; set; } = Vector3.One;
        public Vector3 WallCompensation { get; set; } = Vector3.One;
        public float Brightness { get; set; } = 0.80f;
        public float AmbientStrength { get; set; } = 0.70f;
        public bool BlackBarDetection { get; set; } = true;
        public float BlackBarThreshold { get; set; } = 0.02f;
        public float CropTop { get; set; } = 0f;
        public float CropBottom { get; set; } = 0f;
        public float CropLeft { get; set; } = 0f;
        public float CropRight { get; set; } = 0f;
        public ColorOrder ColorOrder { get; set; } = ColorOrder.RGB;
        public CustomLayout Layout { get; set; } = new CustomLayout();
        public bool IsLayoutTestActive { get; set; } = false;

        public static ProcessingSettings From(
            AmbientMode mode,
            float brightness,
            float ambientStrength,
            Vector3 wallColor)
        {
            var p = mode.GetDefaultParameters();
            var settings = new ProcessingSettings
            {
                Brightness = brightness,
                AmbientStrength = ambientStrength
            };

            // Calculate Wall Color Compensation (subtractive math matching macOS)
            float cx = 1.0f / Math.Max(wallColor.X, 0.1f);
            float cy = 1.0f / Math.Max(wallColor.Y, 0.1f);
            float cz = 1.0f / Math.Max(wallColor.Z, 0.1f);

            float maxComp = Math.Max(cx, Math.Max(cy, cz));
            if (maxComp > 0)
            {
                settings.WallCompensation = new Vector3(cx / maxComp, cy / maxComp, cz / maxComp);
            }

            return settings;
        }
    }
}

