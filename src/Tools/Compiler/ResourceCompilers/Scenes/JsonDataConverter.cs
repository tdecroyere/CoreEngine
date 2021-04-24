using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreEngine.Tools.Compiler.ResourceCompilers.Scenes
{
    public class JsonDataConverter : JsonConverter<object>
    {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = reader.TokenType switch
            {
                JsonTokenType.StartArray => ReadArray(ref reader),
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Number => (float)reader.GetDouble(),
                JsonTokenType.String when reader.TryGetDateTime(out DateTime datetime) => datetime,
                JsonTokenType.String => reader.GetString(),
                _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
            };

            return result ?? 0;
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options) => throw new InvalidOperationException();

        private static object ReadArray(ref Utf8JsonReader reader)
        {
            var list = new List<float>();
            var startDepth = reader.CurrentDepth;

            while(reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray && reader.CurrentDepth == startDepth)
                {
                    break;
                }

                list.Add((float)reader.GetDouble());
            }

            return list.ToArray();
        }
    }
}