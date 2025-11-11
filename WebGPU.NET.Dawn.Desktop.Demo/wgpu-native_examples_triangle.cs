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
            public WGPUInstance instance;
            public WGPUSurface surface;
            public WGPUAdapter adapter;
            public WGPUDevice device;
            public WGPUSurfaceConfiguration config = new();
        }

        private Demo demo;
        private WGPURenderPipeline render_pipeline;
        private WGPUQueue queue;
        private WGPUPipelineLayout pipeline_layout;
        private WGPUSurfaceCapabilities surface_capabilities;
        private WGPUShaderModule shader_module;

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

            demo.config.width = (uint)size.X;
            demo.config.height = (uint)size.Y;

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
                compatibleSurface = demo.surface,
            };
            var pWGPURequestAdapterCallback = Marshal.GetFunctionPointerForDelegate<WGPURequestAdapterCallback>((status, _adapter, str, data1, data2) =>
            {
                if (status == WGPURequestAdapterStatus.Success)
                {
                    demo.adapter = _adapter;
                }
                else
                {
                    var bytes = new Span<byte>(str.data, (int)str.length);
                    Trace.WriteLine(Encoding.UTF8.GetString(bytes));
                }
            });
            wgpuInstanceRequestAdapter(demo.instance, &wGPURequestAdapterOptions, new WGPURequestAdapterCallbackInfo()
            {
                mode = WGPUCallbackMode.AllowSpontaneous,
                callback = (delegate* unmanaged[Cdecl]<WGPURequestAdapterStatus, WGPUAdapter, WGPUStringView, void*, void*, void>)pWGPURequestAdapterCallback
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
                    var bytes = new Span<byte>(str.data, (int)str.length);
                    Trace.WriteLine(Encoding.UTF8.GetString(bytes));
                }
            });
            wgpuAdapterRequestDevice(demo.adapter, null, new WGPURequestDeviceCallbackInfo()
            {
                mode = WGPUCallbackMode.AllowSpontaneous,
                callback = (delegate* unmanaged[Cdecl]<WGPURequestDeviceStatus, WGPUDevice, WGPUStringView, void*, void*, void>)pWGPURequestDeviceCallback
            });
            Debug.Assert(demo.device.Handle != 0, $"{nameof(wgpuAdapterRequestDevice)} failed");

            queue = wgpuDeviceGetQueue(demo.device);
            Debug.Assert(queue.Handle != 0, $"{nameof(wgpuDeviceGetQueue)} failed");

            WGPUShaderModule frmwrk_load_shader_module(WGPUDevice device, string shaderStr)
            {
                byte[] shader_Utf8Bytes = Encoding.UTF8.GetBytes(shaderStr);
                fixed (byte* p_shader_Utf8Bytes = shader_Utf8Bytes)
                {
                    var shader = new WGPUShaderSourceWGSL()
                    {
                        code = new WGPUStringView() { data = p_shader_Utf8Bytes, length = (nuint)shaderStr.Length },
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
                format = surface_capabilities.formats[0],
                //Blend = &blendState,
                writeMask = WGPUColorWriteMask.All
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
                        module = shader_module,
                        targetCount = 1,
                        targets = &colorTargetState,
                        entryPoint = new WGPUStringView() { data = p_fs_main_Utf8Bytes, length = (nuint)fs_main_Str.Length }
                    };

                    var renderPipelineDescriptor = new WGPURenderPipelineDescriptor
                    {
                        layout = pipeline_layout,
                        vertex = new WGPUVertexState
                        {
                            module = shader_module,
                            entryPoint = new WGPUStringView() { data = p_vs_main_Utf8Bytes, length = (nuint)vs_main_Str.Length },
                        },
                        primitive = new WGPUPrimitiveState
                        {
                            topology = WGPUPrimitiveTopology.TriangleList,
                            //StripIndexFormat = WGPUIndexFormat.Undefined,
                            //FrontFace = WGPUFrontFace.CCW,
                            //CullMode = WGPUCullMode.None
                        },
                        multisample = new WGPUMultisampleState
                        {
                            count = 1,
                            mask = 0xFFFFFFFF,
                            //AlphaToCoverageEnabled = WGPU_FALSE
                        },
                        fragment = (&fragmentState),
                        depthStencil = null
                    };

                    render_pipeline = wgpuDeviceCreateRenderPipeline(demo.device, &renderPipelineDescriptor);
                    Debug.Assert(render_pipeline.Handle != 0, $"{nameof(wgpuDeviceCreateRenderPipeline)} failed");

                    demo.config = new WGPUSurfaceConfiguration()
                    {
                        device = demo.device,
                        usage = WGPUTextureUsage.RenderAttachment,
                        format = surface_capabilities.formats[0],
                        presentMode = WGPUPresentMode.Fifo,
                        alphaMode = surface_capabilities.alphaModes[0],
                    };

                    {
                        demo.config.width = this.GetWidth(this.Window);
                        demo.config.height = this.GetHeight(this.Window);
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
            switch (surface_texture.status)
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
                        if (surface_texture.texture.Handle != 0)
                        {
                            wgpuTextureRelease(surface_texture.texture);
                        }
                        var width = GetWidth(this.Window);
                        var height = GetHeight(this.Window);
                        if (width != 0 && height != 0)
                        {
                            demo.config.width = width;
                            demo.config.height = height;
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
                    Trace.TraceError($"get_current_texture status= {surface_texture.status}");
                    throw new NotImplementedException();
            }
            Debug.Assert(surface_texture.texture.Handle != 0, $"{nameof(wgpuSurfaceGetCurrentTexture)} failed");

            WGPUTextureView frame = wgpuTextureCreateView(surface_texture.texture, null);
            Debug.Assert(frame.Handle != 0, $"{nameof(wgpuTextureCreateView)} failed");

            var wGPUCommandEncoderDescriptor = new WGPUCommandEncoderDescriptor()
            {
            };

            WGPUCommandEncoder command_encoder = wgpuDeviceCreateCommandEncoder(demo.device, &wGPUCommandEncoderDescriptor);
            Debug.Assert(command_encoder.Handle != 0, $"{nameof(wgpuDeviceCreateCommandEncoder)} failed");

            var colorAttachments = new WGPURenderPassColorAttachment[]
            {
                new WGPURenderPassColorAttachment()
                {
                    view = frame,
                    loadOp = WGPULoadOp.Clear,
                    storeOp = WGPUStoreOp.Store,
                    depthSlice = WGPU_DEPTH_SLICE_UNDEFINED,
                    clearValue = new WGPUColor()
                    {
                        r = 0,
                        g = 1,
                        b = 0,
                        a = 1,
                    }
                }
            };

            WGPURenderPassEncoder render_pass_encoder;
            fixed (WGPURenderPassColorAttachment* pWGPURenderPassColorAttachment = colorAttachments)
            {
                var wGPURenderPassDescriptor = new WGPURenderPassDescriptor()
                {
                    colorAttachmentCount = 1,
                    colorAttachments = pWGPURenderPassColorAttachment
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
            WGPUCommandBuffer command_buffer = wgpuCommandEncoderFinish(command_encoder, &wGPUCommandBufferDescriptor);
            Debug.Assert(command_buffer.Handle != 0, $"{nameof(wgpuCommandEncoderFinish)} failed");

            wgpuQueueSubmit(queue, 1, &command_buffer);
            wgpuSurfacePresent(demo.surface);

            wgpuCommandBufferRelease(command_buffer);
            wgpuCommandEncoderRelease(command_encoder);
            wgpuTextureViewRelease(frame);
            wgpuTextureRelease(surface_texture.texture);
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