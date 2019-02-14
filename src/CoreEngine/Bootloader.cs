using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace CoreEngine
{
    public class Bootloader
    {
        private static CoreEngineApp? coreEngineApp = null;

        public static void StartEngine(HostPlatform hostPlatform)
        {
            var commandLineArgs = Environment.GetCommandLineArgs();

            foreach (var arg in commandLineArgs)
            {
                Console.WriteLine($"Command Line: {arg}");
            }

            if (commandLineArgs.Length > 1)
            {
                coreEngineApp = LoadCoreEngineApp(commandLineArgs[1]).Result;
            }

            var result = hostPlatform.AddTestHostMethod(3, 8);
            Span<byte> testBuffer = hostPlatform.GetTestBuffer();

            Console.WriteLine($"Starting CoreEngine (Test Parameter: {hostPlatform.TestParameter} - {result})...");

            for (int i = 0; i < testBuffer.Length; i++)
            {
                Console.WriteLine($"TestBuffer {testBuffer[i]}");
            }

            if (coreEngineApp != null)
            {
                coreEngineApp.Init();
            }
        }

        // TODO: Use the isolated app domain new feature to be able to do hot build of the app dll
        private static async Task<CoreEngineApp?> LoadCoreEngineApp(string appName)
        {
            var assemblyContent = await File.ReadAllBytesAsync($"{appName}.dll");
            var assembly = Assembly.Load(assemblyContent);

            foreach (var type in assembly.GetTypes())
            {
                if (type.IsSubclassOf(typeof(CoreEngineApp)))
                {
                    return (CoreEngineApp)Activator.CreateInstance(type);
                }
            }

            return null;
        }
    }
}
