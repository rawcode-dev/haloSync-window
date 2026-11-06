using System;
using HaloSyncWin.Models;

namespace HaloSyncWin.Effects
{
    public sealed class RainbowEffect : IAmbientEffect
    {
        public string Id => "com.halosync.effect.rainbow";
        public string Name => "Rainbow";
        public string Icon => "🌈";
        public WLEDHardwareConfig HardwareConfig => new WLEDHardwareConfig(FxId: 9, PaletteId: 11);

        public LEDFrame Next(int ledCount, double time, float brightness)
        {
            var colors = new LEDColor[ledCount];
            float speed = 0.5f;

            for (int i = 0; i < ledCount; i++)
            {
                float hue = (float)((time * speed + (double)i / ledCount) % 1.0);
                colors[i] = ColorFromHsv(hue, 1.0f, brightness);
            }

            return new LEDFrame(colors, FrameSource.Effect);
        }

        private static LEDColor ColorFromHsv(float h, float s, float v)
        {
            float c = v * s;
            float x = c * (1.0f - Math.Abs((h * 6.0f) % 2.0f - 1.0f));
            float m = v - c;

            float r = 0, g = 0, b = 0;
            int segment = (int)(h * 6.0f);

            switch (segment)
            {
                case 0: r = c; g = x; b = 0; break;
                case 1: r = x; g = c; b = 0; break;
                case 2: r = 0; g = c; b = x; break;
                case 3: r = 0; g = x; b = c; break;
                case 4: r = x; g = 0; b = c; break;
                default: r = c; g = 0; b = x; break;
            }

            return new LEDColor(r + m, g + m, b + m);
        }
    }

    public sealed class FireEffect : IAmbientEffect
    {
        public string Id => "com.halosync.effect.fire";
        public string Name => "Fire";
        public string Icon => "🔥";
        public WLEDHardwareConfig HardwareConfig => new WLEDHardwareConfig(FxId: 117, PaletteId: 35);

        public LEDFrame Next(int ledCount, double time, float brightness)
        {
            var colors = new LEDColor[ledCount];
            float t = (float)time * 1.5f;

            for (int i = 0; i < ledCount; i++)
            {
                float x = (float)i / ledCount;
                float flicker = (float)(Math.Sin(x * 18.0f + t * 6.7f) * 0.15 +
                                        Math.Sin(x * 7.0f - t * 4.3f) * 0.20 + 0.65);
                float v = Math.Clamp(flicker * brightness, 0f, 1f);

                float r = Math.Min(v * 2.0f, 1.0f);
                float g = Math.Max(v * 2.0f - 1.0f, 0f) * 0.7f;
                float b = 0f;

                colors[i] = new LEDColor(r, g, b);
            }

            return new LEDFrame(colors, FrameSource.Effect);
        }
    }

    public sealed class AuroraEffect : IAmbientEffect
    {
        public string Id => "com.halosync.effect.aurora";
        public string Name => "Aurora";
        public string Icon => "🌌";
        public WLEDHardwareConfig HardwareConfig => new WLEDHardwareConfig(FxId: 38, PaletteId: 38);

        public LEDFrame Next(int ledCount, double time, float brightness)
        {
            var colors = new LEDColor[ledCount];
            float t = (float)time * 0.6f;

            for (int i = 0; i < ledCount; i++)
            {
                float x = (float)i / ledCount;
                float wave1 = (float)(Math.Sin(x * 6.0f + t * 1.2f) * 0.5 + 0.5);
                float wave2 = (float)(Math.Cos(x * 4.0f - t * 0.8f) * 0.5 + 0.5);

                float r = wave1 * 0.2f * brightness;
                float g = (wave1 * 0.8f + wave2 * 0.2f) * brightness;
                float b = (wave2 * 0.9f + wave1 * 0.3f) * brightness;

                colors[i] = new LEDColor(r, g, b);
            }

            return new LEDFrame(colors, FrameSource.Effect);
        }
    }

    public sealed class OceanEffect : IAmbientEffect
    {
        public string Id => "com.halosync.effect.ocean";
        public string Name => "Ocean";
        public string Icon => "🌊";
        public WLEDHardwareConfig HardwareConfig => new WLEDHardwareConfig(FxId: 101, PaletteId: 44);

        public LEDFrame Next(int ledCount, double time, float brightness)
        {
            var colors = new LEDColor[ledCount];
            float t = (float)time * 0.8f;

            for (int i = 0; i < ledCount; i++)
            {
                float x = (float)i / ledCount;
                float wave = (float)(Math.Sin(x * 8.0f + t * 2.0f) * 0.4 + 0.6);

                float r = 0.05f * wave * brightness;
                float g = (0.3f + 0.4f * wave) * brightness;
                float b = (0.7f + 0.3f * wave) * brightness;

                colors[i] = new LEDColor(r, g, b);
            }

            return new LEDFrame(colors, FrameSource.Effect);
        }
    }

    public sealed class PulseEffect : IAmbientEffect
    {
        public string Id => "com.halosync.effect.pulse";
        public string Name => "Pulse";
        public string Icon => "💓";
        public WLEDHardwareConfig HardwareConfig => new WLEDHardwareConfig(FxId: 1);

        public LEDFrame Next(int ledCount, double time, float brightness)
        {
            var colors = new LEDColor[ledCount];
            float pulse = (float)(Math.Sin(time * 3.0) * 0.5 + 0.5) * brightness;

            for (int i = 0; i < ledCount; i++)
            {
                colors[i] = new LEDColor(pulse * 0.8f, pulse * 0.2f, pulse * 1.0f);
            }

            return new LEDFrame(colors, FrameSource.Effect);
        }
    }

    public sealed class BreathingEffect : IAmbientEffect
    {
        public string Id => "com.halosync.effect.breathing";
        public string Name => "Breathing";
        public string Icon => "🌬️";
        public WLEDHardwareConfig HardwareConfig => new WLEDHardwareConfig(FxId: 2);

        public LEDFrame Next(int ledCount, double time, float brightness)
        {
            var colors = new LEDColor[ledCount];
            float breath = (float)(Math.Sin(time * 1.2) * 0.45 + 0.55) * brightness;

            for (int i = 0; i < ledCount; i++)
            {
                colors[i] = new LEDColor(breath * 0.9f, breath * 0.6f, breath * 0.2f);
            }

            return new LEDFrame(colors, FrameSource.Effect);
        }
    }

    public sealed class StaticEffect : IAmbientEffect
    {
        public string Id => "com.halosync.effect.static";
        public string Name => "Solid Color";
        public string Icon => "💡";
        public WLEDHardwareConfig HardwareConfig => new WLEDHardwareConfig(FxId: 0, UsesSolidColor: true);

        public LEDFrame Next(int ledCount, double time, float brightness)
        {
            var colors = new LEDColor[ledCount];
            var col = new LEDColor(1.0f, 0.4f, 0.0f).Scaled(brightness); // Orange default

            for (int i = 0; i < ledCount; i++)
            {
                colors[i] = col;
            }

            return new LEDFrame(colors, FrameSource.Effect);
        }
    }

    public sealed class ColorCycleEffect : IAmbientEffect
    {
        public string Id => "com.halosync.effect.colorcycle";
        public string Name => "Color Cycle";
        public string Icon => "🎨";
        public WLEDHardwareConfig HardwareConfig => new WLEDHardwareConfig(FxId: 8);

        public LEDFrame Next(int ledCount, double time, float brightness)
        {
            var colors = new LEDColor[ledCount];
            float hue = (float)((time * 0.2) % 1.0);
            float c = brightness;
            float x = c * (1.0f - Math.Abs((hue * 6.0f) % 2.0f - 1.0f));

            float r = 0, g = 0, b = 0;
            int seg = (int)(hue * 6.0f);
            switch (seg)
            {
                case 0: r = c; g = x; b = 0; break;
                case 1: r = x; g = c; b = 0; break;
                case 2: r = 0; g = c; b = x; break;
                case 3: r = 0; g = x; b = c; break;
                case 4: r = x; g = 0; b = c; break;
                default: r = c; g = 0; b = x; break;
            }

            var col = new LEDColor(r, g, b);
            for (int i = 0; i < ledCount; i++)
            {
                colors[i] = col;
            }

            return new LEDFrame(colors, FrameSource.Effect);
        }
    }
}

