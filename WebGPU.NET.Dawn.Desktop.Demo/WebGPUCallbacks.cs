using System.Runtime.InteropServices;

#if WGPUNATIVE
using static WebGPU.NET.Wgpu.WebGPU;
#else
using static WebGPU.NET.Dawn.WebGPU;
#endif

#if WGPUNATIVE
namespace WebGPU.NET.Wgpu.Desktop.Demo;
#else
namespace WebGPU.NET.Dawn.Desktop.Demo;
#endif

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate void WGPURequestDeviceCallback(WGPURequestDeviceStatus status, WGPUDevice device, WGPUStringView str, void* userData1, void* userData2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate void WGPUDeviceLostCallback(WGPUDevice* device, WGPUDeviceLostReason lost, WGPUStringView str, void* userData1, void* userData2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate void WGPURequestAdapterCallback(WGPURequestAdapterStatus status, WGPUAdapter adapter, WGPUStringView str, void* userData1, void* userData2);

#if WGPUNATIVE
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate void WGPULoggingCallback(WGPULogLevel type, WGPUStringView message, void* userData1);
#else

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate void WGPULoggingCallback(WGPULoggingType type, WGPUStringView message, void* userData1, void* userData2);

#endif

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate void WGPUCreateRenderPipelineCallback(WGPUCreatePipelineAsyncStatus status, WGPURenderPipeline pipeline, WGPUStringView str, void* userData1, void* userData2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate void WGPUBufferMapCallback(WGPUMapAsyncStatus status, WGPUStringView str, void* userData1, void* userData2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate void WGPUQueueWorkDoneCallback(WGPUQueueWorkDoneStatus status, void* userData1, void* userData2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate void WGPUUncapturedErrorCallback(WGPUDevice* device, WGPUErrorType type, WGPUStringView str, void* userData1, void* userData2);