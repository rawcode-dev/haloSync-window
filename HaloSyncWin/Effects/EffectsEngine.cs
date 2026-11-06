using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HaloSyncWin.Models;

namespace HaloSyncWin.Effects
{
    public sealed class EffectsEngine : IDisposable
    {
        public static readonly IReadOnlyList<IAmbientEffect> AllEffects = new List<IAmbientEffect>
        {
            new RainbowEffect(),
            new FireEffect(),
            new AuroraEffect(),
            new OceanEffect(),
            new PulseEffect(),
            new BreathingEffect(),
            new StaticEffect(),
            new ColorCycleEffect()
        };

        private IAmbientEffect? _activeEffect;
        private CancellationTokenSource? _animationCts;
        private readonly Stopwatch _stopwatch = new Stopwatch();

        public IAmbientEffect? ActiveEffect => _activeEffect;
        public bool IsRunning => _animationCts != null;

        public event Action<LEDFrame>? FrameGenerated;

        public void Activate(string effectId)
        {
            _activeEffect = AllEffects.FirstOrDefault(e => e.Id == effectId) ?? AllEffects[0];
        }

        public void Start(int ledCount, float brightness, int targetFps = 60)
        {
            Stop();

            if (_activeEffect == null)
            {
                _activeEffect = AllEffects[0];
            }

            _animationCts = new CancellationTokenSource();
            var token = _animationCts.Token;
            _stopwatch.Restart();

            Task.Run(async () =>
            {
                int delayMs = Math.Max(1, 1000 / targetFps);

                while (!token.IsCancellationRequested)
                {
                    double time = _stopwatch.Elapsed.TotalSeconds;
                    var frame = _activeEffect.Next(ledCount, time, brightness);
                    FrameGenerated?.Invoke(frame);

                    try
                    {
                        await Task.Delay(delayMs, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, token);
        }

        public void Stop()
        {
            if (_animationCts != null)
            {
                _animationCts.Cancel();
                _animationCts.Dispose();
                _animationCts = null;
            }
            _stopwatch.Stop();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

