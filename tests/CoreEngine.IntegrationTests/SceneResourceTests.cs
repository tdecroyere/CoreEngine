using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CoreEngine;
using CoreEngine.Components;
using CoreEngine.Rendering.Components;
using CoreEngine.Resources;
using CoreEngine.Tools.Compiler;
using CoreEngine.Tools.Compiler.ResourceCompilers.Scenes;
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
                new ComponentDescription("CoreEngine.Components.TransformComponent", new Dictionary<string, object>()
                {
                    { "Position", new float[] { 10, 50.3f, 4.5f } }
                })
            }));

            sceneDescription.Entities.Add(new EntityDescription("TestEntity2", new List<ComponentDescription>()
            {
                new ComponentDescription("CoreEngine.Rendering.Components.CameraComponent", new Dictionary<string, object>()
                {
                    { "EyePosition", new float[] { 3.5f, 2.4f, 10.6f } }
                })
            }));

            var jsonContent = JsonSerializer.Serialize(sceneDescription);
            return Encoding.UTF8.GetBytes(jsonContent);
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
            var sceneCompiler = new JsonSceneResourceCompiler();
            var compilerContext = SetupCompilerContext();
            var inputData = SetupInputData();
            var outputData = await sceneCompiler.CompileAsync(inputData, compilerContext);

            var resourcesManager = new ResourcesManager();
            var sceneLoader = new SceneResourceLoader(resourcesManager);
            var scene = new Scene();

            // Act
            sceneLoader.LoadResourceData(scene, outputData.Span[0].Data.ToArray());

            // Assert
            var entities = scene.EntityManager.GetEntities().ToArray();
            Assert.Equal(2, entities.Length);
        }

        [Fact]
        public async Task CompileScene_LoadScene_HasComponentData()
        {
            // Arrange
            var sceneCompiler = new JsonSceneResourceCompiler();
            var compilerContext = SetupCompilerContext();
            var inputData = SetupInputData();
            var outputData = await sceneCompiler.CompileAsync(inputData, compilerContext);

            var resourcesManager = new ResourcesManager();
            var sceneLoader = new SceneResourceLoader(resourcesManager);
            var scene = new Scene();

            // Act
            sceneLoader.LoadResourceData(scene, outputData.Span[0].Data.ToArray());

            // Assert
            var entities = scene.EntityManager.GetEntities().ToArray();
            var component = scene.EntityManager.GetComponentData<TransformComponent>(entities[0]);
            
            Assert.Equal(10, component.Position.X);
            Assert.Equal(50.3f, component.Position.Y);
            Assert.Equal(4.5f, component.Position.Z);
        }
    }
}