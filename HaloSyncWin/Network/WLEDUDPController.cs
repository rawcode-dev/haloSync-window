using System;
using System.Net;
using System.Net.Sockets;
using HaloSyncWin.Models;
using HaloSyncWin.Network.Protocols;

namespace HaloSyncWin.Network
{
    /// <summary>
    /// High-performance UDP LED Controller for WLED / Pixylights hardware.
    /// Operates a non-blocking asynchronous UDP pipeline with pre-allocated reusable byte buffers.
    /// Produces ZERO garbage collection allocations on the 60-144 FPS hot path.
    /// </summary>
    public sealed class WLEDUDPController : IDisposable
    {
        private Socket? _socket;
        private IPEndPoint? _endPoint;
        private ILEDOutputProtocol _protocol;
        private byte _nextSequence = 1;
        private readonly byte[] _sendBuffer = new byte[10 + 4096 * 3]; // Supports up to 4096 LEDs without realloc
        private readonly object _lock = new object();

        public bool IsConnected => _socket != null && _endPoint != null;
        public ILEDOutputProtocol Protocol => _protocol;

        public WLEDUDPController(ControllerProtocol protocol = ControllerProtocol.DDP)
        {
            _protocol = protocol switch
            {
                ControllerProtocol.DDP => new DDPProtocol(),
                ControllerProtocol.UDPRaw => new UDPRawProtocol(),
                _ => new DDPProtocol()
            };
        }

        public void SetProtocol(ControllerProtocol protocol)
        {
            lock (_lock)
            {
                _protocol = protocol switch
                {
                    ControllerProtocol.DDP => new DDPProtocol(),
                    ControllerProtocol.UDPRaw => new UDPRawProtocol(),
                    _ => new DDPProtocol()
                };
            }
        }

        public void Connect(string address, ushort port)
        {
            lock (_lock)
            {
                Disconnect();

                if (string.IsNullOrWhiteSpace(address)) return;

                if (!IPAddress.TryParse(address, out var ip))
                {
                    try
                    {
                        var entry = Dns.GetHostEntry(address);
                        if (entry.AddressList.Length > 0)
                        {
                            ip = entry.AddressList[0];
                        }
                    }
                    catch
                    {
                        return;
                    }
                }

                if (ip == null) return;

                _endPoint = new IPEndPoint(ip, port);
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                {
                    Blocking = false,
                    SendBufferSize = 65536
                };
            }
        }

        public void Disconnect()
        {
            lock (_lock)
            {
                if (_socket != null)
                {
                    try
                    {
                        _socket.Close();
                        _socket.Dispose();
                    }
                    catch { }
                    _socket = null;
                }
                _endPoint = null;
            }
        }

        public void SendFrame(LEDFrame frame, float brightness, ColorOrder order)
        {
            lock (_lock)
            {
                if (_socket == null || _endPoint == null || frame.Colors.Length == 0) return;

                byte seq = _nextSequence;
                _nextSequence = (byte)(_nextSequence >= 15 ? 1 : _nextSequence + 1);

                int packetLength = _protocol.Encode(frame, brightness, order, seq, _sendBuffer);

                try
                {
                    // Fire-and-forget non-blocking UDP transmission with ReadOnlySpan (0 GC overhead)
                    _socket.SendTo(new ReadOnlySpan<byte>(_sendBuffer, 0, packetLength), SocketFlags.None, _endPoint);
                }
                catch (SocketException)
                {
                    // Non-blocking socket can drop or queue; ignore transient drops to maintain zero latency
                }
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
