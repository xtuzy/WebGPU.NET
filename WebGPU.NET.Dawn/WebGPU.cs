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
            public WGPUExtent3D()
            {
                this.width = 0;
                this.height = 1;
                this.depthOrArrayLayers = 1;
            }
        }

        partial struct WGPUMultisampleState
        {
            public WGPUMultisampleState()
            {
                this.count = 1;
                this.mask = 0xFFFFFFFF;
                this.alphaToCoverageEnabled = WGPU_FALSE;
            }
        }

        partial struct WGPUPassTimestampWrites
        {
            public WGPUPassTimestampWrites()
            {
                this.beginningOfPassWriteIndex = WGPU_QUERY_SET_INDEX_UNDEFINED;
                this.endOfPassWriteIndex = WGPU_QUERY_SET_INDEX_UNDEFINED;
            }
        }

        partial struct WGPURenderBundleEncoderDescriptor
        {
            public WGPURenderBundleEncoderDescriptor()
            {
                this.sampleCount = 1;
            }
        }

        partial struct WGPURenderPassDepthStencilAttachment
        {
            public WGPURenderPassDepthStencilAttachment()
            {
                this.depthClearValue = WGPU_DEPTH_CLEAR_VALUE_UNDEFINED;
            }
        }

        partial struct WGPURenderPassMaxDrawCount
        {
            public WGPURenderPassMaxDrawCount()
            {
                this.maxDrawCount = 50000000;
            }
        }

        partial struct WGPUSharedTextureMemoryD3D11BeginState
        {
            public WGPUSharedTextureMemoryD3D11BeginState()
            {
                this.requiresEndAccessFence = WGPU_TRUE;
            }
        }

        partial struct WGPUSharedTextureMemoryIOSurfaceDescriptor
        {
            public WGPUSharedTextureMemoryIOSurfaceDescriptor()
            {
                this.allowStorageBinding = WGPU_TRUE;
            }
        }

        partial struct WGPUStaticSamplerBindingLayout
        {
            public WGPUStaticSamplerBindingLayout()
            {
                this.sampledTextureBinding = WGPU_LIMIT_U32_UNDEFINED;
            }
        }

        partial struct WGPUSurfaceConfiguration
        {
            public WGPUSurfaceConfiguration()
            {
                this.usage = WGPUTextureUsage.RenderAttachment;
                this.alphaMode = WGPUCompositeAlphaMode.Auto;
            }
        }

        partial struct WGPUTexelBufferViewDescriptor
        {
            public WGPUTexelBufferViewDescriptor()
            {
                this.size = WGPU_WHOLE_SIZE;
            }
        }

        partial struct WGPUTexelCopyBufferLayout
        {
            public WGPUTexelCopyBufferLayout()
            {
                this.bytesPerRow = WGPU_COPY_STRIDE_UNDEFINED;
                this.rowsPerImage = WGPU_COPY_STRIDE_UNDEFINED;
            }
        }

        partial struct WGPUDepthStencilState
        {
            public WGPUDepthStencilState()
            {
                this.stencilReadMask = 0xFFFFFFFF;
                this.stencilWriteMask = 0xFFFFFFFF;
            }
        }

        partial struct WGPULimits
        {
            public WGPULimits()
            {
                this.maxTextureDimension1D = WGPU_LIMIT_U32_UNDEFINED;
                this.maxTextureDimension2D = WGPU_LIMIT_U32_UNDEFINED;
                this.maxTextureDimension3D = WGPU_LIMIT_U32_UNDEFINED;
                this.maxTextureArrayLayers = WGPU_LIMIT_U32_UNDEFINED;
                this.maxBindGroups = WGPU_LIMIT_U32_UNDEFINED;
                this.maxBindGroupsPlusVertexBuffers = WGPU_LIMIT_U32_UNDEFINED;
                this.maxBindingsPerBindGroup = WGPU_LIMIT_U32_UNDEFINED;
                this.maxDynamicUniformBuffersPerPipelineLayout = WGPU_LIMIT_U32_UNDEFINED;
                this.maxDynamicStorageBuffersPerPipelineLayout = WGPU_LIMIT_U32_UNDEFINED;
                this.maxSampledTexturesPerShaderStage = WGPU_LIMIT_U32_UNDEFINED;
                this.maxSamplersPerShaderStage = WGPU_LIMIT_U32_UNDEFINED;
                this.maxStorageBuffersPerShaderStage = WGPU_LIMIT_U32_UNDEFINED;
                this.maxStorageTexturesPerShaderStage = WGPU_LIMIT_U32_UNDEFINED;
                this.maxUniformBuffersPerShaderStage = WGPU_LIMIT_U32_UNDEFINED;
                this.maxUniformBufferBindingSize = WGPU_LIMIT_U64_UNDEFINED;
                this.maxStorageBufferBindingSize = WGPU_LIMIT_U64_UNDEFINED;
                this.minUniformBufferOffsetAlignment = WGPU_LIMIT_U32_UNDEFINED;
                this.minStorageBufferOffsetAlignment = WGPU_LIMIT_U32_UNDEFINED;
                this.maxVertexBuffers = WGPU_LIMIT_U32_UNDEFINED;
                this.maxBufferSize = WGPU_LIMIT_U64_UNDEFINED;
                this.maxVertexAttributes = WGPU_LIMIT_U32_UNDEFINED;
                this.maxVertexBufferArrayStride = WGPU_LIMIT_U32_UNDEFINED;
                this.maxInterStageShaderVariables = WGPU_LIMIT_U32_UNDEFINED;
                this.maxColorAttachments = WGPU_LIMIT_U32_UNDEFINED;
                this.maxColorAttachmentBytesPerSample = WGPU_LIMIT_U32_UNDEFINED;
                this.maxComputeWorkgroupStorageSize = WGPU_LIMIT_U32_UNDEFINED;
                this.maxComputeInvocationsPerWorkgroup = WGPU_LIMIT_U32_UNDEFINED;
                this.maxComputeWorkgroupSizeX = WGPU_LIMIT_U32_UNDEFINED;
                this.maxComputeWorkgroupSizeY = WGPU_LIMIT_U32_UNDEFINED;
                this.maxComputeWorkgroupSizeZ = WGPU_LIMIT_U32_UNDEFINED;
                this.maxComputeWorkgroupsPerDimension = WGPU_LIMIT_U32_UNDEFINED;
                this.maxImmediateSize = WGPU_LIMIT_U32_UNDEFINED;
            }
        }

        partial struct WGPUSamplerDescriptor
        {
            public WGPUSamplerDescriptor()
            {
                this.lodMaxClamp = 32.0f;
                this.maxAnisotropy = 1;
            }
        }

        partial struct WGPUTextureDescriptor
        {
            public WGPUTextureDescriptor()
            {
                this.mipLevelCount = 1;
                this.sampleCount = 1;
            }
        }

        partial struct WGPUColorTargetState
        {
            public WGPUColorTargetState()
            {
                this.writeMask = WGPUColorWriteMask.All;
            }
        }
    }
}