using System;
using Xunit;

namespace CoreEngine.UnitTests
{
    public class EntityManagerTests
    {
        [Fact]
        public void CreateComponentLayout_ValidComponentType_IsValid()
        {
            // Arrange
            var entityManager = new EntityManager();

            // Act
            var componentLayout = entityManager.CreateComponentLayout<TestComponent>();

            // Assert
            Assert.Equal(1, componentLayout.Components.Count);
            Assert.False(componentLayout.IsReadOnly);
        }

        [Fact]
        public void CreateComponentLayout_SameComponentTypes_ThrowsArgumentException()
        {
            // Arrange
            var entityManager = new EntityManager();

            // Act / Assert
            Assert.Throws<ArgumentException>(() => entityManager.CreateComponentLayout<TestComponent, TestComponent>());
        }

        [Fact]
        public void CreateComponentLayout_MultipleComponentTypes_IsValid()
        {
            // Arrange
            var entityManager = new EntityManager();

            // Act
            var componentLayout = entityManager.CreateComponentLayout<TestComponent, TestComponent2>();

            // Assert
            Assert.Equal(2, componentLayout.Components.Count);
        }

        [Fact]
        public void CreateComponentLayout_DifferentComponentTypes_HaveDiffentLayoutId()
        {
            // Arrange
            var entityManager = new EntityManager();

            // Act
            var componentLayout = entityManager.CreateComponentLayout<TestComponent>();
            var componentLayout2 = entityManager.CreateComponentLayout<TestComponent2>();

            // Assert
            Assert.NotEqual(componentLayout.LayoutHash, componentLayout2.LayoutHash);
        }

        [Fact]
        public void CreateComponentLayout_DifferentComponentTypesOrder_HaveSameLayoutHash()
        {
            // Arrange
            var entityManager = new EntityManager();

            // Act
            var componentLayout = entityManager.CreateComponentLayout<TestComponent, TestComponent2>();
            var componentLayout2 = entityManager.CreateComponentLayout<TestComponent2, TestComponent>();
            
            // Assert
            Assert.Equal(componentLayout.LayoutHash, componentLayout2.LayoutHash);
        }

        [Fact]
        public void CreateEntity_BasicLayout_IsValid()
        {
            // Arrange
            var entityManager = new EntityManager();
            var componentLayout = entityManager.CreateComponentLayout<TestComponent, TestComponent2>();

            // Act
            var entity = entityManager.CreateEntity(componentLayout);

            // Assert
            Assert.Equal((uint)1, entity.EntityId);
        }

        [Fact]
        public void GetEntities_OneEntity_HasCorrectEntity()
        {
            // Arrange
            var entityManager = new EntityManager();
            var componentLayout = entityManager.CreateComponentLayout<TestComponent, TestComponent2>();
            var entity = entityManager.CreateEntity(componentLayout);

            // Act
            var entities = entityManager.GetEntities();

            // Assert
            Assert.Equal(1, entities.Length);
            Assert.Contains(entity, entities.ToArray());
        }

        [Fact]
        public void GetEntities_MultipleEntities_HasCorrectEntities()
        {
            // Arrange
            var entityManager = new EntityManager();
            var componentLayout = entityManager.CreateComponentLayout<TestComponent, TestComponent2>();
            var entity = entityManager.CreateEntity(componentLayout);
            var entity2 = entityManager.CreateEntity(componentLayout);

            // Act
            var entities = entityManager.GetEntities();

            // Assert
            Assert.Equal(2, entities.Length);
            Assert.Contains(entity, entities.ToArray());
            Assert.Contains(entity2, entities.ToArray());
        }

        [Fact]
        public void GetEntititiesByComponentType_MultipleEntities_HasCorrectEntities()
        {
            // Arrange
            var entityManager = new EntityManager();
            var componentLayout = entityManager.CreateComponentLayout<TestComponent, TestComponent2>();
            var componentLayout2 = entityManager.CreateComponentLayout<TestComponent>();
            var entity = entityManager.CreateEntity(componentLayout);
            var entity2 = entityManager.CreateEntity(componentLayout);
            entityManager.CreateEntity(componentLayout2);

            // Act
            var entities = entityManager.GetEntitiesByComponentType<TestComponent2>();

            // Assert
            Assert.Equal(2, entities.Length);
            Assert.Contains(entity, entities.ToArray());
            Assert.Contains(entity2, entities.ToArray());
        }

        [Fact]
        public void GetComponentData_OneEntity_HasCorrectDefaultValue()
        {
            // Arrange
            var entityManager = new EntityManager();
            var componentLayout = entityManager.CreateComponentLayout<TestComponent, TestComponent2>();
            var entity = entityManager.CreateEntity(componentLayout);

            // Act
            var componentData = entityManager.GetComponentData<TestComponent>(entity);

            // Assert
            Assert.Equal(5, componentData.TestField);
        }

        [Fact]
        public void SetComponentData_OneEntity_HasCorrectValue()
        {
            // Arrange
            var entityManager = new EntityManager();
            var componentLayout = entityManager.CreateComponentLayout<TestComponent, TestComponent2>();
            var entity = entityManager.CreateEntity(componentLayout);

            // Act
            entityManager.SetComponentData(entity, new TestComponent() { TestField = 28 });

            // Assert
            var componentData = entityManager.GetComponentData<TestComponent>(entity);
            Assert.Equal(28, componentData.TestField);
        }

        [Fact]
        public void GetComponentData_WrongComponentType_ThrowsArgumentException()
        {
            // Arrange
            var entityManager = new EntityManager();
            var componentLayout = entityManager.CreateComponentLayout<TestComponent2>();
            var entity = entityManager.CreateEntity(componentLayout);

            // Act / Assert
            Assert.Throws<ArgumentException>(() => entityManager.GetComponentData<TestComponent>(entity));
        }

        [Fact]
        public void SetComponentData_WrongComponentType_ThrowsArgumentException()
        {
            // Arrange
            var entityManager = new EntityManager();
            var componentLayout = entityManager.CreateComponentLayout<TestComponent2>();
            var entity = entityManager.CreateEntity(componentLayout);

            // Act / Assert
            Assert.Throws<ArgumentException>(() => entityManager.SetComponentData(entity, new TestComponent() { TestField = 28 }));
        }

        [Fact]
        public void SetComponentData_WrongComponentDataType_ThrowsArgumentException()
        {
            // Arrange
            var entityManager = new EntityManager();
            var componentLayout = entityManager.CreateComponentLayout<TestComponent2>();
            var entity = entityManager.CreateEntity(componentLayout);

            // Act / Assert
            Assert.Throws<ArgumentException>(() => entityManager.SetComponentData(entity, new TestComponent() { TestField = 28 }));
        }

        [Fact]
        public void SetComponentData_MultipleEntities_HaveCorrectValue()
        {
            // Arrange
            var entityManager = new EntityManager();
            var componentLayout = entityManager.CreateComponentLayout<TestComponent, TestComponent2>();
            var entity = entityManager.CreateEntity(componentLayout);
            var entity2 = entityManager.CreateEntity(componentLayout);

            // Act
            entityManager.SetComponentData(entity, new TestComponent() { TestField = 28 });
            entityManager.SetComponentData(entity2, new TestComponent() { TestField = 56 });

            // Assert
            var componentData = entityManager.GetComponentData<TestComponent>(entity);
            var componentData2 = entityManager.GetComponentData<TestComponent>(entity2);
            Assert.Equal(28, componentData.TestField);
            Assert.Equal(56, componentData2.TestField);
        }

        [Fact]
        public void GetEntitySystemData_MultipleEntitiesOneComponentType_HaveCorrectValue()
        {
            // Arrange
            var entityManager = new EntityManager();
            var componentLayout = entityManager.CreateComponentLayout<TestComponent, TestComponent2>();
            var componentLayout2 = entityManager.CreateComponentLayout<TestComponent2>();
            entityManager.CreateEntity(componentLayout);
            entityManager.CreateEntity(componentLayout2);
            entityManager.CreateEntity(componentLayout);

            // Act
            var entitySystemData = entityManager.GetEntitySystemData(new ComponentHash[] { new TestComponent().GetComponentHash() });

            // Assert
            Assert.Equal(2, entitySystemData.EntityArray.Length);
        }
    }
}