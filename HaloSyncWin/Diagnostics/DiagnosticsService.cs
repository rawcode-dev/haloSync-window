using System;

namespace HaloSyncWin.Diagnostics
{
    public record DiagnosticsSnapshot(
        double CaptureFPS,
        double TotalLatencyMs,
        double GpuProcessingMs,
        long PacketsSent,
        long DroppedFrames);

    public sealed class DiagnosticsService
    {
        private long _framesCaptured = 0;
        private long _packetsSent = 0;
        private long _droppedFrames = 0;
        private double _currentFps = 0.0;
        private double _lastLatencyMs = 0.0;
        private double _lastGpuMs = 0.0;

        private DateTime _lastFpsCalcTime = DateTime.UtcNow;
        private int _fpsCounter = 0;
        private readonly object _lock = new object();

        public DiagnosticsSnapshot Snapshot
        {
            get
            {
                lock (_lock)
                {
                    return new DiagnosticsSnapshot(
                        _currentFps,
                        _lastLatencyMs,
                        _lastGpuMs,
                        _packetsSent,
                        _droppedFrames);
                }
            }
        }

        public void RecordFrame(double totalLatencyMs, double gpuMs)
        {
            lock (_lock)
            {
                _framesCaptured++;
                _packetsSent++;
                _fpsCounter++;
                _lastLatencyMs = totalLatencyMs;
                _lastGpuMs = gpuMs;

                var now = DateTime.UtcNow;
                var elapsed = (now - _lastFpsCalcTime).TotalSeconds;
                if (elapsed >= 1.0)
                {
                    _currentFps = _fpsCounter / elapsed;
                    _fpsCounter = 0;
                    _lastFpsCalcTime = now;
                }
            }
        }

        public void RecordDrop()
        {
            lock (_lock)
            {
                _droppedFrames++;
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _framesCaptured = 0;
                _packetsSent = 0;
                _droppedFrames = 0;
                _currentFps = 0.0;
                _fpsCounter = 0;
                _lastFpsCalcTime = DateTime.UtcNow;
            }
        }
    }
}

