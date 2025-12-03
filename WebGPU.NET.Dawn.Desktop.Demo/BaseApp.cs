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

    public unsafe WGPUSurface CreateWebGPUSurface(WGPUInstance instance)
    {
        var fromWindowsHWND = new WGPUSurfaceSourceWindowsHWND()
        {
            chain = new WGPUChainedStruct()
            {
                sType = WGPUSType.SurfaceSourceWindowsHWND,
                //Next = null
            },
            hwnd = (void*)this.Window.Native.Win32.Value.Hwnd,
            hinstance = (void*)this.Window.Native.Win32.Value.HInstance
        };

        WGPUSurfaceDescriptor descriptor = new()
        {
            nextInChain = &fromWindowsHWND.chain,
            label = new WGPUStringView() { data = null, length = 0 }
        };

        WGPUSurface result = wgpuInstanceCreateSurface(instance, &descriptor);

        return result;
    }

    public uint GetWidth()
    {
        return (uint)this.Window.FramebufferSize.X;
    }

    public uint GetHeight()
    {
        return (uint)this.Window.FramebufferSize.Y;
    }
}