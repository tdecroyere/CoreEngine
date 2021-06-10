using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace CoreEngine.PerformanceTests
{
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 1, targetCount: 10, invocationCount: 10000)]
    public class EntityManagerTests
    {
        private EntityManager entityManager;
        private ComponentLayout componentLayout;

        public EntityManagerTests()
        {
            this.entityManager = new EntityManager();
            this.componentLayout = entityManager.CreateComponentLayout<TestComponent, TestComponent2>();
        }

        [Benchmark]
        public void CreateEntity_BasicLayout()
        {
            this.entityManager.CreateEntity(this.componentLayout);
        }
    }
}