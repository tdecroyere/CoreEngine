using System;
using BenchmarkDotNet.Attributes;

namespace CoreEngine.Tests.EcsTest
{
    public class Benchmark
    {
        private EcsTestApp testApp;

        public Benchmark()
        {
            this.testApp = new EcsTestApp();
            this.testApp.Init();
        }

        [Benchmark]
        public void TestUpdate()
        {
            this.testApp.Update(5);
        }
    }
}