using BenchmarkDotNet.Running;

namespace CoreEngine.PerformanceTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<EntityManagerTests>();
        }
    }
}