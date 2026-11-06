using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using HaloSyncWin.Calibration;
using HaloSyncWin.Capture;
using HaloSyncWin.Diagnostics;
using HaloSyncWin.Discovery;
using HaloSyncWin.Effects;
using HaloSyncWin.Engine;
using HaloSyncWin.Models;
using HaloSyncWin.Network;
using HaloSyncWin.UI.Common;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;

namespace HaloSyncWin.UI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly Device _d3dDevice;
        private readonly ControllerMonitor _controllerMonitor;
        private readonly FluidEngine _fluidEngine;
        private readonly DiagnosticsService _diagnosticsService;
        private readonly EffectsEngine _effectsEngine;
        private readonly CalibrationService _calibrationService;
        private readonly AmbientPipeline _pipeline;
        private readonly DispatcherTimer _uiTimer;

        private string _selectedTab = "Home";
        private HaloSyncSettings _settings;
        private DeviceInfo? _discoveredDevice;
        private bool _isDeviceOn = true;
        private DisplayInfo? _selectedDisplay;
        private string _manualIpAddress = "";
        private DiagnosticsSnapshot _diagnosticsSnapshot;
        private string _statusText = "Initializing...";

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<DisplayInfo> Displays { get; } = new ObservableCollection<DisplayInfo>();
        public IReadOnlyList<IAmbientEffect> EffectsList => EffectsEngine.AllEffects;

        public string SelectedTab
        {
            get => _selectedTab;
            set { if (_selectedTab != value) { _selectedTab = value; OnPropertyChanged(); } }
        }

        public HaloSyncSettings Settings
        {
            get => _settings;
            set { _settings = value; OnPropertyChanged(); }
        }

        public DeviceInfo? DiscoveredDevice
        {
            get => _discoveredDevice;
            private set
            {
                _discoveredDevice = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasDevice));
                OnPropertyChanged(nameof(DeviceStatusColor));
                UpdateStatusText();
            }
        }

        public bool HasDevice => _discoveredDevice != null;
        public Brush DeviceStatusColor => _discoveredDevice?.ConnectionStatus == ConnectionStatus.Connected ?
            new SolidColorBrush(Color.FromRgb(75, 255, 145)) : new SolidColorBrush(Color.FromRgb(255, 90, 90));

        public bool IsDeviceOn
        {
            get => _isDeviceOn;
            set
            {
                if (_isDeviceOn != value)
                {
                    _isDeviceOn = value;
                    OnPropertyChanged();
                    UpdateStatusText();
                }
            }
        }

        public bool IsSyncRunning => _pipeline.IsRunning && _settings.ActiveMode != AmbientMode.Effects;

        public DisplayInfo? SelectedDisplay
        {
            get => _selectedDisplay;
            set
            {
                if (_selectedDisplay != value)
                {
                    _selectedDisplay = value;
                    OnPropertyChanged();
                    if (_pipeline.IsRunning && _selectedDisplay != null && _discoveredDevice != null)
                    {
                        _pipeline.Start(_selectedDisplay, _discoveredDevice, _settings);
                    }
                }
            }
        }

        public string ManualIpAddress
        {
            get => _manualIpAddress;
            set { _manualIpAddress = value; OnPropertyChanged(); }
        }

        public DiagnosticsSnapshot Diagnostics
        {
            get => _diagnosticsSnapshot;
            private set { _diagnosticsSnapshot = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            private set { _statusText = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand SelectTabCommand { get; }
        public ICommand TogglePowerCommand { get; }
        public ICommand ToggleScreenSyncCommand { get; }
        public ICommand SelectModeCommand { get; }
        public ICommand SelectEffectCommand { get; }
        public ICommand SetSolidColorCommand { get; }
        public ICommand ConnectManualIpCommand { get; }
        public ICommand RunColorTestCommand { get; }
        public ICommand StartLedWalkCommand { get; }
        public ICommand StartBrightnessSweepCommand { get; }
        public ICommand StopCalibrationCommand { get; }
        public ICommand SaveSettingsCommand { get; }

        public MainViewModel()
        {
            _settings = HaloSyncSettings.Load();

            // Initialize Direct3D 11 Device with BGRA support for DXGI capture
            _d3dDevice = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);

            _fluidEngine = new FluidEngine();
            _diagnosticsService = new DiagnosticsService();
            _effectsEngine = new EffectsEngine();
            _calibrationService = new CalibrationService();
            _controllerMonitor = new ControllerMonitor();

            _pipeline = new AmbientPipeline(_d3dDevice, _fluidEngine, _diagnosticsService, _effectsEngine);
            _diagnosticsSnapshot = _diagnosticsService.Snapshot;

            // Setup commands
            SelectTabCommand = new RelayCommand(p => SelectedTab = p?.ToString() ?? "Home");
            TogglePowerCommand = new RelayCommand(async () => await ToggleDevicePowerAsync());
            ToggleScreenSyncCommand = new RelayCommand(async () => await ToggleScreenSyncAsync());
            SelectModeCommand = new RelayCommand(p => SelectMode(p));
            SelectEffectCommand = new RelayCommand(p => SelectEffect(p));
            SetSolidColorCommand = new RelayCommand(p => SetSolidColor(p));
            ConnectManualIpCommand = new RelayCommand(() => ConnectManualIp());
            RunColorTestCommand = new RelayCommand(p => RunColorTest(p?.ToString()));
            StartLedWalkCommand = new RelayCommand(() => StartLedWalk());
            StartBrightnessSweepCommand = new RelayCommand(() => StartBrightnessSweep());
            StopCalibrationCommand = new RelayCommand(() => StopCalibration());
            SaveSettingsCommand = new RelayCommand(() => _settings.Save());

            // Wire Controller Discovery
            _controllerMonitor.DeviceChanged += device =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    DiscoveredDevice = device;
                    if (device != null && _settings.AutoReconnect && !_pipeline.IsRunning)
                    {
                        StartPipelineIfReady();
                    }
                });
            };

            // Discover Displays
            RefreshDisplays();

            // UI Refresh Timer (500ms)
            _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _uiTimer.Tick += (s, e) =>
            {
                Diagnostics = _diagnosticsService.Snapshot;
                OnPropertyChanged(nameof(IsSyncRunning));
                UpdateStatusText();
            };
            _uiTimer.Start();

            // Start discovery
            _controllerMonitor.StartDiscovery(_settings.LastKnownControllerAddress);
        }

        private void RefreshDisplays()
        {
            Displays.Clear();
            var detected = DisplayDiscovery.GetDisplays();
            foreach (var d in detected) Displays.Add(d);

            SelectedDisplay = Displays.FirstOrDefault(d => d.IsPrimary) ?? Displays.FirstOrDefault();
        }

        public void NotifySettingsChanged()
        {
            _settings.Save();
            _pipeline.UpdateSettings(_settings);
            OnPropertyChanged(nameof(Settings));
        }

        private async Task ToggleDevicePowerAsync()
        {
            IsDeviceOn = !IsDeviceOn;

            if (_discoveredDevice != null)
            {
                await WLEDHttpClient.SetPowerAsync(_discoveredDevice.Address, IsDeviceOn);
            }

            if (!IsDeviceOn && _pipeline.IsRunning)
            {
                _pipeline.Stop();
            }
            else if (IsDeviceOn && _settings.ActiveMode != AmbientMode.Effects)
            {
                StartPipelineIfReady();
            }

            OnPropertyChanged(nameof(IsSyncRunning));
        }

        private async Task ToggleScreenSyncAsync()
        {
            if (_pipeline.IsRunning)
            {
                _pipeline.Stop();
            }
            else
            {
                if (!IsDeviceOn)
                {
                    await ToggleDevicePowerAsync();
                }

                _settings.ActiveMode = AmbientMode.Ambient;
                NotifySettingsChanged();
                StartPipelineIfReady();
            }

            OnPropertyChanged(nameof(IsSyncRunning));
            UpdateStatusText();
        }

        private void SelectMode(object? modeObj)
        {
            if (modeObj is AmbientMode mode)
            {
                _settings.ActiveMode = mode;
                if (mode != AmbientMode.Custom)
                {
                    var p = mode.GetDefaultParameters();
                    _settings.Brightness = p.Brightness;
                    _settings.Smoothness = p.Smoothness;
                    _settings.AmbientStrength = p.AmbientStrength;
                }

                NotifySettingsChanged();

                if (mode != AmbientMode.Effects && !_pipeline.IsRunning)
                {
                    StartPipelineIfReady();
                }
                else if (mode == AmbientMode.Effects)
                {
                    _pipeline.Stop();
                    ApplyActiveEffectHardware();
                }
            }
        }

        private void SelectEffect(object? effectObj)
        {
            if (effectObj is IAmbientEffect effect)
            {
                _settings.ActiveMode = AmbientMode.Effects;
                _settings.ActiveEffectId = effect.Id;
                NotifySettingsChanged();

                if (_pipeline.IsRunning) _pipeline.Stop();
                ApplyActiveEffectHardware();
            }
        }

        private void SetSolidColor(object? colorHex)
        {
            if (colorHex is string hex && hex.Length >= 6)
            {
                try
                {
                    byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                    byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                    byte b = Convert.ToByte(hex.Substring(4, 2), 16);

                    _settings.SetSolidColor(r / 255.0f, g / 255.0f, b / 255.0f);
                    NotifySettingsChanged();

                    if (_discoveredDevice != null)
                    {
                        Task.Run(async () =>
                        {
                            await WLEDHttpClient.SetSolidColorAsync(
                                _discoveredDevice.Address,
                                r / 255.0f, g / 255.0f, b / 255.0f,
                                _settings.Brightness);
                        });
                    }
                }
                catch { }
            }
        }

        private void ApplyActiveEffectHardware()
        {
            if (_discoveredDevice == null || string.IsNullOrWhiteSpace(_discoveredDevice.Address)) return;

            var effect = EffectsList.FirstOrDefault(e => e.Id == _settings.ActiveEffectId) ?? EffectsList[0];
            var hw = effect.HardwareConfig;

            Task.Run(async () =>
            {
                if (hw.UsesSolidColor)
                {
                    await WLEDHttpClient.SetSolidColorAsync(
                        _discoveredDevice.Address,
                        _settings.SolidColor.X,
                        _settings.SolidColor.Y,
                        _settings.SolidColor.Z,
                        _settings.Brightness);
                }
                else
                {
                    await WLEDHttpClient.SetHardwareEffectAsync(
                        _discoveredDevice.Address,
                        hw.FxId,
                        _settings.EffectSpeed,
                        _settings.EffectIntensity,
                        _settings.Brightness,
                        hw.PaletteId);
                }
            });
        }

        private void ConnectManualIp()
        {
            if (string.IsNullOrWhiteSpace(_manualIpAddress)) return;
            _settings.LastKnownControllerAddress = _manualIpAddress;
            _settings.Save();
            _controllerMonitor.SetManualDevice(_manualIpAddress, _settings.Layout.TotalLEDs);
        }

        private void StartPipelineIfReady()
        {
            if (_selectedDisplay != null && _discoveredDevice != null && IsDeviceOn)
            {
                _pipeline.Start(_selectedDisplay, _discoveredDevice, _settings);
                OnPropertyChanged(nameof(IsSyncRunning));
                UpdateStatusText();
            }
        }

        private void RunColorTest(string? colorName)
        {
            if (_discoveredDevice == null) return;
            _pipeline.Stop();

            LEDColor col = colorName switch
            {
                "Red" => new LEDColor(1f, 0f, 0f),
                "Green" => new LEDColor(0f, 1f, 0f),
                "Blue" => new LEDColor(0f, 0f, 1f),
                "White" => new LEDColor(1f, 1f, 1f),
                _ => LEDColor.White
            };

            _calibrationService.RunSolidTest(_pipeline.Controller, _settings.Layout.TotalLEDs, col, _settings.Brightness, _settings.ColorOrder);
        }

        private void StartLedWalk()
        {
            if (_discoveredDevice == null) return;
            _pipeline.Stop();
            _calibrationService.StartLedWalk(_pipeline.Controller, _settings.Layout.TotalLEDs, _settings.Brightness, _settings.ColorOrder);
        }

        private void StartBrightnessSweep()
        {
            if (_discoveredDevice == null) return;
            _pipeline.Stop();
            _calibrationService.StartBrightnessSweep(_pipeline.Controller, _settings.Layout.TotalLEDs, _settings.ColorOrder);
        }

        private void StopCalibration()
        {
            _calibrationService.Stop();
            StartPipelineIfReady();
        }

        private void UpdateStatusText()
        {
            if (!IsDeviceOn)
            {
                StatusText = "Lighting is powered off";
            }
            else if (_discoveredDevice == null)
            {
                StatusText = "Searching for WLED controller...";
            }
            else if (_pipeline.IsRunning)
            {
                StatusText = $"Syncing screen ({Diagnostics.CaptureFPS:0} FPS · {Diagnostics.TotalLatencyMs:0.0}ms)";
            }
            else if (_settings.ActiveMode == AmbientMode.Effects)
            {
                StatusText = "Hardware effect active";
            }
            else
            {
                StatusText = "Connected · Sync paused";
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Dispose()
        {
            _uiTimer.Stop();
            _pipeline.Dispose();
            _calibrationService.Dispose();
            _effectsEngine.Dispose();
            _controllerMonitor.Dispose();
            _d3dDevice.Dispose();
        }
    }
}

