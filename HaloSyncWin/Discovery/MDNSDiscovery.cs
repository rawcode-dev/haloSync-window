using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HaloSyncWin.Models;
using HaloSyncWin.Network;
using Makaretu.Dns;

namespace HaloSyncWin.Discovery
{
    public sealed class MDNSDiscovery : IDisposable
    {
        private MulticastService? _mdns;
        private ServiceDiscovery? _sd;
        private CancellationTokenSource? _cts;

        public event Action<DeviceInfo>? DeviceDiscovered;
        public bool IsRunning => _mdns != null;

        public void Start()
        {
            Stop();

            _cts = new CancellationTokenSource();

            try
            {
                _mdns = new MulticastService();
                _sd = new ServiceDiscovery(_mdns);

                _mdns.AnswerReceived += OnDnsAnswerReceived;
                _mdns.Start();

                // Query for WLED services
                _sd.QueryServiceInstances("_wled._tcp");
            }
            catch { }
        }

        private void OnDnsAnswerReceived(object? sender, MessageEventArgs e)
        {
            var msg = e.Message;

            foreach (var record in msg.Answers)
            {
                if (record is ARecord aRecord)
                {
                    string ip = aRecord.Address.ToString();
                    Task.Run(async () =>
                    {
                        var dev = await WLEDHttpClient.QueryInfoAsync(ip);
                        if (dev != null)
                        {
                            DeviceDiscovered?.Invoke(dev);
                        }
                    });
                }
                else if (record is PTRRecord ptrRecord && ptrRecord.DomainName.ToString().Contains("wled"))
                {
                    _sd?.QueryServiceInstances(ptrRecord.DomainName);
                }
            }
        }

        public async Task ProbeAddressAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return;

            var dev = await WLEDHttpClient.QueryInfoAsync(address);
            if (dev != null)
            {
                DeviceDiscovered?.Invoke(dev);
            }
        }

        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            if (_mdns != null)
            {
                try
                {
                    _mdns.AnswerReceived -= OnDnsAnswerReceived;
                    _mdns.Stop();
                    _mdns.Dispose();
                }
                catch { }
                _mdns = null;
            }

            _sd?.Dispose();
            _sd = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

