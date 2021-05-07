using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CoreEngine;
using CoreEngine.Components;
using CoreEngine.Graphics;
using CoreEngine.HostServices;
using CoreEngine.UI.Native;
using CoreEngine.Rendering.Components;
using CoreEngine.Rendering;
using CoreEngine.Resources;
using CoreEngine.Tools.Compiler;
using CoreEngine.Tools.Compiler.ResourceCompilers.Scenes;
using Moq;
using Xunit;

namespace CoreEngine.IntegrationTests
{
    public class SceneResourceTests
    {
        private static CompilerContext SetupCompilerContext()
        {
            return new CompilerContext("win-x64", "Test", "Test", "Test", "Test");
        }

        private static byte[] SetupInputData()
        {
            var sceneDescription = new SceneDescription(new List<EntityDescription>());

            sceneDescription.Entities.Add(new EntityDescription("TestEntity", new List<ComponentDescription>()
            {
                new ComponentDescription("CoreEngine.Components.TransformComponent, CoreEngine", new Dictionary<string, object>()
                {
                    { "Position", new float[] { 10, 50.3f, 4.5f } }
                }),
                new ComponentDescription("CoreEngine.Rendering.Components.MeshComponent, CoreEngine", new Dictionary<string, object>()
                {
                    { "MeshResourceId", "resource:/teapot.mesh" }
                })
            }));

            sceneDescription.Entities.Add(new EntityDescription("TestCamera", new List<ComponentDescription>()
            {
                new ComponentDescription("CoreEngine.Rendering.Components.CameraComponent, CoreEngine", new Dictionary<string, object>()
                {
                    { "EyePosition", new float[] { 3.5f, 2.4f, 10.6f } }
                })
            }));

            sceneDescription.Entities.Add(new EntityDescription("SceneEntity", new List<ComponentDescription>()
            {
                new ComponentDescription("CoreEngine.Components.SceneComponent, CoreEngine", new Dictionary<string, object>()
                {
                    { "ActiveCamera", "TestCamera" }
                })
            }));

            var jsonContent = JsonSerializer.Serialize(sceneDescription);
            return Encoding.UTF8.GetBytes(jsonContent);
        }

        private static async Task<byte[]> SetupOutputData()
        {
            var sceneCompiler = new JsonSceneResourceCompiler();
            var compilerContext = SetupCompilerContext();
            var inputData = SetupInputData();
            var outputData = await sceneCompiler.CompileAsync(inputData, compilerContext);

            return outputData.Span[0].Data.ToArray();
        }

        private static SceneResourceLoader SetupSceneLoader(ResourcesManager? resourcesManager = null)
        {
            if (resourcesManager == null)
            {
                resourcesManager = new ResourcesManager();
            }

            var graphicsService = Utils.SetupGraphicsService();

            var nativeUIServiceMock = new Mock<INativeUIService>();

            var graphicsManager = new GraphicsManager(graphicsService, resourcesManager);
            var nativeUIManager = new NativeUIManager(nativeUIServiceMock.Object);
            var renderManager = new RenderManager(new Window(), nativeUIManager, graphicsManager, resourcesManager, new GraphicsSceneQueue());

            return new SceneResourceLoader(resourcesManager);
        }

        [Fact]
        public async Task CompileScene_SimpleScene_HasCompiled()
        {
            // Arrange
            var sceneCompiler = new JsonSceneResourceCompiler();
            var compilerContext = SetupCompilerContext();
            var inputData = SetupInputData();

            // Act
            var outputData = await sceneCompiler.CompileAsync(inputData, compilerContext);

            // Assert
            Assert.Equal(1, outputData.Length);
        }

        [Fact]
        public async Task CompileScene_LoadScene_HasCorrectEntityCount()
        {
            // Arrange
            var outputData = await SetupOutputData();
            var sceneLoader = SetupSceneLoader();
            var scene = new Scene();

            // Act
            sceneLoader.LoadResourceData(scene, outputData);

            // Assert
            var entities = scene.EntityManager.GetEntities().ToArray();
            Assert.Equal(3, entities.Length);
        }

        [Fact]
        public async Task CompileScene_LoadScene_EntityHasCorrectComponentCount()
        {
            // Arrange
            var outputData = await SetupOutputData();
            var sceneLoader = SetupSceneLoader();
            var scene = new Scene();

            // Act
            sceneLoader.LoadResourceData(scene, outputData);

            // Assert
            var entities = scene.EntityManager.GetEntities().ToArray();
            var componentLayout = scene.EntityManager.GetEntityComponentLayout(entities[0]);

            Assert.Equal(2, componentLayout.Components.Count);
        }

        [Fact]
        public async Task CompileScene_LoadScene_HasComponentData()
        {
            // Arrange
            var outputData = await SetupOutputData();
            var sceneLoader = SetupSceneLoader();
            var scene = new Scene();

            // Act
            sceneLoader.LoadResourceData(scene, outputData);

            // Assert
            var entities = scene.EntityManager.GetEntities().ToArray();
            var component = scene.EntityManager.GetComponentData<TransformComponent>(entities[0]);
            
            Assert.Equal(10, component.Position.X);
            Assert.Equal(50.3f, component.Position.Y);
            Assert.Equal(4.5f, component.Position.Z);
        }

        [Fact]
        public async Task CompileScene_LoadScene_HasCorrectEntityReference()
        {
            // Arrange
            var outputData = await SetupOutputData();
            var sceneLoader = SetupSceneLoader();
            var scene = new Scene();

            // Act
            sceneLoader.LoadResourceData(scene, outputData);

            // Assert
            var entities = scene.EntityManager.GetEntities().ToArray();
            var component = scene.EntityManager.GetComponentData<SceneComponent>(entities[2]);
            
            Assert.NotNull(component.ActiveCamera);
            Assert.Equal(entities[1], component.ActiveCamera!.Value);
        }

        [Fact]
        public async Task CompileScene_LoadScene_LoadDependentResource()
        {
            // Arrange
            var resourcesManager = new TestResourcesManager();
            var outputData = await SetupOutputData();
            var sceneLoader = SetupSceneLoader(resourcesManager);
            var scene = new Scene();

            // Act
            sceneLoader.LoadResourceData(scene, outputData);

            // Assert
            Assert.True(resourcesManager.LoadedResources.Contains("/teapot.mesh"));
        }
    }
}