using System;
using System.Threading;
using System.Threading.Tasks;
using HaloSyncWin.Models;
using HaloSyncWin.Network;

namespace HaloSyncWin.Calibration
{
    public sealed class CalibrationService : IDisposable
    {
        private CancellationTokenSource? _testCts;

        public bool IsRunning => _testCts != null;

        public void RunSolidTest(WLEDUDPController controller, int ledCount, LEDColor color, float brightness, ColorOrder order)
        {
            Stop();

            var colors = new LEDColor[ledCount];
            for (int i = 0; i < ledCount; i++) colors[i] = color;
            var frame = new LEDFrame(colors, FrameSource.Calibration);

            controller.SendFrame(frame, brightness, order);
        }

        public void StartLedWalk(WLEDUDPController controller, int ledCount, float brightness, ColorOrder order, int intervalMs = 60)
        {
            Stop();

            _testCts = new CancellationTokenSource();
            var token = _testCts.Token;

            Task.Run(async () =>
            {
                int currentIdx = 0;
                var colors = new LEDColor[ledCount];

                while (!token.IsCancellationRequested)
                {
                    Array.Clear(colors, 0, colors.Length);
                    colors[currentIdx] = LEDColor.White;

                    var frame = new LEDFrame(colors, FrameSource.Calibration);
                    controller.SendFrame(frame, brightness, order);

                    currentIdx = (currentIdx + 1) % ledCount;

                    try
                    {
                        await Task.Delay(intervalMs, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, token);
        }

        public void StartBrightnessSweep(WLEDUDPController controller, int ledCount, ColorOrder order)
        {
            Stop();

            _testCts = new CancellationTokenSource();
            var token = _testCts.Token;

            Task.Run(async () =>
            {
                var colors = new LEDColor[ledCount];
                for (int i = 0; i < ledCount; i++) colors[i] = LEDColor.White;

                float b = 0.1f;
                float step = 0.05f;

                while (!token.IsCancellationRequested)
                {
                    var frame = new LEDFrame(colors, FrameSource.Calibration);
                    controller.SendFrame(frame, b, order);

                    b += step;
                    if (b >= 1.0f || b <= 0.1f) step = -step;

                    try
                    {
                        await Task.Delay(50, token);
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
            if (_testCts != null)
            {
                _testCts.Cancel();
                _testCts.Dispose();
                _testCts = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

