<h1 align="center">HaloSync for Windows</h1>

<p align="center">
  <strong>Ultra-low latency, zero-copy GPU ambient lighting pipeline for Windows 10 & 11.</strong><br>
  Built with C# .NET 8, Direct3D 11 Compute Shaders (HLSL), DXGI Desktop Duplication, and native WPF.
</p>

<p align="center">
  <a href="https://github.com/rawcode-dev/halosync-windows/actions/workflows/ci.yml">
    <img src="https://github.com/rawcode-dev/halosync-windows/actions/workflows/ci.yml/badge.svg" alt="CI Status">
  </a>
  <a href="https://github.com/rawcode-dev/halosync-windows/releases/latest">
    <img src="https://img.shields.io/github/v/release/rawcode-dev/halosync-windows?label=Latest%20Release&color=blue" alt="Latest Release">
  </a>
</p>

---

## ⚡️ Quick Install (For Normal Users)

Transform your Windows PC into an immersive ambient lighting experience! HaloSync connects directly to your WLED / Pixylights controllers over Wi-Fi/Ethernet to synchronize LED strips with your display in real-time.

1. **[⬇️ Download the latest HaloSync-Windows-x64.zip](https://github.com/rawcode-dev/halosync-windows/releases/latest)** or the standalone **`HaloSync-Windows-x64.exe`**.
2. Extract the `.zip` (or run the standalone `.exe`).
3. HaloSync automatically discovers your WLED controller on your local network via mDNS (`_wled._tcp`).
4. Customize your LED segment counts in the **Layout** tab, and toggle **Screen Sync** on!

---

## 🛠 Features

- **DirectCompute HLSL Sampling**: Direct port of `ZoneSampler.metal` into `ZoneSampler.hlsl`. Computes weighted perimeter samples, gamma correction, wall compensation, and black bar suppression on the GPU.
- **FluidEngine Motion-Aware Smoothing**: Dynamically adapts smoothing: high motion (gaming/movies) yields immediate response, while static scenes transition with buttery smoothness.
- **Wall Color Match**: Inverts and compensates for colored walls (e.g. orange, blue) behind your monitor.
- **Smart Black Bar Detection**: Automatically crops letterbox/pillarbox borders so LEDs don't turn off during movie playback.
- **Zero-Allocation DDP Wire Protocol**: UDP packets are generated in pre-allocated buffers and blasted over non-blocking sockets.
- **Hardware Effect Offloading**: Standalone effects (Fire, Ocean, Rainbow, etc.) run directly on the WLED controller, yielding 0% PC CPU load.
- **Built-in Calibration**: Single-LED walk, RGB channel check, and brightness sweeps.

---

## 🧑‍💻 Architecture

```
[ DXGI Desktop Duplication ]  (VRAM Desktop Texture)
            │
            ▼
[ Direct3D 11 Compute Shader ] (ZoneSampler.hlsl - downsamples 4K -> LED array in VRAM)
            │
            ▼ (Tiny Staging Buffer copy: < 3 KB)
[ FluidEngine (C# LERP) ]      (Adaptive temporal smoothing)
            │
            ▼
[ WLEDUDPController ]          (Zero-allocation DDP / Raw UDP -> WLED / Pixylights)
```

---

## 🚀 Building & Running Locally

### Prerequisites
- Windows 10 (version 1803+) or Windows 11 (64-bit)
- DirectX 11 compatible GPU
- Visual Studio 2022 (with .NET Desktop Development workload) or .NET 8.0 SDK

### Build in Visual Studio
1. Open `HaloSync.sln` in Visual Studio 2022.
2. Select **Release | Any CPU** (or `x64`).
3. Press **F5** or **Ctrl+F5** to build and run!

### Build via Command Line
```powershell
cd HaloSyncWin
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

### Native Self-Contained Executable
```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

---

## 🔄 Automated CI/CD & Releases

This repository includes a GitHub Actions workflow ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)) that:
1. Performs static security analysis via **GitHub CodeQL** on every pull request and push to `main`.
2. Automatically compiles and packages self-contained 64-bit binaries:
   - `HaloSync-Windows-x64.exe` (standalone single-file executable)
   - `HaloSync-Windows-x64.zip` (portable archive)
3. Automatically increments semantic version tags (`mathieudutour/github-tag-action@v6.2`).
4. Generates a new GitHub Release with release notes and attaches the executables automatically (`softprops/action-gh-release@v2`).

---

## 📄 License

Distributed under the MIT License.
