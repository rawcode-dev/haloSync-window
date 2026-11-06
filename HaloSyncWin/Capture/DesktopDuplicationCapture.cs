using System;
using HaloSyncWin.Models;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace HaloSyncWin.Capture
{
    /// <summary>
    /// Captures the desktop in real-time at native display refresh rates using DXGI 1.2 Desktop Duplication API.
    /// Provides zero-copy GPU textures directly in VRAM.
    /// Robust error handling handles display switches, UAC prompts, and fullscreen exclusive transitions.
    /// </summary>
    public sealed class DesktopDuplicationCapture : ICaptureEngine
    {
        private readonly Device _device;
        private OutputDuplication? _outputDuplication;
        private SharpDX.DXGI.Resource? _currentResource;
        private Texture2D? _currentTexture;
        private DisplayInfo? _currentDisplay;
        private bool _isRunning = false;
        private readonly object _lock = new object();

        public bool IsRunning => _isRunning;
        public DisplayInfo? CurrentDisplay => _currentDisplay;

        public DesktopDuplicationCapture(Device device)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        public void Start(DisplayInfo display)
        {
            lock (_lock)
            {
                Stop();
                _currentDisplay = display;
                InitializeDuplication();
                _isRunning = true;
            }
        }

        private void InitializeDuplication()
        {
            _outputDuplication?.Dispose();
            _outputDuplication = null;

            using var dxgiDevice = _device.QueryInterface<SharpDX.DXGI.Device>();
            using var adapter = dxgiDevice.Adapter;

            int outputIndex = _currentDisplay?.OutputIndex ?? 0;
            if (outputIndex >= adapter.GetOutputCount())
            {
                outputIndex = 0;
            }

            using var output = adapter.GetOutput(outputIndex);
            using var output1 = output.QueryInterface<Output1>();

            _outputDuplication = output1.DuplicateOutput(_device);
        }

        public Texture2D? AcquireFrame(int timeoutMs = 100)
        {
            lock (_lock)
            {
                if (!_isRunning) return null;

                ReleaseFrame();

                if (_outputDuplication == null)
                {
                    try
                    {
                        InitializeDuplication();
                    }
                    catch
                    {
                        return null;
                    }
                }

                try
                {
                    var result = _outputDuplication!.AcquireNextFrame(timeoutMs, out var frameInfo, out _currentResource);
                    if (result.Failure || _currentResource == null)
                    {
                        return null;
                    }

                    _currentTexture = _currentResource.QueryInterface<Texture2D>();
                    return _currentTexture;
                }
                catch (SharpDXException ex)
                {
                    // DXGI_ERROR_ACCESS_LOST (0x887A0026) occurs when user presses Ctrl+Alt+Del,
                    // UAC prompt appears, resolution changes, or a fullscreen game takes exclusive mode.
                    if (ex.ResultCode.Code == unchecked((int)0x887A0026) ||
                        ex.ResultCode.Code == unchecked((int)0x887A0001))
                    {
                        _outputDuplication?.Dispose();
                        _outputDuplication = null;
                    }
                    return null;
                }
            }
        }

        public void ReleaseFrame()
        {
            lock (_lock)
            {
                if (_currentTexture != null)
                {
                    _currentTexture.Dispose();
                    _currentTexture = null;
                }

                if (_currentResource != null)
                {
                    _currentResource.Dispose();
                    _currentResource = null;
                }

                if (_outputDuplication != null)
                {
                    try
                    {
                        _outputDuplication.ReleaseFrame();
                    }
                    catch (SharpDXException)
                    {
                        _outputDuplication.Dispose();
                        _outputDuplication = null;
                    }
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _isRunning = false;
                ReleaseFrame();
                _outputDuplication?.Dispose();
                _outputDuplication = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
