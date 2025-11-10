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
    internal class wgpu_native_examples_triangle : BaseApp
    {
        private class Demo
        {
            public WGPUInstanceImpl instance;
            public WGPUSurfaceImpl surface;
            public WGPUAdapterImpl adapter;
            public WGPUDeviceImpl device;
            public WGPUSurfaceConfiguration config = new();
        }

        private Demo demo;
        private WGPURenderPipelineImpl render_pipeline;
        private WGPUQueueImpl queue;
        private WGPUPipelineLayoutImpl pipeline_layout;
        private WGPUSurfaceCapabilities surface_capabilities;
        private WGPUShaderModuleImpl shader_module;

        public wgpu_native_examples_triangle()
        {
            demo = new();
        }

        protected override void FramebufferResize(Vector2D<int> size)
        {
            if (size.X == 0 && size.Y == 0)
            {
                return;
            }

            demo.config.Width = (uint)size.X;
            demo.config.Height = (uint)size.Y;

            unsafe
            {
                fixed (WGPUSurfaceConfiguration* pConfig = &demo.config)
                {
                    wgpuSurfaceConfigure(demo.surface, pConfig);
                }
            }
        }

        protected override unsafe void WindowOnLoad()
        {
            demo.instance = wgpuCreateInstance(null);
            Debug.Assert(demo.instance.Handle != 0, $"{nameof(wgpuCreateInstance)} failed");

            demo.surface = CreateWebGPUSurface(this.Window, demo.instance);
            Debug.Assert(demo.surface.Handle != 0, $"{nameof(CreateWebGPUSurface)} failed");

            var wGPURequestAdapterOptions = new WGPURequestAdapterOptions()
            {
                CompatibleSurface = demo.surface,
            };
            var pWGPURequestAdapterCallback = Marshal.GetFunctionPointerForDelegate<WGPURequestAdapterCallback>((status, _adapter, str, data1, data2) =>
            {
                if (status == WGPURequestAdapterStatus.Success)
                {
                    demo.adapter = _adapter;
                }
                else
                {
                    var bytes = new Span<byte>(str.Data, (int)str.Length);
                    Trace.WriteLine(Encoding.UTF8.GetString(bytes));
                }
            });
            wgpuInstanceRequestAdapter(demo.instance, &wGPURequestAdapterOptions, new WGPURequestAdapterCallbackInfo()
            {
                Mode = WGPUCallbackMode.AllowSpontaneous,
                Callback = (delegate* unmanaged[Cdecl]<WGPURequestAdapterStatus, WGPUAdapterImpl, WGPUStringView, void*, void*, void>)pWGPURequestAdapterCallback
            });
            Debug.Assert(demo.adapter.Handle != 0, $"{nameof(wgpuInstanceRequestAdapter)} failed");

            var pWGPURequestDeviceCallback = Marshal.GetFunctionPointerForDelegate<WGPURequestDeviceCallback>((status, _device, str, data1, data2) =>
            {
                if (status == WGPURequestDeviceStatus.Success)
                {
                    demo.device = _device;
                }
                else
                {
                    var bytes = new Span<byte>(str.Data, (int)str.Length);
                    Trace.WriteLine(Encoding.UTF8.GetString(bytes));
                }
            });
            wgpuAdapterRequestDevice(demo.adapter, null, new WGPURequestDeviceCallbackInfo()
            {
                Mode = WGPUCallbackMode.AllowSpontaneous,
                Callback = (delegate* unmanaged[Cdecl]<WGPURequestDeviceStatus, WGPUDeviceImpl, WGPUStringView, void*, void*, void>)pWGPURequestDeviceCallback
            });
            Debug.Assert(demo.device.Handle != 0, $"{nameof(wgpuAdapterRequestDevice)} failed");

            queue = wgpuDeviceGetQueue(demo.device);
            Debug.Assert(queue.Handle != 0, $"{nameof(wgpuDeviceGetQueue)} failed");

            WGPUShaderModuleImpl frmwrk_load_shader_module(WGPUDeviceImpl device, string shaderStr)
            {
                byte[] shader_Utf8Bytes = Encoding.UTF8.GetBytes(shaderStr);
                fixed (byte* p_shader_Utf8Bytes = shader_Utf8Bytes)
                {
                    var shader = new WGPUShaderSourceWGSL()
                    {
                        Code = new WGPUStringView() { Data = p_shader_Utf8Bytes, Length = (nuint)shaderStr.Length },
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

                    return wgpuDeviceCreateShaderModule(device, &shaderModuleDescriptor);
                }
            }

            shader_module = frmwrk_load_shader_module(demo.device, shader);
            Debug.Assert(shader_module.Handle != 0, $"{nameof(frmwrk_load_shader_module)} failed");

            var wGPUPipelineLayoutDescriptor = new WGPUPipelineLayoutDescriptor()
            {
            };
            pipeline_layout = wgpuDeviceCreatePipelineLayout(demo.device, &wGPUPipelineLayoutDescriptor);
            Debug.Assert(pipeline_layout.Handle != 0, $"{nameof(wgpuDeviceCreatePipelineLayout)} failed");

            WGPUSurfaceCapabilities surface_capabilities = new();
            wgpuSurfaceGetCapabilities(demo.surface, demo.adapter, &surface_capabilities);
            this.surface_capabilities = surface_capabilities;

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
                Format = surface_capabilities.Formats[0],
                //Blend = &blendState,
                WriteMask = WGPUColorWriteMask.All
            };

            var fs_main_Str = "fs_main";
            var vs_main_Str = "vs_main";
            byte[] fs_main_Utf8Bytes = Encoding.UTF8.GetBytes(fs_main_Str);
            byte[] vs_main_Utf8Bytes = Encoding.UTF8.GetBytes(vs_main_Str);
            fixed (byte* p_fs_main_Utf8Bytes = fs_main_Utf8Bytes)
            {
                fixed (byte* p_vs_main_Utf8Bytes = vs_main_Utf8Bytes)
                {
                    var fragmentState = new WGPUFragmentState
                    {
                        Module = shader_module,
                        TargetCount = 1,
                        Targets = &colorTargetState,
                        EntryPoint = new WGPUStringView() { Data = p_fs_main_Utf8Bytes, Length = (nuint)fs_main_Str.Length }
                    };

                    var renderPipelineDescriptor = new WGPURenderPipelineDescriptor
                    {
                        Layout = pipeline_layout,
                        Vertex = new WGPUVertexState
                        {
                            Module = shader_module,
                            EntryPoint = new WGPUStringView() { Data = p_vs_main_Utf8Bytes, Length = (nuint)vs_main_Str.Length },
                        },
                        Primitive = new WGPUPrimitiveState
                        {
                            Topology = WGPUPrimitiveTopology.TriangleList,
                            //StripIndexFormat = WGPUIndexFormat.Undefined,
                            //FrontFace = WGPUFrontFace.CCW,
                            //CullMode = WGPUCullMode.None
                        },
                        Multisample = new WGPUMultisampleState
                        {
                            Count = 1,
                            Mask = 0xFFFFFFFF,
                            //AlphaToCoverageEnabled = WGPU_FALSE
                        },
                        Fragment = (&fragmentState),
                        DepthStencil = null
                    };

                    render_pipeline = wgpuDeviceCreateRenderPipeline(demo.device, &renderPipelineDescriptor);
                    Debug.Assert(render_pipeline.Handle != 0, $"{nameof(wgpuDeviceCreateRenderPipeline)} failed");

                    demo.config = new WGPUSurfaceConfiguration()
                    {
                        Device = demo.device,
                        Usage = WGPUTextureUsage.RenderAttachment,
                        Format = surface_capabilities.Formats[0],
                        PresentMode = WGPUPresentMode.Fifo,
                        AlphaMode = surface_capabilities.AlphaModes[0],
                    };

                    {
                        demo.config.Width = this.GetWidth(this.Window);
                        demo.config.Height = this.GetHeight(this.Window);
                    }

                    fixed (WGPUSurfaceConfiguration* pConfig = &demo.config)
                    {
                        wgpuSurfaceConfigure(demo.surface, pConfig);
                    }
                }
            }
        }

        protected override unsafe void WindowOnRender(double delta)
        {
            WGPUSurfaceTexture surface_texture;
            wgpuSurfaceGetCurrentTexture(demo.surface, &surface_texture);
            switch (surface_texture.Status)
            {
                case WGPUSurfaceGetCurrentTextureStatus.SuccessOptimal:
                case WGPUSurfaceGetCurrentTextureStatus.SuccessSuboptimal:
                    // All good, could handle suboptimal here
                    break;

                case WGPUSurfaceGetCurrentTextureStatus.Timeout:
                case WGPUSurfaceGetCurrentTextureStatus.Outdated:
                case WGPUSurfaceGetCurrentTextureStatus.Lost:
                    {
                        // Skip this frame, and re-configure surface.
                        if (surface_texture.Texture.Handle != 0)
                        {
                            wgpuTextureRelease(surface_texture.Texture);
                        }
                        var width = GetWidth(this.Window);
                        var height = GetHeight(this.Window);
                        if (width != 0 && height != 0)
                        {
                            demo.config.Width = width;
                            demo.config.Height = height;
                            fixed (WGPUSurfaceConfiguration* pConfig = &demo.config)
                            {
                                wgpuSurfaceConfigure(demo.surface, pConfig);
                            }
                        }
                        return;
                    }
#if WGPUNATIVE
                case WGPUSurfaceGetCurrentTextureStatus.OutOfMemory:
                case WGPUSurfaceGetCurrentTextureStatus.DeviceLost:
#endif
                case WGPUSurfaceGetCurrentTextureStatus.Force32:
                    // Fatal error
                    Trace.TraceError($"get_current_texture status= {surface_texture.Status}");
                    throw new NotImplementedException();
            }
            Debug.Assert(surface_texture.Texture.Handle != 0, $"{nameof(wgpuSurfaceGetCurrentTexture)} failed");

            WGPUTextureViewImpl frame = wgpuTextureCreateView(surface_texture.Texture, null);
            Debug.Assert(frame.Handle != 0, $"{nameof(wgpuTextureCreateView)} failed");

            var wGPUCommandEncoderDescriptor = new WGPUCommandEncoderDescriptor()
            {
            };

            WGPUCommandEncoderImpl command_encoder = wgpuDeviceCreateCommandEncoder(demo.device, &wGPUCommandEncoderDescriptor);
            Debug.Assert(command_encoder.Handle != 0, $"{nameof(wgpuDeviceCreateCommandEncoder)} failed");

            var colorAttachments = new WGPURenderPassColorAttachment[]
            {
                new WGPURenderPassColorAttachment()
                {
                    View = frame,
                    LoadOp = WGPULoadOp.Clear,
                    StoreOp = WGPUStoreOp.Store,
                    DepthSlice = WGPU_DEPTH_SLICE_UNDEFINED,
                    ClearValue = new WGPUColor()
                    {
                        R = 0,
                        G = 1,
                        B = 0,
                        A = 1,
                    }
                }
            };

            WGPURenderPassEncoderImpl render_pass_encoder;
            fixed (WGPURenderPassColorAttachment* pWGPURenderPassColorAttachment = colorAttachments)
            {
                var wGPURenderPassDescriptor = new WGPURenderPassDescriptor()
                {
                    ColorAttachmentCount = 1,
                    ColorAttachments = pWGPURenderPassColorAttachment
                };
                render_pass_encoder = wgpuCommandEncoderBeginRenderPass(command_encoder, &wGPURenderPassDescriptor);
            }
            Debug.Assert(render_pass_encoder.Handle != 0, $"{nameof(wgpuCommandEncoderBeginRenderPass)} failed");

            wgpuRenderPassEncoderSetPipeline(render_pass_encoder, render_pipeline);
            wgpuRenderPassEncoderDraw(render_pass_encoder, 3, 1, 0, 0);
            wgpuRenderPassEncoderEnd(render_pass_encoder);
            wgpuRenderPassEncoderRelease(render_pass_encoder);

            var wGPUCommandBufferDescriptor = new WGPUCommandBufferDescriptor()
            {
            };
            WGPUCommandBufferImpl command_buffer = wgpuCommandEncoderFinish(command_encoder, &wGPUCommandBufferDescriptor);
            Debug.Assert(command_buffer.Handle != 0, $"{nameof(wgpuCommandEncoderFinish)} failed");

            wgpuQueueSubmit(queue, 1, &command_buffer);
            wgpuSurfacePresent(demo.surface);

            wgpuCommandBufferRelease(command_buffer);
            wgpuCommandEncoderRelease(command_encoder);
            wgpuTextureViewRelease(frame);
            wgpuTextureRelease(surface_texture.Texture);
        }

        protected override void WindowClosing()
        {
            wgpuRenderPipelineRelease(render_pipeline);
            wgpuPipelineLayoutRelease(pipeline_layout);
            wgpuShaderModuleRelease(shader_module);
            wgpuSurfaceCapabilitiesFreeMembers(surface_capabilities);
            wgpuQueueRelease(queue);
            wgpuDeviceRelease(demo.device);
            wgpuAdapterRelease(demo.adapter);
            wgpuSurfaceRelease(demo.surface);
            wgpuInstanceRelease(demo.instance);
        }

        private const string shader = @"
@vertex
fn vs_main(@builtin(vertex_index) in_vertex_index: u32) -> @builtin(position) vec4<f32> {
    let x = f32(i32(in_vertex_index) - 1);
    let y = f32(i32(in_vertex_index & 1u) * 2 - 1);
    return vec4<f32>(x, y, 0.0, 1.0);
}

@fragment
fn fs_main() -> @location(0) vec4<f32> {
    return vec4<f32>(1.0, 0.0, 0.0, 1.0);
}
";
    }
}