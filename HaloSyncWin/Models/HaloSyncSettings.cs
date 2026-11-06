using System;
using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaloSyncWin.Models
{
    public class HaloSyncSettings
    {
        // Basic Controls
        public float Brightness { get; set; } = 0.80f;
        public float Smoothness { get; set; } = 0.55f;
        public float AmbientStrength { get; set; } = 0.70f;

        // Active Mode & Effects
        public AmbientMode ActiveMode { get; set; } = AmbientMode.Ambient;
        public float[] SolidColorRgb { get; set; } = new float[] { 1.0f, 0.4f, 0.0f }; // Orange default
        public string? ActiveEffectId { get; set; } = null;
        public float EffectSpeed { get; set; } = 0.5f;
        public float EffectIntensity { get; set; } = 0.5f;

        // Startup & Lifecycle
        public bool LaunchAtLogin { get; set; } = false;
        public bool StartMinimized { get; set; } = false;
        public bool AutoReconnect { get; set; } = true;

        // Display & Controller
        public string? SelectedDisplayDeviceName { get; set; } = null;
        public string? LastKnownControllerAddress { get; set; } = null;
        public ControllerProtocol ActiveProtocol { get; set; } = ControllerProtocol.DDP;
        public ushort UdpPort { get; set; } = 4048;

        // Advanced Tuning
        public int SamplingDepth { get; set; } = 3;
        public float Gamma { get; set; } = 2.2f;
        public ColorOrder ColorOrder { get; set; } = ColorOrder.RGB;
        public int Rotation { get; set; } = 0;
        public float[] WallColorRgb { get; set; } = new float[] { 1.0f, 1.0f, 1.0f }; // Neutral white wall

        // Layout & Crop
        public CustomLayout Layout { get; set; } = new CustomLayout();
        public bool IsLayoutTestActive { get; set; } = false;
        public bool ShowLivePreview { get; set; } = true;
        public bool BlackBarDetection { get; set; } = true;
        public float BlackBarThreshold { get; set; } = 0.02f;
        public float CropTop { get; set; } = 0.0f;
        public float CropBottom { get; set; } = 0.0f;
        public float CropLeft { get; set; } = 0.0f;
        public float CropRight { get; set; } = 0.0f;

        [JsonIgnore]
        public Vector3 SolidColor => new Vector3(SolidColorRgb[0], SolidColorRgb[1], SolidColorRgb[2]);

        [JsonIgnore]
        public Vector3 WallColor => new Vector3(WallColorRgb[0], WallColorRgb[1], WallColorRgb[2]);

        public void SetSolidColor(float r, float g, float b)
        {
            SolidColorRgb = new float[] { r, g, b };
        }

        public void SetWallColor(float r, float g, float b)
        {
            WallColorRgb = new float[] { r, g, b };
        }

        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HaloSync");
        private static readonly string SettingsFilePath = Path.Combine(SettingsDir, "settings.json");

        public static HaloSyncSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<HaloSyncSettings>(json);
                    if (settings != null) return settings;
                }
            }
            catch { }

            return new HaloSyncSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch { }
        }
    }
}

