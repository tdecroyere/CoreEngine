using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CoreEngine.Components;
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
            var jsonContent = JsonSerializer.Serialize(new
            { 
                Entities = new [] 
                {
                    new {    
                        Entity = "TestEntity",
                        Components = new []
                        {
                            new { Component = "CoreEngine.Components.TransformComponent", Data = new { Position = new[] { 10, 50.3, 4.5 } } }
                        }
                    }
                }
            });

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
    }
}