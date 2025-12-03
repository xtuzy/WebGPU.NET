using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Text;

#if WGPUNATIVE
using static WebGPU.NET.Wgpu.WebGPU;
#else
using static WebGPU.NET.Dawn.WebGPU;
#endif

namespace WebGPU.NET.Dawn.Desktop.Demo
{
    public abstract class BaseApp
    {
        WebGPUControl Window;

        public void Run(WebGPUControl window)
        {
            this.Window = window;

             WindowOnLoad();

            this.Window.Paint += (sender, e) =>
            {
                WindowOnRender(16);
            };

            this.Window.SizeChanged += (sender, e) =>
            {
                FramebufferResize(new Vector2D<int>(this.Window.ClientSize.Width, this.Window.ClientSize.Height));
            };

            this.Window.VisibleChanged += (sender, e) =>
            {
                if(this.Window.Visible == false)
                    WindowClosing();
            };
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
                hwnd = (void*)this.Window.Hwnd,
                hinstance = (void*)this.Window.HInstance
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
            return (uint)this.Window.ClientSize.Width;
        }

        public uint GetHeight()
        {
            return (uint)this.Window.ClientSize.Height;
        }
    }
}
