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
        private WGPUInstance _Instance;
        private WGPUSurface _Surface;
        private WGPUAdapter _Adapter;
        private WGPUAdapterInfo adapterProperties;
        private WGPULimits adapterLimits;
        private WGPUDevice _Device;
        private WGPUTextureFormat[] swapChainFormats;
        private WGPUQueue _Queue;

        private WGPURenderPipeline _Pipeline;
        private WGPUBuffer vertexBuffer;

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
                var bytes = new Span<byte>(str.data, (int)str.length);
                Trace.WriteLine(Encoding.UTF8.GetString(bytes));
            });
            wgpuSetLogCallback((delegate* unmanaged[Cdecl]<WGPULogLevel, WGPUStringView, void*, void>)pWGPULoggingCallback, (void*)IntPtr.Zero);
#endif

#if WGPUNATIVE
            WGPUInstanceExtras instanceExtras = new WGPUInstanceExtras()
            {
                chain = new WGPUChainedStruct()
                {
                    sType = (WGPUSType)WGPUNativeSType.WGPUSTypeInstanceExtras,
                },
                backends = WGPUInstanceBackend.Primary
            };

            WGPUInstanceDescriptor instanceDescriptor = new WGPUInstanceDescriptor()
            {
                nextInChain = &instanceExtras.chain,
            };
            _Instance = wgpuCreateInstance(&instanceDescriptor);
#else
            _Instance = wgpuCreateInstance(null);
#endif

            _Surface = CreateWebGPUSurface(_Instance);

            WGPURequestAdapterOptions options = new WGPURequestAdapterOptions()
            {
                nextInChain = null,
                compatibleSurface = _Surface,
                powerPreference = WGPUPowerPreference.HighPerformance,
                backendType = WGPUBackendType.Vulkan
            };

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
            wgpuInstanceRequestAdapter(_Instance, &options, new WGPURequestAdapterCallbackInfo()
            {
                mode = WGPUCallbackMode.AllowSpontaneous,
                callback = (delegate* unmanaged[Cdecl]<WGPURequestAdapterStatus, WGPUAdapter, WGPUStringView, void*, void*, void>)pWGPURequestAdapterCallback
            });

            var pWGPUUncapturedErrorCallback = Marshal.GetFunctionPointerForDelegate<WGPUUncapturedErrorCallback>((_device, type, str, data1, data2) =>
            {
                var bytes = new Span<byte>(str.data, (int)str.length);
                Trace.WriteLine(Encoding.UTF8.GetString(bytes));
            });
            WGPUDeviceDescriptor deviceDescriptor = new WGPUDeviceDescriptor()
            {
                nextInChain = null,
                //Label = null,
                requiredFeatures = (WGPUFeatureName*)0,
                requiredLimits = null,
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
            wgpuAdapterRequestDevice(_Adapter, &deviceDescriptor, new WGPURequestDeviceCallbackInfo()
            {
                mode = WGPUCallbackMode.AllowSpontaneous,
                callback = (delegate* unmanaged[Cdecl]<WGPURequestDeviceStatus, WGPUDevice, WGPUStringView, void*, void*, void>)pWGPURequestDeviceCallback
            });

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

            _Queue = wgpuDeviceGetQueue(_Device);

            WGPUSurfaceCapabilities surfaceCapabilities = default;
            wgpuSurfaceGetCapabilities(_Surface, _Adapter, &surfaceCapabilities);
            this.swapChainFormats = new Span<WGPUTextureFormat>(surfaceCapabilities.formats, (int)surfaceCapabilities.formatCount).ToArray();
            var presentModes = new Span<WGPUPresentMode>(surfaceCapabilities.presentModes, (int)surfaceCapabilities.presentModeCount);
            var alphaModes = new Span<WGPUCompositeAlphaMode>(surfaceCapabilities.alphaModes, (int)surfaceCapabilities.alphaModeCount).ToArray();

            var width = this.GetWidth();
            var height = this.GetHeight();

            WGPUTextureFormat textureFormat = this.swapChainFormats[0];
            WGPUSurfaceConfiguration surfaceConfiguration = new WGPUSurfaceConfiguration()
            {
                nextInChain = null,
                device = _Device,
                format = textureFormat,
                usage = WGPUTextureUsage.RenderAttachment,
                width = (uint)width,
                height = (uint)height,
                presentMode = WGPUPresentMode.Immediate,// presentModes[0],
                alphaMode = alphaModes[0]
            };

            wgpuSurfaceConfigure(_Surface, &surfaceConfiguration);
        }

        private void InitResources()
        {
            WGPUPipelineLayoutDescriptor layoutDescription = new()
            {
                nextInChain = null,
                bindGroupLayoutCount = 0,
            };

            var pipelineLayout = wgpuDeviceCreatePipelineLayout(_Device, &layoutDescription);

            WGPUShaderSourceWGSL shaderCodeDescriptor = new()
            {
                chain = new WGPUChainedStruct()
                {
                    sType = WGPUSType.ShaderSourceWGSL,
                },
                code = new WGPUStringView() { data = (byte*)SilkMarshal.StringToPtr(shaderSource), length = (nuint)shaderSource.Length }
            };

            WGPUShaderModuleDescriptor moduleDescriptor = new()
            {
                nextInChain = &shaderCodeDescriptor.chain,
                //Count = 0,
                //Hints = null,
            };

            WGPUShaderModule shaderModule = wgpuDeviceCreateShaderModule(_Device, &moduleDescriptor);

            WGPUVertexAttribute* vertexAttributes = stackalloc WGPUVertexAttribute[2]
            {
                new WGPUVertexAttribute()
                {
                    format = WGPUVertexFormat.Float32x4,
                    offset = 0,
                    shaderLocation = 0,
                },
                new WGPUVertexAttribute()
                {
                    format = WGPUVertexFormat.Float32x4,
                    offset = 16,
                    shaderLocation = 1,
                },
            };

            WGPUVertexBufferLayout vertexLayout = new WGPUVertexBufferLayout()
            {
                attributeCount = 2,
                attributes = vertexAttributes,
                arrayStride = (nuint)sizeof(Vector4) * 2,
                stepMode = WGPUVertexStepMode.Vertex,
            };

            WGPUBlendState blendState = new WGPUBlendState()
            {
                color = new WGPUBlendComponent()
                {
                    srcFactor = WGPUBlendFactor.One,
                    dstFactor = WGPUBlendFactor.Zero,
                    operation = WGPUBlendOperation.Add,
                },
                alpha = new WGPUBlendComponent()
                {
                    srcFactor = WGPUBlendFactor.One,
                    dstFactor = WGPUBlendFactor.Zero,
                    operation = WGPUBlendOperation.Add,
                }
            };

            WGPUColorTargetState colorTargetState = new WGPUColorTargetState()
            {
                nextInChain = null,
                format = this.swapChainFormats[0],
                blend = &blendState,
                writeMask = WGPUColorWriteMask.All,
            };

            WGPUFragmentState fragmentState = new WGPUFragmentState()
            {
                nextInChain = null,
                module = shaderModule,
                entryPoint = new WGPUStringView() { data = (byte*)SilkMarshal.StringToPtr("fragmentMain"), length = (nuint)"fragmentMain".Length },
                constantCount = 0,
                constants = null,
                targetCount = 1,
                targets = &colorTargetState,
            };

            WGPURenderPipelineDescriptor pipelineDescriptor = new WGPURenderPipelineDescriptor()
            {
                layout = pipelineLayout,
                vertex = new WGPUVertexState()
                {
                    bufferCount = 1,
                    buffers = &vertexLayout,

                    module = shaderModule,
                    entryPoint = new WGPUStringView()
                    {
                        data = (byte*)SilkMarshal.StringToPtr("vertexMain"),
                        length = (nuint)"vertexMain".Length
                    },
                    constantCount = 0,
                    constants = null,
                },
                primitive = new WGPUPrimitiveState()
                {
                    topology = WGPUPrimitiveTopology.TriangleList,
                    stripIndexFormat = WGPUIndexFormat.Undefined,
                    frontFace = WGPUFrontFace.CCW,
                    cullMode = WGPUCullMode.None,
                },
                fragment = &fragmentState,
                depthStencil = null,
                multisample = new WGPUMultisampleState()
                {
                    count = 1,
                    mask = ~0u,
                    alphaToCoverageEnabled = 0,
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
                nextInChain = null,
                usage = (WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst),
                size = size,
                mappedAtCreation = 0,
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
            if (surface_texture.status == WGPUSurfaceGetCurrentTextureStatus.Timeout)
            {
                Debug.WriteLine("Cannot acquire next swap chain texture");
                return;
            }

            if (surface_texture.status == WGPUSurfaceGetCurrentTextureStatus.Outdated)
            {
                Console.WriteLine("Surface texture is outdated, reconfigure the surface!");
                return;
            }

            WGPUTextureView frame = wgpuTextureCreateView(surface_texture.texture, null);

            WGPUCommandEncoderDescriptor encoderDescriptor = new WGPUCommandEncoderDescriptor()
            {
                nextInChain = null,
            };
            WGPUCommandEncoder command_encoder = wgpuDeviceCreateCommandEncoder(_Device, &encoderDescriptor);

            WGPURenderPassColorAttachment colorAttachment = new WGPURenderPassColorAttachment()
            {
                view = frame,
                resolveTarget = new WGPUTextureView(nint.Zero),
                loadOp = WGPULoadOp.Clear,
                storeOp = WGPUStoreOp.Store,
                clearValue = new WGPUColor() { a = 1.0f },
                depthSlice = WGPU_DEPTH_SLICE_UNDEFINED
            };

            WGPURenderPassDescriptor renderPassDescriptor = new WGPURenderPassDescriptor()
            {
                nextInChain = null,
                colorAttachmentCount = 1,
                colorAttachments = &colorAttachment,
                depthStencilAttachment = null,
                timestampWrites = null,
            };

            WGPURenderPassEncoder render_pass_encoder = wgpuCommandEncoderBeginRenderPass(command_encoder, &renderPassDescriptor);

            wgpuRenderPassEncoderSetPipeline(render_pass_encoder, _Pipeline);
            wgpuRenderPassEncoderSetVertexBuffer(render_pass_encoder, 0, vertexBuffer, 0, nuint.MaxValue);
            wgpuRenderPassEncoderDraw(render_pass_encoder, 3, 1, 0, 0);
            wgpuRenderPassEncoderEnd(render_pass_encoder);

            wgpuRenderPassEncoderRelease(render_pass_encoder);//https://github.com/gfx-rs/wgpu-native/issues/412

            WGPUCommandBufferDescriptor commandBufferDescriptor = new WGPUCommandBufferDescriptor()
            {
                nextInChain = null,
            };

            WGPUCommandBuffer command_buffer = wgpuCommandEncoderFinish(command_encoder, null);
            wgpuQueueSubmit(_Queue, 1, &command_buffer);
            wgpuSurfacePresent(_Surface);

            wgpuCommandBufferRelease(command_buffer);
            wgpuCommandEncoderRelease(command_encoder);
            wgpuTextureViewRelease(frame);
            wgpuTextureRelease(surface_texture.texture);
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