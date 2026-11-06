using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HaloSyncWin.Models;
using HaloSyncWin.Network;

namespace HaloSyncWin.Discovery
{
    public sealed class ControllerMonitor : IDisposable
    {
        private readonly MDNSDiscovery _discovery = new MDNSDiscovery();
        private CancellationTokenSource? _monitorCts;
        private DeviceInfo? _discoveredDevice;
        private bool _isSearching = false;

        public DeviceInfo? DiscoveredDevice => _discoveredDevice;
        public bool IsSearching => _isSearching;

        public event Action<DeviceInfo?>? DeviceChanged;
        public event Action<bool>? SearchingChanged;

        public ControllerMonitor()
        {
            _discovery.DeviceDiscovered += OnDeviceFound;
        }

        public void StartDiscovery(string? storedAddress = null)
        {
            _isSearching = true;
            SearchingChanged?.Invoke(true);

            _discovery.Start();

            if (!string.IsNullOrWhiteSpace(storedAddress))
            {
                Task.Run(async () =>
                {
                    await _discovery.ProbeAddressAsync(storedAddress);
                });
            }

            StartHealthCheck();
        }

        private void OnDeviceFound(DeviceInfo device)
        {
            _discoveredDevice = device;
            _isSearching = false;
            SearchingChanged?.Invoke(false);
            DeviceChanged?.Invoke(device);
        }

        public void SetManualDevice(string address, int ledCount = 113)
        {
            Task.Run(async () =>
            {
                var probed = await WLEDHttpClient.QueryInfoAsync(address);
                if (probed != null)
                {
                    OnDeviceFound(probed);
                }
                else
                {
                    var dev = new DeviceInfo
                    {
                        Name = "WLED Controller",
                        Address = address,
                        LedCount = ledCount,
                        ConnectionStatus = ConnectionStatus.Connected,
                        LastSeenAt = DateTime.UtcNow
                    };
                    OnDeviceFound(dev);
                }
            });
        }

        private void StartHealthCheck()
        {
            _monitorCts?.Cancel();
            _monitorCts = new CancellationTokenSource();
            var token = _monitorCts.Token;

            Task.Run(async () =>
            {
                var sw = new Stopwatch();

                while (!token.IsCancellationRequested)
                {
                    if (_discoveredDevice != null && !string.IsNullOrWhiteSpace(_discoveredDevice.Address))
                    {
                        sw.Restart();
                        var probed = await WLEDHttpClient.QueryInfoAsync(_discoveredDevice.Address);
                        sw.Stop();

                        if (probed != null)
                        {
                            probed.LatencyMs = sw.Elapsed.TotalMilliseconds;
                            probed.ConnectionStatus = ConnectionStatus.Connected;
                            _discoveredDevice = probed;
                            DeviceChanged?.Invoke(probed);
                        }
                        else
                        {
                            _discoveredDevice.ConnectionStatus = ConnectionStatus.Disconnected;
                            DeviceChanged?.Invoke(_discoveredDevice);
                        }
                    }

                    try
                    {
                        await Task.Delay(5000, token);
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
            if (_monitorCts != null)
            {
                _monitorCts.Cancel();
                _monitorCts.Dispose();
                _monitorCts = null;
            }
            _discovery.Stop();
        }

        public void Dispose()
        {
            Stop();
            _discovery.Dispose();
        }
    }
}

