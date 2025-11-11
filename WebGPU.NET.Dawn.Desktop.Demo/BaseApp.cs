using Silk.NET.Maths;
using Silk.NET.Windowing;
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

public abstract class BaseApp
{
    public IWindow Window { private set; get; }

    private int Width = 1024;
    private int Height = 800;

    public IWindow Run(object obj = null)
    {
        //Create window
        var options = WindowOptions.Default;
        options.API = GraphicsAPI.None;
        options.Size = new Vector2D<int>(Width, Height);
        options.FramesPerSecond = 60;
        options.UpdatesPerSecond = 60;
        options.Position = new Vector2D<int>(100, 100);
        options.Title = "WebGPU Triangle";
        options.IsVisible = true;
        options.ShouldSwapAutomatically = false;
        options.IsContextControlDisabled = true;

        Window = Silk.NET.Windowing.Window.Create(options);

        Window.Load += WindowOnLoad;
        Window.Closing += WindowClosing;
        //Window.Update += WindowOnRender;
        Window.Render += WindowOnRender;
        Window.FramebufferResize += FramebufferResize;

        Window.Run();

        return Window;
    }

    protected abstract void FramebufferResize(Vector2D<int> size);

    protected abstract void WindowOnLoad();

    protected abstract void WindowOnRender(double delta);

    protected abstract void WindowClosing();

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

    public static unsafe WGPUSurface CreateWebGPUSurface(IWindow view, WGPUInstance instance)
    {
        var fromWindowsHWND = new WGPUSurfaceSourceWindowsHWND()
        {
            chain = new WGPUChainedStruct()
            {
                sType = WGPUSType.SurfaceSourceWindowsHWND,
                //Next = null
            },
            hwnd = (void*)view.Native.Win32.Value.Hwnd,
            hinstance = (void*)view.Native.Win32.Value.HInstance
        };

        WGPUSurfaceDescriptor descriptor = new()
        {
            nextInChain = &fromWindowsHWND.chain,
            label = new WGPUStringView() { data = null, length = 0 }
        };

        WGPUSurface result = wgpuInstanceCreateSurface(instance, &descriptor);

        return result;
    }

    public uint GetWidth(IWindow view)
    {
        return (uint)view.FramebufferSize.X;
    }

    public uint GetHeight(IWindow view)
    {
        return (uint)view.FramebufferSize.Y;
    }
}