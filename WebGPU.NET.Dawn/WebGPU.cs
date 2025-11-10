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

        public const uint WGPU_TRUE = 1;
        public const uint WGPU_FALSE = 0;

        public const uint WGPU_ARRAY_LAYER_COUNT_UNDEFINED = UInt32.MaxValue;
        public const uint WGPU_COPY_STRIDE_UNDEFINED = UInt32.MaxValue;
        public const float WGPU_DEPTH_CLEAR_VALUE_UNDEFINED = float.NaN;
        public const uint WGPU_DEPTH_SLICE_UNDEFINED = UInt32.MaxValue;
        public const uint WGPU_LIMIT_U32_UNDEFINED = UInt32.MaxValue;
        public const ulong WGPU_LIMIT_U64_UNDEFINED = UInt64.MaxValue;
        public const uint WGPU_MIP_LEVEL_COUNT_UNDEFINED = UInt32.MaxValue;
        public const uint WGPU_QUERY_SET_INDEX_UNDEFINED = UInt32.MaxValue;
        public const ulong WGPU_WHOLE_MAP_SIZE = UInt64.MaxValue;
        public const ulong WGPU_WHOLE_SIZE = UInt64.MaxValue;

        public const ulong WGPU_STRLEN = UInt64.MaxValue; // assuming SIZE_MAX is 64-bit
    }
}