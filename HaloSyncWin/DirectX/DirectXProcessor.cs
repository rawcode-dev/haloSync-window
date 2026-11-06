using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using HaloSyncWin.Models;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace HaloSyncWin.DirectX
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ShaderParams
    {
        public uint LedCount;
        public uint SamplingDepth;
        public uint SamplesPerZone;
        public float BlackBarThreshold;
        public float Gamma;
        public float SaturationBoost;
        public uint TextureWidth;
        public uint TextureHeight;
        public Vector3 WallCompensation;
        public float Pad;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LEDColorOutput
    {
        public float R;
        public float G;
        public float B;
        public float Padding;
    }

    /// <summary>
    /// Direct3D 11 Compute Pipeline that processes captured desktop textures directly in VRAM.
    /// Runs ZoneSampler.hlsl to downsample screen edges into an array of LED colors.
    /// Zero 4K CPU copies: Only the tiny output array is read back to system RAM.
    /// </summary>
    public sealed class DirectXProcessor : IDisposable
    {
        private readonly Device _device;
        private readonly DeviceContext _context;
        private ComputeShader? _computeShader;
        private SamplerState? _samplerState;
        private Buffer? _constantBuffer;
        private Buffer? _coordinatesBuffer;
        private ShaderResourceView? _coordinatesSrv;
        private Buffer? _outputBuffer;
        private UnorderedAccessView? _outputUav;
        private Buffer? _stagingBuffer;

        private int _allocatedLedCount = 0;
        private readonly object _lock = new object();

        public Device Device => _device;
        public DeviceContext Context => _context;

        public DirectXProcessor(Device device)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _context = _device.ImmediateContext;
            InitializePipeline();
        }

        private void InitializePipeline()
        {
            // 1. Compile HLSL Compute Shader
            string shaderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Shaders", "ZoneSampler.hlsl");
            string shaderSource;

            if (File.Exists(shaderPath))
            {
                shaderSource = File.ReadAllText(shaderPath);
            }
            else
            {
                // Fallback: search relative to source directory
                string altPath = Path.Combine(Directory.GetCurrentDirectory(), "Shaders", "ZoneSampler.hlsl");
                if (File.Exists(altPath))
                {
                    shaderSource = File.ReadAllText(altPath);
                }
                else
                {
                    throw new FileNotFoundException("ZoneSampler.hlsl shader file was not found.");
                }
            }

            using var compilationResult = ShaderBytecode.Compile(shaderSource, "CSMain", "cs_5_0", ShaderFlags.OptimizationLevel3);
            if (compilationResult.HasErrors)
            {
                throw new InvalidOperationException($"Shader compilation failed: {compilationResult.Message}");
            }

            _computeShader = new ComputeShader(_device, compilationResult.Bytecode);

            // 2. Linear Sampler State
            var samplerDesc = new SamplerStateDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                ComparisonFunction = Comparison.Never,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            };
            _samplerState = new SamplerState(_device, samplerDesc);

            // 3. Constant Buffer for ZoneSamplerParams
            int cbSize = Utilities.SizeOf<ShaderParams>();
            // Must be multiple of 16 bytes
            cbSize = (cbSize + 15) & ~15;

            var cbDesc = new BufferDescription
            {
                SizeInBytes = cbSize,
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0
            };
            _constantBuffer = new Buffer(_device, cbDesc);
        }

        private void EnsureOutputBuffers(int ledCount)
        {
            if (_allocatedLedCount >= ledCount && _outputBuffer != null && _stagingBuffer != null)
            {
                return;
            }

            _outputUav?.Dispose();
            _outputBuffer?.Dispose();
            _stagingBuffer?.Dispose();

            int stride = Utilities.SizeOf<LEDColorOutput>();
            int bufferSize = stride * ledCount;

            // GPU RW Structured Buffer
            var outDesc = new BufferDescription
            {
                SizeInBytes = bufferSize,
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.BufferStructured,
                StructureByteStride = stride
            };
            _outputBuffer = new Buffer(_device, outDesc);

            var uavDesc = new UnorderedAccessViewDescription
            {
                Format = SharpDX.DXGI.Format.Unknown,
                Dimension = UnorderedAccessViewDimension.Buffer,
                Buffer = new UnorderedAccessViewDescription.BufferResource
                {
                    FirstElement = 0,
                    ElementCount = ledCount,
                    Flags = UnorderedAccessViewBufferFlags.None
                }
            };
            _outputUav = new UnorderedAccessView(_device, _outputBuffer, uavDesc);

            // CPU Readback Staging Buffer
            var stagingDesc = new BufferDescription
            {
                SizeInBytes = bufferSize,
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = stride
            };
            _stagingBuffer = new Buffer(_device, stagingDesc);

            _allocatedLedCount = ledCount;
        }

        public void UpdateCoordinates(Vector2[] coordinates)
        {
            lock (_lock)
            {
                _coordinatesSrv?.Dispose();
                _coordinatesBuffer?.Dispose();

                int stride = Utilities.SizeOf<Vector2>();
                int bufferSize = stride * coordinates.Length;

                var bufferDesc = new BufferDescription
                {
                    SizeInBytes = bufferSize,
                    Usage = ResourceUsage.Immutable,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.BufferStructured,
                    StructureByteStride = stride
                };

                using var stream = DataStream.Create(coordinates, true, true);
                _coordinatesBuffer = new Buffer(_device, stream, bufferDesc);

                var srvDesc = new ShaderResourceViewDescription
                {
                    Format = SharpDX.DXGI.Format.Unknown,
                    Dimension = ShaderResourceViewDimension.Buffer,
                    Buffer = new ShaderResourceViewDescription.BufferResource
                    {
                        FirstElement = 0,
                        ElementCount = coordinates.Length
                    }
                };
                _coordinatesSrv = new ShaderResourceView(_device, _coordinatesBuffer, srvDesc);
            }
        }

        public LEDColor[] ProcessFrame(
            Texture2D desktopTexture,
            ProcessingSettings settings,
            int ledCount)
        {
            if (desktopTexture == null) throw new ArgumentNullException(nameof(desktopTexture));
            if (ledCount <= 0) return Array.Empty<LEDColor>();

            lock (_lock)
            {
                EnsureOutputBuffers(ledCount);

                if (_computeShader == null || _constantBuffer == null ||
                    _outputUav == null || _stagingBuffer == null || _coordinatesSrv == null)
                {
                    return Array.Empty<LEDColor>();
                }

                int texWidth = desktopTexture.Description.Width;
                int texHeight = desktopTexture.Description.Height;

                // 1. Update Constant Buffer
                var p = new ShaderParams
                {
                    LedCount = (uint)ledCount,
                    SamplingDepth = (uint)Math.Max(1, settings.SamplingDepth),
                    SamplesPerZone = (uint)Math.Max(4, settings.SamplesPerZone),
                    BlackBarThreshold = settings.BlackBarDetection ? settings.BlackBarThreshold : 0.0f,
                    Gamma = settings.Gamma,
                    SaturationBoost = settings.AmbientStrength,
                    TextureWidth = (uint)texWidth,
                    TextureHeight = (uint)texHeight,
                    WallCompensation = settings.WallCompensation,
                    Pad = 0f
                };

                DataBox box = _context.MapSubresource(_constantBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
                Utilities.Write(box.DataPointer, ref p);
                _context.UnmapSubresource(_constantBuffer, 0);

                // 2. Create SRV for Desktop Texture
                using var textureSrv = new ShaderResourceView(_device, desktopTexture);

                // 3. Bind Compute Pipeline State
                _context.ComputeShader.Set(_computeShader);
                _context.ComputeShader.SetSampler(0, _samplerState);
                _context.ComputeShader.SetConstantBuffer(0, _constantBuffer);
                _context.ComputeShader.SetShaderResource(0, textureSrv);
                _context.ComputeShader.SetShaderResource(1, _coordinatesSrv);
                _context.ComputeShader.SetUnorderedAccessView(0, _outputUav);

                // 4. Dispatch Compute Kernel
                int threadGroupsX = (ledCount + 63) / 64;
                _context.Dispatch(threadGroupsX, 1, 1);

                // 5. Unbind resources to avoid DX hazards
                _context.ComputeShader.SetShaderResource(0, null);
                _context.ComputeShader.SetShaderResource(1, null);
                _context.ComputeShader.SetUnorderedAccessView(0, null);

                // 6. Copy GPU Output Buffer to Staging Buffer (Tiny copy, < 3KB)
                _context.CopyResource(_outputBuffer, _stagingBuffer);

                // 7. Map Staging Buffer and Read Back
                var results = new LEDColor[ledCount];
                DataBox stagingBox = _context.MapSubresource(_stagingBuffer, 0, MapMode.Read, MapFlags.None);
                try
                {
                    unsafe
                    {
                        var ptr = (LEDColorOutput*)stagingBox.DataPointer;
                        for (int i = 0; i < ledCount; i++)
                        {
                            results[i] = new LEDColor(ptr[i].R, ptr[i].G, ptr[i].B);
                        }
                    }
                }
                finally
                {
                    _context.UnmapSubresource(_stagingBuffer, 0);
                }

                return results;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _outputUav?.Dispose();
                _outputBuffer?.Dispose();
                _stagingBuffer?.Dispose();
                _coordinatesSrv?.Dispose();
                _coordinatesBuffer?.Dispose();
                _constantBuffer?.Dispose();
                _samplerState?.Dispose();
                _computeShader?.Dispose();
            }
        }
    }
}

