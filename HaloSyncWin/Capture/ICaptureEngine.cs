using System;
using HaloSyncWin.Models;
using SharpDX.Direct3D11;

namespace HaloSyncWin.Capture
{
    public interface ICaptureEngine : IDisposable
    {
        bool IsRunning { get; }
        DisplayInfo? CurrentDisplay { get; }
        void Start(DisplayInfo display);
        void Stop();
        Texture2D? AcquireFrame(int timeoutMs = 100);
        void ReleaseFrame();
    }
}

