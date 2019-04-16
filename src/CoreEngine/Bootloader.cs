﻿using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CoreEngine.Graphics;
using CoreEngine.Inputs;
using CoreEngine.Resources;

namespace CoreEngine
{
    public class Bootloader
    {
        private static CoreEngineApp? coreEngineApp = null;

        public static void StartEngine(string appName, ref HostPlatform hostPlatform)
        {
            Console.WriteLine($"Starting CoreEngine...");
            Console.WriteLine($"Test Parameter: {hostPlatform.TestParameter}");
            
            if (hostPlatform.AddTestHostMethod != null)
            {
                var result = hostPlatform.AddTestHostMethod(3, 8);
                Console.WriteLine($"Test Parameter: {hostPlatform.TestParameter} - {result}");
            }

            if (hostPlatform.GetTestBuffer != null)
            {
                Span<byte> testBuffer = hostPlatform.GetTestBuffer();

                for (int i = 0; i < testBuffer.Length; i++)
                {
                    Console.WriteLine($"TestBuffer {testBuffer[i]}");
                }
            }

            if (appName != null)
            {
                Console.WriteLine($"Loading CoreEngineApp '{appName}'...");
                coreEngineApp = LoadCoreEngineApp(appName).Result;

                if (coreEngineApp != null)
                {
                    Console.WriteLine("CoreEngineApp loading successfull.");

                    var resourcesManager = new ResourcesManager();
                    
                    // TODO: Get the config from the host using hardcoded values for the moment
                    resourcesManager.AddResourceStorage(new FileSystemResourceStorage("."));

                    // Register managers
                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<ResourcesManager>(resourcesManager);
                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<GraphicsManager>(new GraphicsManager(hostPlatform.GraphicsService));
                    coreEngineApp.SystemManagerContainer.RegisterSystemManager<InputsManager>(new InputsManager(hostPlatform.InputsService));

                    Console.WriteLine("Initializing app...");
                    coreEngineApp.Init();
                    Console.WriteLine("Initializing app done.");
                }
            }
        }

        public static void UpdateEngine(float deltaTime)
        {
            if (coreEngineApp != null)
            {
                coreEngineApp.SystemManagerContainer.UpdateSystemManagers();
                coreEngineApp.Update(deltaTime);
            }
        }

        // TODO: Use the isolated app domain new feature to be able to do hot build of the app dll
        private static async Task<CoreEngineApp?> LoadCoreEngineApp(string appName)
        {
            // TODO: Check if dll exists
            var currentAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var assemblyContent = await File.ReadAllBytesAsync(Path.Combine(currentAssemblyPath, $"{appName}.dll"));
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
