using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CoreEngine.Tools.Compiler.ResourceCompilers.Scenes
{
    public class ComponentDescription
    {
        public ComponentDescription(string componentType, IDictionary<string, object> data)
        {
            this.ComponentType = componentType;
            this.Data = data;
        }
        
        [JsonPropertyName("Component")]
        public string ComponentType { get; }

        public IDictionary<string, object> Data { get; }
    }
}