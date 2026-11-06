using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using HaloSyncWin.Capture;
using HaloSyncWin.Diagnostics;
using HaloSyncWin.DirectX;
using HaloSyncWin.Effects;
using HaloSyncWin.Layout;
using HaloSyncWin.Models;
using HaloSyncWin.Network;
using SharpDX.Direct3D11;

namespace HaloSyncWin.Engine
{
    /// <summary>
    /// The heart of HaloSync Windows. Coordinates the entire real-time pipeline:
    /// DesktopDuplicationCapture -> DirectXProcessor (HLSL Compute) -> FluidEngine -> WLEDUDPController.
    /// Runs asynchronously in a high-priority background thread with precision timing.
    /// </summary>
    public sealed class AmbientPipeline : IDisposable
    {
        private readonly Device _device;
        private readonly DesktopDuplicationCapture _capture;
        private readonly DirectXProcessor _processor;
        private readonly FluidEngine _fluid;
        private readonly DiagnosticsService _diagnostics;
        private readonly EffectsEngine _effects;
        private readonly WLEDUDPController _udpController;

        private CancellationTokenSource? _pipelineCts;
        private HaloSyncSettings _settings = new HaloSyncSettings();
        private DisplayInfo? _currentDisplay;
        private DeviceInfo? _currentDevice;
        private bool _isRunning = false;
        private readonly object _lock = new object();

        public bool IsRunning => _isRunning;
        public DiagnosticsService Diagnostics => _diagnostics;
        public FluidEngine Fluid => _fluid;
        public WLEDUDPController Controller => _udpController;

        public event Action<LEDFrame>? FrameProcessed;

        public AmbientPipeline(
            Device device,
            FluidEngine fluid,
            DiagnosticsService diagnostics,
            EffectsEngine effects)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _capture = new DesktopDuplicationCapture(_device);
            _processor = new DirectXProcessor(_device);
            _fluid = fluid;
            _diagnostics = diagnostics;
            _effects = effects;
            _udpController = new WLEDUDPController();

            _effects.FrameGenerated += OnEffectFrameGenerated;
        }

        private void OnEffectFrameGenerated(LEDFrame frame)
        {
            if (_isRunning && _settings.ActiveMode == AmbientMode.Effects)
            {
                _udpController.SendFrame(frame, _settings.Brightness, _settings.ColorOrder);
                FrameProcessed?.Invoke(frame);
            }
        }

        public void UpdateSettings(HaloSyncSettings settings)
        {
            lock (_lock)
            {
                _settings = settings;
                _udpController.SetProtocol(settings.ActiveProtocol);

                // Update Fluid parameters
                _fluid.Config.BaseSmoothing = settings.Smoothness;

                // Recompute and upload coordinates to GPU
                int totalLeds = settings.Layout.TotalLEDs;
                var coords = CoordinateMapper.Map(
                    settings.Layout,
                    totalLeds,
                    settings.CropTop / 100.0f,
                    settings.CropBottom / 100.0f,
                    settings.CropLeft / 100.0f,
                    settings.CropRight / 100.0f);

                _processor.UpdateCoordinates(coords);
            }
        }

        public void Start(DisplayInfo display, DeviceInfo device, HaloSyncSettings settings)
        {
            lock (_lock)
            {
                Stop();

                _currentDisplay = display;
                _currentDevice = device;
                _settings = settings;

                // 1. Connect UDP Controller
                _udpController.SetProtocol(settings.ActiveProtocol);
                _udpController.Connect(device.Address, device.Port);

                // 2. Setup coordinates for compute shader
                int totalLeds = settings.Layout.TotalLEDs;
                var coords = CoordinateMapper.Map(
                    settings.Layout,
                    totalLeds,
                    settings.CropTop / 100.0f,
                    settings.CropBottom / 100.0f,
                    settings.CropLeft / 100.0f,
                    settings.CropRight / 100.0f);

                _processor.UpdateCoordinates(coords);
                _fluid.Reset();
                _diagnostics.Reset();

                // 3. Start capture
                _capture.Start(display);

                _pipelineCts = new CancellationTokenSource();
                var token = _pipelineCts.Token;
                _isRunning = true;

                // 4. Spawn high-priority capture thread
                var thread = new Thread(() => PipelineLoop(token))
                {
                    IsBackground = true,
                    Priority = ThreadPriority.AboveNormal,
                    Name = "HaloSync-Pipeline"
                };
                thread.Start();
            }
        }

        private void PipelineLoop(CancellationToken token)
        {
            var totalSw = new Stopwatch();
            var gpuSw = new Stopwatch();

            int targetFps = _settings.ActiveMode.GetDefaultParameters().TargetFPS;
            int targetIntervalMs = Math.Max(1, 1000 / targetFps);

            while (!token.IsCancellationRequested)
            {
                totalSw.Restart();

                // Check for Layout Test override
                if (_settings.IsLayoutTestActive)
                {
                    int totalLeds = _settings.Layout.TotalLEDs;
                    var testColors = LayoutTestGenerator.Generate(_settings.Layout, totalLeds);
                    var testFrame = new LEDFrame(testColors, FrameSource.Calibration);

                    _udpController.SendFrame(testFrame, _settings.Brightness, _settings.ColorOrder);
                    FrameProcessed?.Invoke(testFrame);
                    _diagnostics.RecordFrame(totalSw.Elapsed.TotalMilliseconds, 0.0);

                    Thread.Sleep(targetIntervalMs);
                    continue;
                }

                // If in Effects Mode, let EffectsEngine or WLED hardware offload handle it
                if (_settings.ActiveMode == AmbientMode.Effects)
                {
                    Thread.Sleep(50);
                    continue;
                }

                // Hot Path: Screen Sync
                var screenTexture = _capture.AcquireFrame(targetIntervalMs);
                if (screenTexture == null)
                {
                    _diagnostics.RecordDrop();
                    Thread.Sleep(1);
                    continue;
                }

                try
                {
                    int totalLeds = _settings.Layout.TotalLEDs;
                    var procSettings = ProcessingSettings.From(
                        _settings.ActiveMode,
                        _settings.Brightness,
                        _settings.AmbientStrength,
                        _settings.WallColor);

                    procSettings.SamplingDepth = _settings.SamplingDepth;
                    procSettings.Gamma = _settings.Gamma;
                    procSettings.BlackBarDetection = _settings.BlackBarDetection;
                    procSettings.BlackBarThreshold = _settings.BlackBarThreshold;

                    // GPU Compute Dispatch
                    gpuSw.Restart();
                    var rawColors = _processor.ProcessFrame(screenTexture, procSettings, totalLeds);
                    gpuSw.Stop();

                    // Release screen texture immediately
                    _capture.ReleaseFrame();

                    // Fluid Smoothing (CPU LERP)
                    var rawFrame = new LEDFrame(rawColors, FrameSource.ScreenCapture);
                    var smoothedFrame = _fluid.Smooth(rawFrame);

                    // UDP Wire Transmission (Zero-Allocation)
                    _udpController.SendFrame(smoothedFrame, _settings.Brightness, _settings.ColorOrder);

                    totalSw.Stop();

                    double totalMs = totalSw.Elapsed.TotalMilliseconds;
                    double gpuMs = gpuSw.Elapsed.TotalMilliseconds;

                    _diagnostics.RecordFrame(totalMs, gpuMs);
                    FrameProcessed?.Invoke(smoothedFrame);
                }
                catch (Exception)
                {
                    _diagnostics.RecordDrop();
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _isRunning = false;

                if (_pipelineCts != null)
                {
                    _pipelineCts.Cancel();
                    _pipelineCts.Dispose();
                    _pipelineCts = null;
                }

                _capture.Stop();
                _udpController.Disconnect();
                _effects.Stop();
                _fluid.Reset();
            }
        }

        public void Dispose()
        {
            Stop();
            _capture.Dispose();
            _processor.Dispose();
            _udpController.Dispose();
            _effects.Dispose();
        }
    }
}

