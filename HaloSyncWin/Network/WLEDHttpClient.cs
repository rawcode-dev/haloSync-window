using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HaloSyncWin.Models;

namespace HaloSyncWin.Network
{
    /// <summary>
    /// Communicates with WLED's HTTP JSON API for device identification, power control,
    /// solid colors, and hardware-offloaded standalone effects.
    /// </summary>
    public static class WLEDHttpClient
    {
        private static readonly HttpClient HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(3)
        };

        public static async Task<DeviceInfo?> QueryInfoAsync(string host)
        {
            try
            {
                string url = $"http://{host}/json/info";
                string json = await HttpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "WLED Controller" : "WLED Controller";
                string ver = root.TryGetProperty("ver", out var verProp) ? verProp.GetString() ?? "Unknown" : "Unknown";

                int ledCount = 113;
                if (root.TryGetProperty("leds", out var ledsProp) &&
                    ledsProp.TryGetProperty("count", out var countProp))
                {
                    ledCount = countProp.GetInt32();
                }

                return new DeviceInfo
                {
                    Name = name,
                    Address = host,
                    LedCount = ledCount,
                    FirmwareVersion = ver,
                    ActiveProtocol = ControllerProtocol.DDP,
                    ConnectionStatus = ConnectionStatus.Connected,
                    LastSeenAt = DateTime.UtcNow
                };
            }
            catch
            {
                return null;
            }
        }

        public static async Task<bool> SetPowerAsync(string host, bool isOn)
        {
            try
            {
                string url = $"http://{host}/json/state";
                var payload = new { on = isOn };
                string json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await HttpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> SetSolidColorAsync(string host, float r, float g, float b, float brightness)
        {
            try
            {
                string url = $"http://{host}/json/state";
                int byteR = (int)Math.Round(Math.Clamp(r, 0f, 1f) * 255.0f);
                int byteG = (int)Math.Round(Math.Clamp(g, 0f, 1f) * 255.0f);
                int byteB = (int)Math.Round(Math.Clamp(b, 0f, 1f) * 255.0f);
                int bri = (int)Math.Round(Math.Clamp(brightness, 0f, 1f) * 255.0f);

                var payload = new
                {
                    on = true,
                    bri = bri,
                    seg = new[]
                    {
                        new
                        {
                            fx = 0, // Solid effect
                            col = new[] { new[] { byteR, byteG, byteB } }
                        }
                    }
                };

                string json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await HttpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> SetHardwareEffectAsync(
            string host,
            int fxId,
            float speed,
            float intensity,
            float brightness,
            int? paletteId = null,
            float[]? color = null)
        {
            try
            {
                string url = $"http://{host}/json/state";
                int sx = (int)Math.Round(Math.Clamp(speed, 0f, 1f) * 255.0f);
                int ix = (int)Math.Round(Math.Clamp(intensity, 0f, 1f) * 255.0f);
                int bri = (int)Math.Round(Math.Clamp(brightness, 0f, 1f) * 255.0f);

                var segObj = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["fx"] = fxId,
                    ["sx"] = sx,
                    ["ix"] = ix
                };

                if (paletteId.HasValue)
                {
                    segObj["pal"] = paletteId.Value;
                }

                if (color != null && color.Length >= 3)
                {
                    int r = (int)Math.Round(Math.Clamp(color[0], 0f, 1f) * 255.0f);
                    int g = (int)Math.Round(Math.Clamp(color[1], 0f, 1f) * 255.0f);
                    int b = (int)Math.Round(Math.Clamp(color[2], 0f, 1f) * 255.0f);
                    segObj["col"] = new[] { new[] { r, g, b } };
                }

                var payload = new
                {
                    on = true,
                    bri = bri,
                    seg = new[] { segObj }
                };

                string json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await HttpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}

