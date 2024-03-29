using System;
using Xunit;

namespace CoreEngine.UnitTests
{
    public class TestSystem : EntitySystem
    {
        public TestSystem()
        {
        }

        public bool SetupCalled { get; private set; }
        public bool ProcessCalled { get; private set; }
        public int ProcessEntityCount { get; private set; }
        public int ProcessComponentCount { get; private set; }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Test");
            definition.Parameters.Add(new EntitySystemParameter<TestComponent>());

            return definition;
        }

        public override void Setup(EntityManager entityManager)
        {
            this.SetupCalled = true;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            this.ProcessCalled = true;
            
            var memoryChunks = GetMemoryChunks();

            if (memoryChunks.Length == 0)
            {
                return;
            }

            var memoryChunk = memoryChunks.Span[0];

            this.ProcessEntityCount = GetEntityArray(memoryChunk).Length;
            this.ProcessComponentCount = GetComponentArray<TestComponent>(memoryChunk).Length;
        }
    }
    
    public class EntitySystemManagerTests
    {
        [Fact]
        public void RegisterEntitySystem_ValidParameter_WasCorrectlyAdded()
        {
            // Arrange
            var entitySystemManager = new EntitySystemManager();

            // Act
            entitySystemManager.RegisterEntitySystem<TestSystem>();

            // Assert
            Assert.Equal(1, entitySystemManager.RegisteredSystems.Count);
        }

        [Fact]
        public void RegisterEntitySystem_RegisteredTwice_ThrowsInvalidOperationException()
        {
            // Arrange
            var entitySystemManager = new EntitySystemManager();
            entitySystemManager.RegisterEntitySystem<TestSystem>();

            // Act / Assert
            Assert.Throws<ArgumentException>(() => entitySystemManager.RegisterEntitySystem<TestSystem>());
        }

        [Fact]
        public void Process_SystemRegistered_SystemSetupCalled()
        {
            // Arrange
            var entityManager = new EntityManager();
            var container = new SystemManagerContainer();
            container.RegisterSystemManager(new PluginManager());

            var entitySystemManager = new EntitySystemManager();
            entitySystemManager.RegisterEntitySystem<TestSystem>();

            // Act
            entitySystemManager.Process(container, entityManager, 0.0f);

            // Assert
            Assert.True(((TestSystem)entitySystemManager.RegisteredSystems[0].EntitySystem!).SetupCalled);
        }

        [Fact]
        public void Process_SystemRegistered_SystemProcessCalled()
        {
            // Arrange
            var entityManager = new EntityManager();
            var container = new SystemManagerContainer();
            container.RegisterSystemManager(new PluginManager());

            var entitySystemManager = new EntitySystemManager();
            entitySystemManager.RegisterEntitySystem<TestSystem>();

            // Act
            entitySystemManager.Process(container, entityManager, 0.0f);

            // Assert
            Assert.True(((TestSystem)entitySystemManager.RegisteredSystems[0].EntitySystem!).ProcessCalled);
        }

        [Fact]
        public void Process_OneEntityCreated_ProcessHasCorrectEntityCount()
        {
            // Arrange
            var entityManager = new EntityManager();
            var componentLayout = entityManager.CreateComponentLayout<TestComponent>();
            entityManager.CreateEntity(componentLayout);

            var container = new SystemManagerContainer();
            container.RegisterSystemManager(new PluginManager());

            var entitySystemManager = new EntitySystemManager();
            entitySystemManager.RegisterEntitySystem<TestSystem>();

            // Act
            entitySystemManager.Process(container, entityManager, 0.0f);

            // Assert
            Assert.Equal(1, ((TestSystem)entitySystemManager.RegisteredSystems[0].EntitySystem!).ProcessEntityCount);
        }

        [Fact]
        public void Process_OneEntityCreatedWithNotCompatibleLayout_ProcessHasZeroEntityCount()
        {
            // Arrange
            var entityManager = new EntityManager();
            var componentLayout = entityManager.CreateComponentLayout<TestComponent2>();
            entityManager.CreateEntity(componentLayout);

            var container = new SystemManagerContainer();
            container.RegisterSystemManager(new PluginManager());

            var entitySystemManager = new EntitySystemManager();
            entitySystemManager.RegisterEntitySystem<TestSystem>();

            // Act
            entitySystemManager.Process(container, entityManager, 0.0f);

            // Assert
            Assert.Equal(0, ((TestSystem)entitySystemManager.RegisteredSystems[0].EntitySystem!).ProcessEntityCount);
        }

        [Fact]
        public void Process_OneEntityCreated_ProcessHasCorrectComponentCount()
        {
            // Arrange
            var entityManager = new EntityManager();
            var componentLayout = entityManager.CreateComponentLayout<TestComponent>();
            entityManager.CreateEntity(componentLayout);

            var container = new SystemManagerContainer();
            container.RegisterSystemManager(new PluginManager());
            
            var entitySystemManager = new EntitySystemManager();
            entitySystemManager.RegisterEntitySystem<TestSystem>();

            // Act
            entitySystemManager.Process(container, entityManager, 0.0f);

            // Assert
            Assert.Equal(1, ((TestSystem)entitySystemManager.RegisteredSystems[0].EntitySystem!).ProcessComponentCount);
        }
    }
}