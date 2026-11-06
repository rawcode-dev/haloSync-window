using System;

namespace HaloSyncWin.Models
{
    /// <summary>
    /// Represents an RGB color for an LED pixel.
    /// Internal values are normalized floats in [0.0, 1.0] for precision color science,
    /// with byte accessors for direct hardware wire protocol encoding.
    /// </summary>
    public readonly struct LEDColor : IEquatable<LEDColor>
    {
        public readonly float Red;
        public readonly float Green;
        public readonly float Blue;

        public static readonly LEDColor Black = new LEDColor(0f, 0f, 0f);
        public static readonly LEDColor White = new LEDColor(1f, 1f, 1f);

        public LEDColor(float red, float green, float blue)
        {
            Red = Math.Clamp(red, 0f, 1f);
            Green = Math.Clamp(green, 0f, 1f);
            Blue = Math.Clamp(blue, 0f, 1f);
        }

        public LEDColor(byte r, byte g, byte b)
        {
            Red = r / 255.0f;
            Green = g / 255.0f;
            Blue = b / 255.0f;
        }

        public byte R => (byte)Math.Clamp((int)Math.Round(Red * 255.0f), 0, 255);
        public byte G => (byte)Math.Clamp((int)Math.Round(Green * 255.0f), 0, 255);
        public byte B => (byte)Math.Clamp((int)Math.Round(Blue * 255.0f), 0, 255);

        public LEDColor Scaled(float brightness)
        {
            return new LEDColor(Red * brightness, Green * brightness, Blue * brightness);
        }

        public LEDColor Lerp(LEDColor target, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return new LEDColor(
                Red + (target.Red - Red) * t,
                Green + (target.Green - Green) * t,
                Blue + (target.Blue - Blue) * t
            );
        }

        public (byte c0, byte c1, byte c2) ToBytes(ColorOrder order)
        {
            byte r = R;
            byte g = G;
            byte b = B;

            return order switch
            {
                ColorOrder.RGB => (r, g, b),
                ColorOrder.GRB => (g, r, b),
                ColorOrder.BGR => (b, g, r),
                ColorOrder.RBG => (r, b, g),
                _ => (r, g, b)
            };
        }

        public bool Equals(LEDColor other)
        {
            return Math.Abs(Red - other.Red) < 0.001f &&
                   Math.Abs(Green - other.Green) < 0.001f &&
                   Math.Abs(Blue - other.Blue) < 0.001f;
        }

        public override bool Equals(object? obj) => obj is LEDColor other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Red, Green, Blue);
        public static bool operator ==(LEDColor left, LEDColor right) => left.Equals(right);
        public static bool operator !=(LEDColor left, LEDColor right) => !left.Equals(right);
    }
}

