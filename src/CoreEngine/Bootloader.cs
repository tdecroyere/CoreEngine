using System;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    public class Bootloader
    {
        public static void StartEngine(HostPlatform hostPlatform)
        {
            var result = hostPlatform.AddTestHostMethod(3, 8);
            Span<byte> testBuffer = hostPlatform.GetTestBuffer();

            Console.WriteLine($"Starting CoreEngine (Test Parameter: {hostPlatform.TestParameter} - {result})...");

            for (int i = 0; i < testBuffer.Length; i++)
            {
                Console.WriteLine($"TestBuffer {testBuffer[i]}");
            }
        }
    }
}
