#if WGPUNATIVE
namespace WebGPU.NET.Wgpu.Desktop.Demo
#else
namespace WebGPU.NET.Dawn.Desktop.Demo
#endif
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var demoList = new List<BaseApp>()
            {
                new EvergineTeamHelloTriangle(),
                new HelloTriangle(),
                new wgpu_native_examples_triangle(),
            };
            for (var index = 0; index < demoList.Count; index++)
            {
                Console.WriteLine($"{index} : {demoList[index].GetType().Name}");
            }
            Console.Write("Select Demo:");
            var key = Console.ReadLine();
            var demoInex = int.Parse(key);
            demoList[demoInex].Run();
        }
    }
}