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
        private WGPUSurface _Surface;
        private WGPUTextureFormat _Format;

        private WGPUInstance _Instance;
        private WGPUAdapter _Adapter;
        private WGPUDevice _Device;
        private WGPUShaderModule _Shader;
        private WGPURenderPipeline _Pipeline;
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
                var bytes = new Span<byte>(str.data, (int)str.length);
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
                compatibleSurface = _Surface,
                backendType = WGPUBackendType.D3D12
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
                        var bytes = new Span<byte>(str.data, (int)str.length);
                        Trace.WriteLine(Encoding.UTF8.GetString(bytes));
                    }
                });
                wgpuInstanceRequestAdapter
                (
                    _Instance,
                    &requestAdapterOptions,
                    new WGPURequestAdapterCallbackInfo()
                    {
                        mode = WGPUCallbackMode.AllowSpontaneous,
                        callback = (delegate* unmanaged[Cdecl]<WGPURequestAdapterStatus, WGPUAdapter, WGPUStringView, void*, void*, void>)pWGPURequestAdapterCallback,
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
                var bytes = new Span<byte>(str.data, (int)str.length);
                Trace.WriteLine(Encoding.UTF8.GetString(bytes));
            });

            var pWGPUUncapturedErrorCallback = Marshal.GetFunctionPointerForDelegate<WGPUUncapturedErrorCallback>((_device, type, str, data1, data2) =>
            {
                var bytes = new Span<byte>(str.data, (int)str.length);
                Trace.WriteLine(Encoding.UTF8.GetString(bytes));
            });

            var deviceDescriptor = new WGPUDeviceDescriptor
            {
                deviceLostCallbackInfo = new WGPUDeviceLostCallbackInfo()
                {
                    mode = WGPUCallbackMode.AllowSpontaneous,
                    callback = (delegate* unmanaged[Cdecl]<WGPUDevice*, WGPUDeviceLostReason, WGPUStringView, void*, void*, void>)pWGPUDeviceLostCallback
                },
                uncapturedErrorCallbackInfo = new WGPUUncapturedErrorCallbackInfo()
                {
                    callback = (delegate* unmanaged[Cdecl]<WGPUDevice*, WGPUErrorType, WGPUStringView, void*, void*, void>)pWGPUUncapturedErrorCallback
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
                    var bytes = new Span<byte>(str.data, (int)str.length);
                    Trace.WriteLine(Encoding.UTF8.GetString(bytes));
                }
            });

            wgpuAdapterRequestDevice
            (
                _Adapter,
                &deviceDescriptor,
                new WGPURequestDeviceCallbackInfo()
                {
                    mode = WGPUCallbackMode.AllowSpontaneous,
                    callback = (delegate* unmanaged[Cdecl]<WGPURequestDeviceStatus, WGPUDevice, WGPUStringView, void*, void*, void>)pWGPURequestDeviceCallback
                }
            );
            Debug.WriteLine($"Got device {_Device:X}");

            #endregion Get device

            #region Log

#if !WGPUNATIVE
            var pWGPULoggingCallback = Marshal.GetFunctionPointerForDelegate<WGPULoggingCallback>((status, message, userData1, userData2) =>
            {
                var bytes = new Span<byte>(message.data, (int)message.length);
                Trace.WriteLine("Log:" + Encoding.UTF8.GetString(bytes));
            });

            wgpuDeviceSetLoggingCallback(_Device, new WGPULoggingCallbackInfo()
            {
                callback = (delegate* unmanaged[Cdecl]<WGPULoggingType, WGPUStringView, void*, void*, void>)pWGPULoggingCallback,
            });
#endif

            #endregion Log

            #region Load shader

            byte[] shader_Utf8Bytes = Encoding.UTF8.GetBytes(SHADER);
            fixed (byte* p_shader_Utf8Bytes = shader_Utf8Bytes)
            {
                var shader = new WGPUShaderSourceWGSL()
                {
                    code = new WGPUStringView() { data = p_shader_Utf8Bytes, length = (nuint)SHADER.Length },
                    chain = new WGPUChainedStruct
                    {
                        sType = WGPUSType.ShaderSourceWGSL,
                    }
                };

                var shaderModuleDescriptor = new WGPUShaderModuleDescriptor
                {
                    nextInChain = &shader.chain,
                    label = new WGPUStringView() { data = null, length = 0 }
                };

                _Shader = wgpuDeviceCreateShaderModule(_Device, &shaderModuleDescriptor);
            }

            Debug.WriteLine($"Created shader {_Shader:X}");

            #endregion Load shader

            WGPUSurfaceCapabilities surfaceCapabilities = default;
            wgpuSurfaceGetCapabilities(_Surface, _Adapter, &surfaceCapabilities);
            if (surfaceCapabilities.formatCount > 0)
            {
                Span<WGPUTextureFormat> surfaceFormats = new Span<WGPUTextureFormat>(surfaceCapabilities.formats, (int)surfaceCapabilities.formatCount);
                WGPUTextureFormat surfaceFormat = surfaceFormats[0];
                _Format = surfaceFormat;
            }
            presentModes = new Span<WGPUPresentMode>(surfaceCapabilities.presentModes, (int)surfaceCapabilities.presentModeCount).ToArray();
            alphaModes = new Span<WGPUCompositeAlphaMode>(surfaceCapabilities.alphaModes, (int)surfaceCapabilities.alphaModeCount).ToArray();

            #region Create pipeline

            var blendState = new WGPUBlendState
            {
                color = new WGPUBlendComponent
                {
                    srcFactor = WGPUBlendFactor.One,
                    dstFactor = WGPUBlendFactor.Zero,
                    operation = WGPUBlendOperation.Add
                },
                alpha = new WGPUBlendComponent
                {
                    srcFactor = WGPUBlendFactor.One,
                    dstFactor = WGPUBlendFactor.Zero,
                    operation = WGPUBlendOperation.Add
                }
            };

            var colorTargetState = new WGPUColorTargetState
            {
                format = _Format,
                blend = &blendState,
                writeMask = WGPUColorWriteMask.All
            };

            byte[] fs_main_Utf8Bytes = Encoding.UTF8.GetBytes("fs_main");
            fixed (byte* p_fs_main_Utf8Bytes = fs_main_Utf8Bytes)
            {
                var fragmentState = new WGPUFragmentState
                {
                    module = _Shader,
                    targetCount = 1,
                    targets = &colorTargetState,
                    entryPoint = new WGPUStringView() { data = p_fs_main_Utf8Bytes, length = (nuint)"fs_main".Length }
                };
                byte[] vs_main_Utf8Bytes = Encoding.UTF8.GetBytes("vs_main");
                fixed (byte* p_vs_main_Utf8Bytes = vs_main_Utf8Bytes)
                {
                    var renderPipelineDescriptor = new WGPURenderPipelineDescriptor
                    {
                        vertex = new WGPUVertexState
                        {
                            module = _Shader,
                            entryPoint = new WGPUStringView() { data = p_vs_main_Utf8Bytes, length = (nuint)"vs_main".Length },
                        },
                        primitive = new WGPUPrimitiveState
                        {
                            topology = WGPUPrimitiveTopology.TriangleList,
                            stripIndexFormat = WGPUIndexFormat.Undefined,
                            frontFace = WGPUFrontFace.CCW,
                            cullMode = WGPUCullMode.None
                        },
                        multisample = new WGPUMultisampleState
                        {
                            count = 1,
                            mask = 0xFFFFFFFF,
                            alphaToCoverageEnabled = WGPU_FALSE
                        },
                        fragment = (&fragmentState),
                        depthStencil = null
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
                alphaMode = alphaModes[0],
                usage = WGPUTextureUsage.RenderAttachment,
                format = _Format,
                presentMode = presentModes[0],
                device = _Device,
                width = (uint)GetWidth(Window),
                height = (uint)GetHeight(Window),
            };

            wgpuSurfaceConfigure(_Surface, &surfaceConfiguration);
        }

        protected override void WindowOnRender(double delta)
        {
            WGPUSurfaceTexture surfaceTexture = default;
            wgpuSurfaceGetCurrentTexture(_Surface, &surfaceTexture);
            switch (surfaceTexture.status)
            {
                case WGPUSurfaceGetCurrentTextureStatus.Timeout:
                case WGPUSurfaceGetCurrentTextureStatus.Outdated:
                case WGPUSurfaceGetCurrentTextureStatus.Lost:
                case WGPUSurfaceGetCurrentTextureStatus.Error:

                    // Recreate swapchain,
                    if (surfaceTexture.texture.Handle != 0)
                        wgpuTextureRelease(surfaceTexture.texture);
                    CreateSwapchain();
                    // Skip this frame
                    return;

                case WGPUSurfaceGetCurrentTextureStatus.Force32:
                    throw new Exception($"What is going on bros... {surfaceTexture.status}");
            }
            var descrip = new WGPUTextureDescriptor()
            {
                dimension = WGPUTextureDimension.TwoDimensions,
                format = WGPUTextureFormat.RGBA8Unorm,
                size = new WGPUExtent3D
                {
                    width = (uint)GetWidth(Window),
                    height = (uint)GetHeight(Window),
                    depthOrArrayLayers = 1
                },
                usage = WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.CopySrc,
            };

            var texture = surfaceTexture.texture.Handle == 0 ? wgpuDeviceCreateTexture(_Device, &descrip) : surfaceTexture.texture;
            WGPUTextureViewDescriptor wGPUTextureViewDescriptor = new WGPUTextureViewDescriptor()
            {
                arrayLayerCount = 1,
                mipLevelCount = 1,
            };
            var view = wgpuTextureCreateView(texture, &wGPUTextureViewDescriptor);

            var commandEncoderDescriptor = new WGPUCommandEncoderDescriptor();

            var encoder = wgpuDeviceCreateCommandEncoder(_Device, &commandEncoderDescriptor);

            var colorAttachment = new WGPURenderPassColorAttachment
            {
                view = view,
                //resolveTarget = null,
                loadOp = WGPULoadOp.Clear,
                storeOp = WGPUStoreOp.Store,
                clearValue = new WGPUColor
                {
                    r = 0,
                    g = 1,
                    b = 0,
                    a = 1
                },
                depthSlice = WGPU_DEPTH_SLICE_UNDEFINED
            };

            var renderPassDescriptor = new WGPURenderPassDescriptor
            {
                colorAttachments = &colorAttachment,
                colorAttachmentCount = 1,
                depthStencilAttachment = null
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

            for (var i = 0; i < (int)supportFeatures.featureCount; i++)
            {
                var feature = supportFeatures.features[i];
                Debug.WriteLine($"\t{feature.ToString()}");
            }

            wgpuSupportedFeaturesFreeMembers(supportFeatures);
        }
    }
}