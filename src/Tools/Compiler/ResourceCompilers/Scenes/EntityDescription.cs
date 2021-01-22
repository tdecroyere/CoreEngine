using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CoreEngine.Tools.Compiler.ResourceCompilers.Scenes
{
    public class EntityDescription
    {
        public EntityDescription(string name, IList<ComponentDescription> components)
        {
            this.Name = name;
            this.Components = components;
        }
        
        [JsonPropertyName("Entity")]
        public string Name { get; }
        public int EntityLayoutIndex { get; set; }
        public IList<ComponentDescription> Components { get; }
    }
}