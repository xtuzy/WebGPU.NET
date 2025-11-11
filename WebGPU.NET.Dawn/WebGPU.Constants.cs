#if WGPUNATIVE
namespace WebGPU.NET.Wgpu
#else
namespace WebGPU.NET.Dawn
#endif
{
    public static partial class WebGPU
    {
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
        public static nuint WGPU_WHOLE_MAP_SIZE = nuint.MaxValue;
        public const ulong WGPU_WHOLE_SIZE = UInt64.MaxValue;

        public static nuint WGPU_STRLEN = nuint.MaxValue;
    }
}