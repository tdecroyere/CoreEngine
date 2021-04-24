using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;

namespace CoreEngine.Tools.Compiler.ResourceCompilers.Scenes
{
    public class JsonSceneResourceCompiler : SceneResourceCompiler
    {
        public JsonSceneResourceCompiler()
        {

        }
        
        public override string Name
        {
            get
            {
                return "Json Scene Resource Compiler";
            }
        }

        protected override Task<SceneDescription> ParseSceneFileAsync(ReadOnlyMemory<byte> sourceData)
        {
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                Converters =
                {
                    new JsonDataConverter()
                }
            };

            var sceneDescription = JsonSerializer.Deserialize<SceneDescription>(sourceData.Span, options);

            if (sceneDescription != null)
            {
                foreach (var entity in sceneDescription.Entities)
                {
                    var entityLayoutDescription = new EntityLayoutDescription();

                    foreach (var component in entity.Components)
                    {
                        entityLayoutDescription.Types.Add(component.ComponentType);
                    }

                    entity.EntityLayoutIndex = sceneDescription.AddEntityLayoutDescription(entityLayoutDescription);
                }
            }

            return sceneDescription != null ? Task.FromResult(sceneDescription) : Task.FromResult(new SceneDescription(new List<EntityDescription>()));
        }
    }
}