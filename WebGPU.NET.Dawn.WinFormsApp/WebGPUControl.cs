using System.Diagnostics;
using System.Runtime.InteropServices;
using WebGPU.NET.Dawn.WinFormsApp;

namespace WebGPU.NET.Dawn.Desktop.Demo
{
    public partial class WebGPUControl : UserControl
    {
        public WebGPUControl()
        {
            bool IsAppIdle()
            {
                NativeMessage msg;
                return !PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
            }

            Application.Idle += (sender, e) =>
            {
                while (IsAppIdle())
                {
                    this.Invalidate();
                }
            };
        }

        [System.Security.SuppressUnmanagedCodeSecurity]
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool PeekMessage(
            out NativeMessage msg,
            IntPtr hWnd,
            uint messageFilterMin,
            uint messageFilterMax,
            uint flags);

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeMessage
        {
            public IntPtr hWnd;
            public Int32 msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public System.Drawing.Point p;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        public nint Hwnd => Handle;

        public nint HInstance => Process.GetCurrentProcess().Handle;
    }
}