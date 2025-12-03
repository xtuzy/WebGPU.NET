using System.Runtime.InteropServices;
using WebGPU.NET.Dawn.Desktop.Demo;

namespace WebGPU.NET.Dawn.WinFormsApp
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            var form = new Form1();

            var layout = new FlowLayoutPanel() { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowOnly };
            form.Controls.Add(layout);

            var button1 = new Button() { Text = nameof(HelloTriangle), AutoSize = true };
            var button2 = new Button() { Text = nameof(EvergineTeamHelloTriangle), AutoSize = true };
            layout.Controls.Add(button1);
            layout.Controls.Add(button2);

            var webgpuControl = new WebGPUControl() { Dock = DockStyle.Fill };
            form.Controls.Add(webgpuControl);

            button1.Click += (s, e) =>
            {
                var example = new HelloTriangle();
                example.Run(webgpuControl);
            };
            button2.Click += (s, e) =>
            {
                var example = new EvergineTeamHelloTriangle();
                example.Run(webgpuControl);
            };

            Application.Run(form);
        }
    }
}