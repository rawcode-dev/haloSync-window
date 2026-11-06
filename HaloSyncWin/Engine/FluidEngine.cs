using System;
using HaloSyncWin.Models;

namespace HaloSyncWin.Engine
{
    public class FluidConfiguration
    {
        public float BaseSmoothing { get; set; } = 0.55f;
        public float MotionSensitivity { get; set; } = 1.2f;
        public float MinimumSmoothing { get; set; } = 0.05f;
        public float MaximumSmoothing { get; set; } = 0.95f;
    }

    /// <summary>
    /// Motion-aware adaptive temporal smoothing engine.
    /// Provides buttery-smooth color transitions during static scenes and desktop usage,
    /// while reacting instantly with minimal smoothing during fast gaming or movie motion.
    /// </summary>
    public sealed class FluidEngine
    {
        private LEDColor[]? _previousFrame;
        private readonly object _lock = new object();

        public FluidConfiguration Config { get; set; } = new FluidConfiguration();
        public float CurrentSmoothingFactor { get; private set; } = 0.55f;
        public float LastMotionScore { get; private set; } = 0f;

        public FluidEngine(FluidConfiguration? config = null)
        {
            if (config != null) Config = config;
            CurrentSmoothingFactor = Config.BaseSmoothing;
        }

        public LEDFrame Smooth(LEDFrame rawFrame)
        {
            lock (_lock)
            {
                var newColors = rawFrame.Colors;

                if (_previousFrame == null || _previousFrame.Length != newColors.Length)
                {
                    _previousFrame = (LEDColor[])newColors.Clone();
                    return rawFrame;
                }

                // 1. Compute aggregate motion score across all LEDs
                float motionScore = ComputeMotionScore(_previousFrame, newColors);
                LastMotionScore = motionScore;

                // 2. Adaptive smoothing factor: higher motion -> lower smoothing (faster response)
                float rawSmoothing = Config.BaseSmoothing - (motionScore * Config.MotionSensitivity);
                float adaptiveFactor = Math.Clamp(rawSmoothing, Config.MinimumSmoothing, Config.MaximumSmoothing);
                CurrentSmoothingFactor = adaptiveFactor;

                // 3. Interpolate (LERP)
                float blendFactor = 1.0f - adaptiveFactor;
                var smoothed = new LEDColor[newColors.Length];

                for (int i = 0; i < newColors.Length; i++)
                {
                    smoothed[i] = _previousFrame[i].Lerp(newColors[i], blendFactor);
                }

                _previousFrame = smoothed;

                return new LEDFrame(smoothed, rawFrame.Source);
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _previousFrame = null;
                CurrentSmoothingFactor = Config.BaseSmoothing;
                LastMotionScore = 0f;
            }
        }

        private static float ComputeMotionScore(LEDColor[] prev, LEDColor[] next)
        {
            if (prev.Length == 0) return 0f;

            float totalDelta = 0f;
            for (int i = 0; i < prev.Length; i++)
            {
                totalDelta += Math.Abs(prev[i].Red - next[i].Red) +
                              Math.Abs(prev[i].Green - next[i].Green) +
                              Math.Abs(prev[i].Blue - next[i].Blue);
            }

            float maxPossibleDelta = prev.Length * 3.0f;
            return Math.Min(totalDelta / maxPossibleDelta, 1.0f);
        }
    }
}

