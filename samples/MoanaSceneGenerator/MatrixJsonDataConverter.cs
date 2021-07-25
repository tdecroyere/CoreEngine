using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MoanaSceneGenerator
{
    public class MatrixJsonDataConverter : JsonConverter<Matrix4x4>
    {

        public override Matrix4x4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var floatArray = (float[])ReadArray(ref reader);
            var result = new Matrix4x4();

            result.M11 = floatArray[0];
            result.M12 = floatArray[1];
            result.M13 = floatArray[2];
            result.M14 = floatArray[3];
            
            result.M21 = floatArray[4];
            result.M22 = floatArray[5];
            result.M23 = floatArray[6];
            result.M24 = floatArray[7];

            result.M31 = floatArray[8];
            result.M32 = floatArray[9];
            result.M33 = floatArray[10];
            result.M34 = floatArray[11];

            result.M41 = floatArray[12];
            result.M42 = floatArray[13];
            result.M43 = floatArray[14];
            result.M44 = floatArray[15];
            
            return result;
        }

        public override void Write(Utf8JsonWriter writer, Matrix4x4 value, JsonSerializerOptions options) => throw new InvalidOperationException();

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