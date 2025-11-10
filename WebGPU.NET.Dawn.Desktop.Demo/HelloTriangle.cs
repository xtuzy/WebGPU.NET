// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Silk.NET.Maths;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
#if WGPUNATIVE
using static WebGPU.NET.Wgpu.WebGPU;
#else
using static WebGPU.NET.Dawn.WebGPU;
#endif

#if WGPUNATIVE
namespace WebGPU.NET.Wgpu.Desktop.Demo
#else
namespace WebGPU.NET.Dawn.Desktop.Demo
#endif
{
    /// <summary>
    /// https://github.com/dotnet/Silk.NET/blob/v2.22.0/src/Lab/Experiments/WebGPUTriangle/Program.cs
    /// </summary>
    public unsafe class HelloTriangle : BaseApp
    {
        private WGPUSurfaceImpl _Surface;
        private WGPUTextureFormat _Format;

        private WGPUInstanceImpl _Instance;
        private WGPUAdapterImpl _Adapter;
        private WGPUDeviceImpl _Device;
        private WGPUShaderModuleImpl _Shader;
        private WGPURenderPipelineImpl _Pipeline;
        private WGPUPresentMode[] presentModes;
        private WGPUCompositeAlphaMode[] alphaModes;

        private const string SHADER = @"@vertex
fn vs_main(@builtin(vertex_index) in_vertex_index: u32) -> @builtin(position) vec4<f32> {
    let x = f32(i32(in_vertex_index) - 1);
    let y = f32(i32(in_vertex_index & 1u) * 2 - 1);
    return vec4<f32>(x, y, 0.0, 1.0);
}

@fragment
fn fs_main() -> @location(0) vec4<f32> {
    return vec4<f32>(1.0, 0.0, 0.0, 1.0);
}"
        ;

        protected override void FramebufferResize(Vector2D<int> size)
        {
            if (size.X == 0 && size.Y == 0)
            {
                return;
            }

            if (_Surface.Handle != nint.Zero)
                CreateSwapchain();
        }

        protected override void WindowOnLoad()
        {
#if WGPUNATIVE
            wgpuSetLogLevel(WGPULogLevel.Debug);
            var pWGPULoggingCallback = Marshal.GetFunctionPointerForDelegate<WGPULoggingCallback>((level, str, data1) =>
            {
                var bytes = new Span<byte>(str.Data, (int)str.Length);
                Trace.WriteLine(Encoding.UTF8.GetString(bytes));
            });
            wgpuSetLogCallback((delegate* unmanaged[Cdecl]<WGPULogLevel, WGPUStringView, void*, void>)pWGPULoggingCallback, (void*)IntPtr.Zero);
#endif

            WGPUInstanceDescriptor instanceDescriptor = new WGPUInstanceDescriptor()
            {
            };

            _Instance = wgpuCreateInstance(&instanceDescriptor);

            _Surface = CreateWebGPUSurface(Window, _Instance);

            #region Get adapter

            var requestAdapterOptions = new WGPURequestAdapterOptions
            {
                CompatibleSurface = _Surface,
                BackendType = WGPUBackendType.D3D12
            };

            unsafe
            {
                var pWGPURequestAdapterCallback = Marshal.GetFunctionPointerForDelegate<WGPURequestAdapterCallback>((status, _adapter, str, data1, data2) =>
                {
                    if (status == WGPURequestAdapterStatus.Success)
                    {
                        _Adapter = _adapter;
                    }
                    else
                    {
                        var bytes = new Span<byte>(str.Data, (int)str.Length);
                        Trace.WriteLine(Encoding.UTF8.GetString(bytes));
                    }
                });
                wgpuInstanceRequestAdapter
                (
                    _Instance,
                    &requestAdapterOptions,
                    new WGPURequestAdapterCallbackInfo()
                    {
                        Mode = WGPUCallbackMode.AllowSpontaneous,
                        Callback = (delegate* unmanaged[Cdecl]<WGPURequestAdapterStatus, WGPUAdapterImpl, WGPUStringView, void*, void*, void>)pWGPURequestAdapterCallback,
                    }
                );
            }

            Debug.WriteLine($"Got adapter {_Adapter:X}");

            WGPUAdapterInfo adapterInfo = default;
            wgpuAdapterGetInfo(_Adapter, &adapterInfo);

            WGPULimits limits = default;
            wgpuAdapterGetLimits(_Adapter, &limits);

            #endregion Get adapter

            PrintAdapterFeatures();

            #region Get device

            var pWGPUDeviceLostCallback = Marshal.GetFunctionPointerForDelegate<WGPUDeviceLostCallback>((_device, type, str, data1, data2) =>
            {
                var bytes = new Span<byte>(str.Data, (int)str.Length);
                Trace.WriteLine(Encoding.UTF8.GetString(bytes));
            });

            var pWGPUUncapturedErrorCallback = Marshal.GetFunctionPointerForDelegate<WGPUUncapturedErrorCallback>((_device, type, str, data1, data2) =>
            {
                var bytes = new Span<byte>(str.Data, (int)str.Length);
                Trace.WriteLine(Encoding.UTF8.GetString(bytes));
            });

            var deviceDescriptor = new WGPUDeviceDescriptor
            {
                DeviceLostCallbackInfo = new WGPUDeviceLostCallbackInfo()
                {
                    Mode = WGPUCallbackMode.AllowSpontaneous,
                    Callback = (delegate* unmanaged[Cdecl]<WGPUDeviceImpl*, WGPUDeviceLostReason, WGPUStringView, void*, void*, void>)pWGPUDeviceLostCallback
                },
                UncapturedErrorCallbackInfo = new WGPUUncapturedErrorCallbackInfo()
                {
                    Callback = (delegate* unmanaged[Cdecl]<WGPUDeviceImpl*, WGPUErrorType, WGPUStringView, void*, void*, void>)pWGPUUncapturedErrorCallback
                }
            };

            var pWGPURequestDeviceCallback = Marshal.GetFunctionPointerForDelegate<WGPURequestDeviceCallback>((status, _device, str, data1, data2) =>
            {
                if (status == WGPURequestDeviceStatus.Success)
                {
                    _Device = _device;
                }
                else
                {
                    var bytes = new Span<byte>(str.Data, (int)str.Length);
                    Trace.WriteLine(Encoding.UTF8.GetString(bytes));
                }
            });

            wgpuAdapterRequestDevice
            (
                _Adapter,
                &deviceDescriptor,
                new WGPURequestDeviceCallbackInfo()
                {
                    Mode = WGPUCallbackMode.AllowSpontaneous,
                    Callback = (delegate* unmanaged[Cdecl]<WGPURequestDeviceStatus, WGPUDeviceImpl, WGPUStringView, void*, void*, void>)pWGPURequestDeviceCallback
                }
            );
            Debug.WriteLine($"Got device {_Device:X}");

            #endregion Get device

            #region Log

#if !WGPUNATIVE
            var pWGPULoggingCallback = Marshal.GetFunctionPointerForDelegate<WGPULoggingCallback>((status, message, userData1, userData2) =>
            {
                var bytes = new Span<byte>(message.Data, (int)message.Length);
                Trace.WriteLine("Log:" + Encoding.UTF8.GetString(bytes));
            });

            wgpuDeviceSetLoggingCallback(_Device, new WGPULoggingCallbackInfo()
            {
                Callback = (delegate* unmanaged[Cdecl]<WGPULoggingType, WGPUStringView, void*, void*, void>)pWGPULoggingCallback,
            });
#endif

            #endregion Log

            #region Load shader

            byte[] shader_Utf8Bytes = Encoding.UTF8.GetBytes(SHADER);
            fixed (byte* p_shader_Utf8Bytes = shader_Utf8Bytes)
            {
                var shader = new WGPUShaderSourceWGSL()
                {
                    Code = new WGPUStringView() { Data = p_shader_Utf8Bytes, Length = (nuint)SHADER.Length },
                    Chain = new WGPUChainedStruct
                    {
                        SType = WGPUSType.ShaderSourceWGSL,
                    }
                };

                var shaderModuleDescriptor = new WGPUShaderModuleDescriptor
                {
                    NextInChain = &shader.Chain,
                    Label = new WGPUStringView() { Data = null, Length = 0 }
                };

                _Shader = wgpuDeviceCreateShaderModule(_Device, &shaderModuleDescriptor);
            }

            Debug.WriteLine($"Created shader {_Shader:X}");

            #endregion Load shader

            WGPUSurfaceCapabilities surfaceCapabilities = default;
            wgpuSurfaceGetCapabilities(_Surface, _Adapter, &surfaceCapabilities);
            if (surfaceCapabilities.FormatCount > 0)
            {
                Span<WGPUTextureFormat> surfaceFormats = new Span<WGPUTextureFormat>(surfaceCapabilities.Formats, (int)surfaceCapabilities.FormatCount);
                WGPUTextureFormat surfaceFormat = surfaceFormats[0];
                _Format = surfaceFormat;
            }
            presentModes = new Span<WGPUPresentMode>(surfaceCapabilities.PresentModes, (int)surfaceCapabilities.PresentModeCount).ToArray();
            alphaModes = new Span<WGPUCompositeAlphaMode>(surfaceCapabilities.AlphaModes, (int)surfaceCapabilities.AlphaModeCount).ToArray();

            #region Create pipeline

            var blendState = new WGPUBlendState
            {
                Color = new WGPUBlendComponent
                {
                    SrcFactor = WGPUBlendFactor.One,
                    DstFactor = WGPUBlendFactor.Zero,
                    Operation = WGPUBlendOperation.Add
                },
                Alpha = new WGPUBlendComponent
                {
                    SrcFactor = WGPUBlendFactor.One,
                    DstFactor = WGPUBlendFactor.Zero,
                    Operation = WGPUBlendOperation.Add
                }
            };

            var colorTargetState = new WGPUColorTargetState
            {
                Format = _Format,
                Blend = &blendState,
                WriteMask = WGPUColorWriteMask.All
            };

            byte[] fs_main_Utf8Bytes = Encoding.UTF8.GetBytes("fs_main");
            fixed (byte* p_fs_main_Utf8Bytes = fs_main_Utf8Bytes)
            {
                var fragmentState = new WGPUFragmentState
                {
                    Module = _Shader,
                    TargetCount = 1,
                    Targets = &colorTargetState,
                    EntryPoint = new WGPUStringView() { Data = p_fs_main_Utf8Bytes, Length = (nuint)"fs_main".Length }
                };
                byte[] vs_main_Utf8Bytes = Encoding.UTF8.GetBytes("vs_main");
                fixed (byte* p_vs_main_Utf8Bytes = vs_main_Utf8Bytes)
                {
                    var renderPipelineDescriptor = new WGPURenderPipelineDescriptor
                    {
                        Vertex = new WGPUVertexState
                        {
                            Module = _Shader,
                            EntryPoint = new WGPUStringView() { Data = p_vs_main_Utf8Bytes, Length = (nuint)"vs_main".Length },
                        },
                        Primitive = new WGPUPrimitiveState
                        {
                            Topology = WGPUPrimitiveTopology.TriangleList,
                            StripIndexFormat = WGPUIndexFormat.Undefined,
                            FrontFace = WGPUFrontFace.CCW,
                            CullMode = WGPUCullMode.None
                        },
                        Multisample = new WGPUMultisampleState
                        {
                            Count = 1,
                            Mask = 0xFFFFFFFF,
                            AlphaToCoverageEnabled = WGPU_FALSE
                        },
                        Fragment = (&fragmentState),
                        DepthStencil = null
                    };

                    _Pipeline = wgpuDeviceCreateRenderPipeline(_Device, &renderPipelineDescriptor);
                }
            }

            Debug.WriteLine($"Created pipeline {_Pipeline:X}");

            #endregion Create pipeline

            CreateSwapchain();
        }

        private void CreateSwapchain()
        {
            var surfaceConfiguration = new WGPUSurfaceConfiguration
            {
                AlphaMode = alphaModes[0],
                Usage = WGPUTextureUsage.RenderAttachment,
                Format = _Format,
                PresentMode = presentModes[0],
                Device = _Device,
                Width = (uint)GetWidth(Window),
                Height = (uint)GetHeight(Window),
            };

            wgpuSurfaceConfigure(_Surface, &surfaceConfiguration);
        }

        protected override void WindowOnRender(double delta)
        {
            WGPUSurfaceTexture surfaceTexture = default;
            wgpuSurfaceGetCurrentTexture(_Surface, &surfaceTexture);
            switch (surfaceTexture.Status)
            {
                case WGPUSurfaceGetCurrentTextureStatus.Timeout:
                case WGPUSurfaceGetCurrentTextureStatus.Outdated:
                case WGPUSurfaceGetCurrentTextureStatus.Lost:
                case WGPUSurfaceGetCurrentTextureStatus.Error:

                    // Recreate swapchain,
                    if (surfaceTexture.Texture.Handle != 0)
                        wgpuTextureRelease(surfaceTexture.Texture);
                    CreateSwapchain();
                    // Skip this frame
                    return;

                case WGPUSurfaceGetCurrentTextureStatus.Force32:
                    throw new Exception($"What is going on bros... {surfaceTexture.Status}");
            }
            var descrip = new WGPUTextureDescriptor()
            {
                Dimension = WGPUTextureDimension.TwoDimensions,
                Format = WGPUTextureFormat.RGBA8Unorm,
                Size = new WGPUExtent3D
                {
                    Width = (uint)GetWidth(Window),
                    Height = (uint)GetHeight(Window),
                    DepthOrArrayLayers = 1
                },
                Usage = WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.CopySrc,
            };

            var texture = surfaceTexture.Texture.Handle == 0 ? wgpuDeviceCreateTexture(_Device, &descrip) : surfaceTexture.Texture;
            WGPUTextureViewDescriptor wGPUTextureViewDescriptor = new WGPUTextureViewDescriptor()
            {
                ArrayLayerCount = 1,
                MipLevelCount = 1,
            };
            var view = wgpuTextureCreateView(texture, &wGPUTextureViewDescriptor);

            var commandEncoderDescriptor = new WGPUCommandEncoderDescriptor();

            var encoder = wgpuDeviceCreateCommandEncoder(_Device, &commandEncoderDescriptor);

            var colorAttachment = new WGPURenderPassColorAttachment
            {
                View = view,
                //resolveTarget = null,
                LoadOp = WGPULoadOp.Clear,
                StoreOp = WGPUStoreOp.Store,
                ClearValue = new WGPUColor
                {
                    R = 0,
                    G = 1,
                    B = 0,
                    A = 1
                },
                DepthSlice = WGPU_DEPTH_SLICE_UNDEFINED
            };

            var renderPassDescriptor = new WGPURenderPassDescriptor
            {
                ColorAttachments = &colorAttachment,
                ColorAttachmentCount = 1,
                DepthStencilAttachment = null
            };

            var renderPass = wgpuCommandEncoderBeginRenderPass(encoder, &renderPassDescriptor);

            wgpuRenderPassEncoderSetPipeline(renderPass, _Pipeline);
            wgpuRenderPassEncoderDraw(renderPass, 3, 1, 0, 0);
            wgpuRenderPassEncoderEnd(renderPass);

            var queue = wgpuDeviceGetQueue(_Device);

            var commandBuffer = wgpuCommandEncoderFinish(encoder, null);
            wgpuQueueSubmit(queue, 1, &commandBuffer);

            wgpuSurfacePresent(_Surface);
            wgpuCommandBufferRelease(commandBuffer);
            wgpuRenderPassEncoderRelease(renderPass);
            wgpuCommandEncoderRelease(encoder);
            wgpuTextureViewRelease(view);

            wgpuTextureRelease(texture);
        }

        protected override void WindowClosing()
        {
            wgpuShaderModuleRelease(_Shader);
            wgpuRenderPipelineRelease(_Pipeline);
            wgpuDeviceRelease(_Device);
            wgpuAdapterRelease(_Adapter);
            wgpuSurfaceRelease(_Surface);
            wgpuInstanceRelease(_Instance);
        }

        private void PrintAdapterFeatures()
        {
            var supportFeatures = new WGPUSupportedFeatures();
            wgpuAdapterGetFeatures(_Adapter, &supportFeatures);

            Debug.WriteLine("Adapter features:");

            for (var i = 0; i < (int)supportFeatures.FeatureCount; i++)
            {
                var feature = supportFeatures.Features[i];
                Debug.WriteLine($"\t{feature.ToString()}");
            }

            wgpuSupportedFeaturesFreeMembers(supportFeatures);
        }
    }
}