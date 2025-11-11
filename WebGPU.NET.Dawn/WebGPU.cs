namespace WebGPU.NET.Dawn
{
    public static partial class WebGPU
    {
#if ANDROID
        private const string webgpu_native_dll_name = "webgpu_dawn";
#elif IOS || MACCATALYST
        private const string webgpu_native_dll_name = "__Internal";
#else
        private const string webgpu_native_dll_name = "webgpu_dawn";
#endif

        partial struct WGPUExtent3D
        {
            public void INIT()
            {
                this.Width = 0;
                this.Height = 1;
                this.DepthOrArrayLayers = 1;
            }
        }

        partial struct WGPUMultisampleState
        {
            public void INIT()
            {
                this.Count = 1;
                this.Mask = 0xFFFFFFFF;
                this.AlphaToCoverageEnabled = WGPU_FALSE;
            }
        }

        partial struct WGPUPassTimestampWrites
        {
            public void INIT()
            {
                this.BeginningOfPassWriteIndex = WGPU_QUERY_SET_INDEX_UNDEFINED;
                this.EndOfPassWriteIndex = WGPU_QUERY_SET_INDEX_UNDEFINED;
            }
        }

        partial struct WGPURenderBundleEncoderDescriptor
        {
            public void INIT()
            {
                this.SampleCount = 1;
            }
        }

        partial struct WGPURenderPassDepthStencilAttachment
        {
            public void INIT()
            {
                this.DepthClearValue = WGPU_DEPTH_CLEAR_VALUE_UNDEFINED;
            }
        }

        partial struct WGPURenderPassMaxDrawCount
        {
            public void INIT()
            {
                this.MaxDrawCount = 50000000;
            }
        }

        partial struct WGPUSharedTextureMemoryD3D11BeginState
        {
            public void INIT()
            {
                this.RequiresEndAccessFence = WGPU_TRUE;
            }
        }

        partial struct WGPUSharedTextureMemoryIOSurfaceDescriptor
        {
            public void INIT()
            {
                this.AllowStorageBinding = WGPU_TRUE;
            }
        }

        partial struct WGPUStaticSamplerBindingLayout
        {
            public void INIT()
            {
                this.SampledTextureBinding = WGPU_LIMIT_U32_UNDEFINED;
            }
        }

        partial struct WGPUSurfaceConfiguration
        {
            public void INIT()
            {
                this.Usage = WGPUTextureUsage.RenderAttachment;
                this.AlphaMode = WGPUCompositeAlphaMode.Auto;
            }
        }

        partial struct WGPUTexelBufferViewDescriptor
        {
            public void INIT()
            {
                this.Size = WGPU_WHOLE_SIZE;
            }
        }

        partial struct WGPUTexelCopyBufferLayout
        {
            public void INIT()
            {
                this.BytesPerRow = WGPU_COPY_STRIDE_UNDEFINED;
                this.RowsPerImage = WGPU_COPY_STRIDE_UNDEFINED;
            }
        }

        partial struct WGPUDepthStencilState
        {
            public void INIT()
            {
                this.StencilReadMask = 0xFFFFFFFF;
                this.StencilWriteMask = 0xFFFFFFFF;
            }
        }

        partial struct WGPULimits
        {
            public void INIT()
            {
                this.MaxTextureDimension1D = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxTextureDimension2D = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxTextureDimension3D = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxTextureArrayLayers = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxBindGroups = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxBindGroupsPlusVertexBuffers = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxBindingsPerBindGroup = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxDynamicUniformBuffersPerPipelineLayout = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxDynamicStorageBuffersPerPipelineLayout = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxSampledTexturesPerShaderStage = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxSamplersPerShaderStage = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxStorageBuffersPerShaderStage = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxStorageTexturesPerShaderStage = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxUniformBuffersPerShaderStage = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxUniformBufferBindingSize = WGPU_LIMIT_U64_UNDEFINED;
                this.MaxStorageBufferBindingSize = WGPU_LIMIT_U64_UNDEFINED;
                this.MinUniformBufferOffsetAlignment = WGPU_LIMIT_U32_UNDEFINED;
                this.MinStorageBufferOffsetAlignment = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxVertexBuffers = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxBufferSize = WGPU_LIMIT_U64_UNDEFINED;
                this.MaxVertexAttributes = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxVertexBufferArrayStride = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxInterStageShaderVariables = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxColorAttachments = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxColorAttachmentBytesPerSample = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxComputeWorkgroupStorageSize = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxComputeInvocationsPerWorkgroup = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxComputeWorkgroupSizeX = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxComputeWorkgroupSizeY = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxComputeWorkgroupSizeZ = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxComputeWorkgroupsPerDimension = WGPU_LIMIT_U32_UNDEFINED;
                this.MaxImmediateSize = WGPU_LIMIT_U32_UNDEFINED;
            }
        }

        partial struct WGPUSamplerDescriptor
        {
            public void INIT()
            {
                this.LodMaxClamp = 32.0f;
                this.MaxAnisotropy = 1;
            }
        }

        partial struct WGPUTextureDescriptor
        {
            public void INIT()
            {
                this.MipLevelCount = 1;
                this.SampleCount = 1;
            }
        }

        partial struct WGPUColorTargetState
        {
            public void INIT()
            {
                this.WriteMask = WGPUColorWriteMask.All;
            }
        }
    }
}