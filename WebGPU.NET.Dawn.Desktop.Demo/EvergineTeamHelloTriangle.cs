using Silk.NET.Core.Native;
using Silk.NET.Maths;
using System.Diagnostics;
using System.Numerics;
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
    /// https://github.com/EvergineTeam/WebGPU.NET/blob/master/WebGPUGen/HelloTriangle/HelloTriangle.cs
    /// </summary>
    public unsafe class EvergineTeamHelloTriangle : BaseApp
    {
        private WGPUInstanceImpl _Instance;
        private WGPUSurfaceImpl _Surface;
        private WGPUAdapterImpl _Adapter;
        private WGPUAdapterInfo adapterProperties;
        private WGPULimits adapterLimits;
        private WGPUDeviceImpl _Device;
        private WGPUTextureFormat[] swapChainFormats;
        private WGPUQueueImpl _Queue;

        private WGPURenderPipelineImpl _Pipeline;
        private WGPUBufferImpl vertexBuffer;

        protected override void FramebufferResize(Vector2D<int> obj)
        {
        }

        protected override void WindowOnLoad()
        {
            this.InitWebGPU();
            this.InitResources();
        }

        private void InitWebGPU()
        {
#if WGPUNATIVE
            wgpuSetLogLevel(WGPULogLevel.Trace);
            var pWGPULoggingCallback = Marshal.GetFunctionPointerForDelegate<WGPULoggingCallback>((level, str, data1) =>
            {
                var bytes = new Span<byte>(str.Data, (int)str.Length);
                Trace.WriteLine(Encoding.UTF8.GetString(bytes));
            });
            wgpuSetLogCallback((delegate* unmanaged[Cdecl]<WGPULogLevel, WGPUStringView, void*, void>)pWGPULoggingCallback, (void*)IntPtr.Zero);
#endif

#if WGPUNATIVE
            WGPUInstanceExtras instanceExtras = new WGPUInstanceExtras()
            {
                Chain = new WGPUChainedStruct()
                {
                    SType = (WGPUSType)WGPUNativeSType.WGPUSTypeInstanceExtras,
                },
                Backends = WGPUInstanceBackend.Primary
            };

            WGPUInstanceDescriptor instanceDescriptor = new WGPUInstanceDescriptor()
            {
                NextInChain = &instanceExtras.Chain,
            };
            _Instance = wgpuCreateInstance(&instanceDescriptor);
#else
            _Instance = wgpuCreateInstance(null);
#endif

            _Surface = CreateWebGPUSurface(Window, _Instance);

            WGPURequestAdapterOptions options = new WGPURequestAdapterOptions()
            {
                NextInChain = null,
                CompatibleSurface = _Surface,
                PowerPreference = WGPUPowerPreference.HighPerformance,
                BackendType = WGPUBackendType.Undefined
            };

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
            wgpuInstanceRequestAdapter(_Instance, &options, new WGPURequestAdapterCallbackInfo()
            {
                Mode = WGPUCallbackMode.AllowSpontaneous,
                Callback = (delegate* unmanaged[Cdecl]<WGPURequestAdapterStatus, WGPUAdapterImpl, WGPUStringView, void*, void*, void>)pWGPURequestAdapterCallback
            });

            var pWGPUUncapturedErrorCallback = Marshal.GetFunctionPointerForDelegate<WGPUUncapturedErrorCallback>((_device, type, str, data1, data2) =>
            {
                var bytes = new Span<byte>(str.Data, (int)str.Length);
                Trace.WriteLine(Encoding.UTF8.GetString(bytes));
            });
            WGPUDeviceDescriptor deviceDescriptor = new WGPUDeviceDescriptor()
            {
                NextInChain = null,
                //Label = null,
                RequiredFeatures = (WGPUFeatureName*)0,
                RequiredLimits = null,
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
            wgpuAdapterRequestDevice(_Adapter, &deviceDescriptor, new WGPURequestDeviceCallbackInfo()
            {
                Mode = WGPUCallbackMode.AllowSpontaneous,
                Callback = (delegate* unmanaged[Cdecl]<WGPURequestDeviceStatus, WGPUDeviceImpl, WGPUStringView, void*, void*, void>)pWGPURequestDeviceCallback
            });

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

            _Queue = wgpuDeviceGetQueue(_Device);

            WGPUSurfaceCapabilities surfaceCapabilities = default;
            wgpuSurfaceGetCapabilities(_Surface, _Adapter, &surfaceCapabilities);
            this.swapChainFormats = new Span<WGPUTextureFormat>(surfaceCapabilities.Formats, (int)surfaceCapabilities.FormatCount).ToArray();
            var presentModes = new Span<WGPUPresentMode>(surfaceCapabilities.PresentModes, (int)surfaceCapabilities.PresentModeCount);
            var alphaModes = new Span<WGPUCompositeAlphaMode>(surfaceCapabilities.AlphaModes, (int)surfaceCapabilities.AlphaModeCount).ToArray();

            var width = this.GetWidth(this.Window);
            var height = this.GetHeight(this.Window);

            WGPUTextureFormat textureFormat = this.swapChainFormats[0];
            WGPUSurfaceConfiguration surfaceConfiguration = new WGPUSurfaceConfiguration()
            {
                NextInChain = null,
                Device = _Device,
                Format = textureFormat,
                Usage = WGPUTextureUsage.RenderAttachment,
                Width = (uint)width,
                Height = (uint)height,
                PresentMode = WGPUPresentMode.Immediate,// presentModes[0],
                AlphaMode = alphaModes[0]
            };

            wgpuSurfaceConfigure(_Surface, &surfaceConfiguration);
        }

        private void InitResources()
        {
            WGPUPipelineLayoutDescriptor layoutDescription = new()
            {
                NextInChain = null,
                BindGroupLayoutCount = 0,
            };

            var pipelineLayout = wgpuDeviceCreatePipelineLayout(_Device, &layoutDescription);

            WGPUShaderSourceWGSL shaderCodeDescriptor = new()
            {
                Chain = new WGPUChainedStruct()
                {
                    SType = WGPUSType.ShaderSourceWGSL,
                },
                Code = new WGPUStringView() { Data = (byte*)SilkMarshal.StringToPtr(shaderSource), Length = (nuint)shaderSource.Length }
            };

            WGPUShaderModuleDescriptor moduleDescriptor = new()
            {
                NextInChain = &shaderCodeDescriptor.Chain,
                //Count = 0,
                //Hints = null,
            };

            WGPUShaderModuleImpl shaderModule = wgpuDeviceCreateShaderModule(_Device, &moduleDescriptor);

            WGPUVertexAttribute* vertexAttributes = stackalloc WGPUVertexAttribute[2]
            {
                new WGPUVertexAttribute()
                {
                    Format = WGPUVertexFormat.Float32x4,
                    Offset = 0,
                    ShaderLocation = 0,
                },
                new WGPUVertexAttribute()
                {
                    Format = WGPUVertexFormat.Float32x4,
                    Offset = 16,
                    ShaderLocation = 1,
                },
            };

            WGPUVertexBufferLayout vertexLayout = new WGPUVertexBufferLayout()
            {
                AttributeCount = 2,
                Attributes = vertexAttributes,
                ArrayStride = (nuint)sizeof(Vector4) * 2,
                StepMode = WGPUVertexStepMode.Vertex,
            };

            WGPUBlendState blendState = new WGPUBlendState()
            {
                Color = new WGPUBlendComponent()
                {
                    SrcFactor = WGPUBlendFactor.One,
                    DstFactor = WGPUBlendFactor.Zero,
                    Operation = WGPUBlendOperation.Add,
                },
                Alpha = new WGPUBlendComponent()
                {
                    SrcFactor = WGPUBlendFactor.One,
                    DstFactor = WGPUBlendFactor.Zero,
                    Operation = WGPUBlendOperation.Add,
                }
            };

            WGPUColorTargetState colorTargetState = new WGPUColorTargetState()
            {
                NextInChain = null,
                Format = this.swapChainFormats[0],
                Blend = &blendState,
                WriteMask = WGPUColorWriteMask.All,
            };

            WGPUFragmentState fragmentState = new WGPUFragmentState()
            {
                NextInChain = null,
                Module = shaderModule,
                EntryPoint = new WGPUStringView() { Data = (byte*)SilkMarshal.StringToPtr("fragmentMain"), Length = (nuint)"fragmentMain".Length },
                ConstantCount = 0,
                Constants = null,
                TargetCount = 1,
                Targets = &colorTargetState,
            };

            WGPURenderPipelineDescriptor pipelineDescriptor = new WGPURenderPipelineDescriptor()
            {
                Layout = pipelineLayout,
                Vertex = new WGPUVertexState()
                {
                    BufferCount = 1,
                    Buffers = &vertexLayout,

                    Module = shaderModule,
                    EntryPoint = new WGPUStringView()
                    {
                        Data = (byte*)SilkMarshal.StringToPtr("vertexMain"),
                        Length = (nuint)"vertexMain".Length
                    },
                    ConstantCount = 0,
                    Constants = null,
                },
                Primitive = new WGPUPrimitiveState()
                {
                    Topology = WGPUPrimitiveTopology.TriangleList,
                    StripIndexFormat = WGPUIndexFormat.Undefined,
                    FrontFace = WGPUFrontFace.CCW,
                    CullMode = WGPUCullMode.None,
                },
                Fragment = &fragmentState,
                DepthStencil = null,
                Multisample = new WGPUMultisampleState()
                {
                    Count = 1,
                    Mask = ~0u,
                    AlphaToCoverageEnabled = 0,
                }
            };

            _Pipeline = wgpuDeviceCreateRenderPipeline(_Device, &pipelineDescriptor);

            wgpuShaderModuleRelease(shaderModule);

            Vector4* vertexData = stackalloc Vector4[]
            {
                new Vector4(0.0f, 0.5f, 0.5f, 1.0f),
                new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                new Vector4(0.5f, -0.5f, 0.5f, 1.0f),
                new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                new Vector4(-0.5f, -0.5f, 0.5f, 1.0f),
                new Vector4(0.0f, 0.0f, 1.0f, 1.0f)
            };

            var size = (nuint)(6 * sizeof(Vector4));
            WGPUBufferDescriptor bufferDescriptor = new WGPUBufferDescriptor()
            {
                NextInChain = null,
                Usage = (WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst),
                Size = size,
                MappedAtCreation = 0,
            };
            vertexBuffer = wgpuDeviceCreateBuffer(_Device, &bufferDescriptor);
            wgpuQueueWriteBuffer(_Queue, vertexBuffer, 0, vertexData, size);
        }

        protected override void WindowOnRender(double delta)
        {
            WGPUSurfaceTexture surface_texture = default;
            wgpuSurfaceGetCurrentTexture(_Surface, &surface_texture);

            // Getting the texture may fail, in particular if the window has been resized
            // and thus the target surface changed.
            if (surface_texture.Status == WGPUSurfaceGetCurrentTextureStatus.Timeout)
            {
                Debug.WriteLine("Cannot acquire next swap chain texture");
                return;
            }

            if (surface_texture.Status == WGPUSurfaceGetCurrentTextureStatus.Outdated)
            {
                Console.WriteLine("Surface texture is outdated, reconfigure the surface!");
                return;
            }

            WGPUTextureViewImpl frame = wgpuTextureCreateView(surface_texture.Texture, null);

            WGPUCommandEncoderDescriptor encoderDescriptor = new WGPUCommandEncoderDescriptor()
            {
                NextInChain = null,
            };
            WGPUCommandEncoderImpl command_encoder = wgpuDeviceCreateCommandEncoder(_Device, &encoderDescriptor);

            WGPURenderPassColorAttachment colorAttachment = new WGPURenderPassColorAttachment()
            {
                View = frame,
                ResolveTarget = new WGPUTextureViewImpl(nint.Zero),
                LoadOp = WGPULoadOp.Clear,
                StoreOp = WGPUStoreOp.Store,
                ClearValue = new WGPUColor() { A = 1.0f },
                DepthSlice = WGPU_DEPTH_SLICE_UNDEFINED
            };

            WGPURenderPassDescriptor renderPassDescriptor = new WGPURenderPassDescriptor()
            {
                NextInChain = null,
                ColorAttachmentCount = 1,
                ColorAttachments = &colorAttachment,
                DepthStencilAttachment = null,
                TimestampWrites = null,
            };

            WGPURenderPassEncoderImpl render_pass_encoder = wgpuCommandEncoderBeginRenderPass(command_encoder, &renderPassDescriptor);

            wgpuRenderPassEncoderSetPipeline(render_pass_encoder, _Pipeline);
            wgpuRenderPassEncoderSetVertexBuffer(render_pass_encoder, 0, vertexBuffer, 0, nuint.MaxValue);
            wgpuRenderPassEncoderDraw(render_pass_encoder, 3, 1, 0, 0);
            wgpuRenderPassEncoderEnd(render_pass_encoder);

            wgpuRenderPassEncoderRelease(render_pass_encoder);//https://github.com/gfx-rs/wgpu-native/issues/412

            WGPUCommandBufferDescriptor commandBufferDescriptor = new WGPUCommandBufferDescriptor()
            {
                NextInChain = null,
            };

            WGPUCommandBufferImpl command_buffer = wgpuCommandEncoderFinish(command_encoder, null);
            wgpuQueueSubmit(_Queue, 1, &command_buffer);
            wgpuSurfacePresent(_Surface);

            wgpuCommandBufferRelease(command_buffer);
            wgpuCommandEncoderRelease(command_encoder);
            wgpuTextureViewRelease(frame);
            wgpuTextureRelease(surface_texture.Texture);
        }

        protected override void WindowClosing()
        {
            wgpuSurfaceRelease(_Surface);
            wgpuDeviceDestroy(_Device);
            wgpuDeviceRelease(_Device);
            wgpuAdapterRelease(_Adapter);
            wgpuInstanceRelease(_Instance);
        }

        private string shaderSource = @"
struct VertexInput {
    @location(0) position: vec4f,
    @location(1) color: vec4f,
};

/**
 * A structure with fields labeled with builtins and locations can also be used
 * as *output* of the vertex shader, which is also the input of the fragment
 * shader.
 */
struct VertexOutput {
    @builtin(position) position: vec4f,
    // The location here does not refer to a vertex attribute, it just means
    // that this field must be handled by the rasterizer.
    // (It can also refer to another field of another struct that would be used
    // as input to the fragment shader.)
    @location(0) color: vec4f,
};

@vertex
fn vertexMain(in: VertexInput) -> VertexOutput {
    var out: VertexOutput;
    out.position = in.position;
    out.color = in.color; // forward to the fragment shader
    return out;
}

@fragment
fn fragmentMain(in: VertexOutput) -> @location(0) vec4f {
    return in.color;
}";
    }
}