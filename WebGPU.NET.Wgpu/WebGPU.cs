namespace WebGPU.NET.Wgpu
{
    public static partial class WebGPU
    {
#if ANDROID
        private const string webgpu_native_dll_name = "wgpu_native";
#elif IOS || MACCATALYST
        private const string webgpu_native_dll_name = "__Internal";
#else
        private const string webgpu_native_dll_name = "wgpu_native";
#endif
    }
}