using System;
using System.Globalization;
using System.Numerics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace MoanaSceneGenerator
{
    public class MoanaSceneDescription
    {
        public Matrix4x4 TransformMatrix { get; set; }
        public string GeomObjFile { get; set; }
        public string Name { get; set; }
    }

    class Program
    {
        private const string outputTemplate = @"{
  ""Entities"": [
    {
      ""Entity"": ""Scene"",
      ""Components"": [
        {
          ""Component"": ""CoreEngine.Components.SceneComponent, CoreEngine"",
          ""Data"": {
            ""ActiveCamera"": ""MainCamera""
          }
        }
      ]
    },
    {
      ""Entity"": ""MainCamera"",
      ""Components"": [
        {
          ""Component"": ""CoreEngine.Rendering.Components.CameraComponent, CoreEngine""
        },
        {
          ""Component"": ""CoreEngine.Components.TransformComponent, CoreEngine"",
          ""Data"": {
            ""Position"": [0, 0, 12],
            ""RotationY"": 0
          }
        },
        {
          ""Component"": ""CoreEngine.Samples.SceneViewer.PlayerComponent, SceneViewer"",
          ""Data"": {
            ""MovementAcceleration"": 30000
          }
        }
      ]
    },
    {
      ""Entity"": ""DebugCamera"",
      ""Components"": [
        {
          ""Component"": ""CoreEngine.Rendering.Components.CameraComponent, CoreEngine""
        },
        {
          ""Component"": ""CoreEngine.Components.TransformComponent, CoreEngine"",
          ""Data"": {
            ""Position"": [-10, 10, 0],
            ""RotationY"": 90,
            ""RotationX"": 45
          }
        },
        {
          ""Component"": ""CoreEngine.Samples.SceneViewer.PlayerComponent, SceneViewer"",
          ""Data"": {
            ""MovementAcceleration"": 300,
            ""IsActive"": false
          }
        }
      ]
    },
#ADDITIONAL_ENTITIES#
  ]
}";

        static void Indent(StringBuilder stringBuilder, uint indentLevel)
        {
            for (var i = 0; i < indentLevel; i++)
            {
                stringBuilder.Append("  ");
            }
        }

        public static float RadToDegrees(float angle)
        {
            return angle * 180.0f / MathF.PI;
        }

        static void ToEulerianAngle(Quaternion data, out float yaw, out float pitch, out float roll)
        {
            double q2sqr = data.Z * data.Z;
            double t0 = -2.0 * (q2sqr + data.W * data.W) + 1.0;
            double t1 = +2.0 * (data.Y * data.Z + data.X * data.W);
            double t2 = -2.0 * (data.Y * data.W - data.X * data.Z);
            double t3 = +2.0 * (data.Z * data.W + data.X * data.Y);
            double t4 = -2.0 * (data.Y * data.Y + q2sqr) + 1.0;

            t2 = t2 > 1.0 ? 1.0 : t2;
            t2 = t2 < -1.0 ? -1.0 : t2;

            pitch = (float)Math.Asin(t2);
            roll = (float) Math.Atan2(t3, t4);
            yaw = (float)Math.Atan2(t1, t0);
        }

        static string FormatNumber(float number)
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        static string FormatTransformComponent(Matrix4x4 matrix)
        {
            var result = Matrix4x4.Decompose(matrix, out var scale, out var rotation, out var translation);

            ToEulerianAngle(rotation, out var yaw, out var pitch, out var roll);
            pitch = 0;//RadToDegrees(pitch);
            yaw = 180.0f - RadToDegrees(yaw);
            roll = 0;//RadToDegrees(roll);

            return $"\"Position\": [{FormatNumber(translation.X)}, {FormatNumber(translation.Y)}, {FormatNumber(-translation.Z)}], " + 
                   $"\"Scale\": [{FormatNumber(scale.X)}, {FormatNumber(scale.Y)}, {FormatNumber(scale.Z)}], " + 
                   $"\"RotationX\": {FormatNumber(pitch)}, " + 
                   $"\"RotationY\": {FormatNumber(yaw)}, " +
                   $"\"RotationZ\": {FormatNumber(roll)}";
/*
            return "\"WorldMatrix\": [ " +
                    $"{matrix.M11y}, {matrix.M12.ToString(CultureInfo.InvariantCulture)}, {matrix.M13.ToString(CultureInfo.InvariantCulture)}, {matrix.M14.ToString(CultureInfo.InvariantCulture)}, " + 
                   $"{matrix.M21.ToString(CultureInfo.InvariantCulture)}, {(matrix.M22).ToString(CultureInfo.InvariantCulture)}, {matrix.M23.ToString(CultureInfo.InvariantCulture)}, {matrix.M24.ToString(CultureInfo.InvariantCulture)}, " + 
                   $"{matrix.M31.ToString(CultureInfo.InvariantCulture)}, {matrix.M32.ToString(CultureInfo.InvariantCulture)}, {matrix.M33.ToString(CultureInfo.InvariantCulture)}, {matrix.M34.ToString(CultureInfo.InvariantCulture)}, " + 
                   $"{matrix.M41.ToString(CultureInfo.InvariantCulture)}, {matrix.M42.ToString(CultureInfo.InvariantCulture)}, {(-matrix.M43).ToString(CultureInfo.InvariantCulture)}, {matrix.M44.ToString(CultureInfo.InvariantCulture)}" +
                   "]"; */
        }

        static void GenerateJsonScene(string outputPath, ReadOnlySpan<MoanaSceneDescription> moanaSceneDescriptions)
        {
            var stringBuilder = new StringBuilder();

            for (var i = 0; i < moanaSceneDescriptions.Length; i++)
            {
                var sceneDescription = moanaSceneDescriptions[i];

                if (sceneDescription == null || string.IsNullOrEmpty(sceneDescription.GeomObjFile))
                {
                    continue;
                }

                var meshName = Path.GetFileNameWithoutExtension(sceneDescription.GeomObjFile);

                Indent(stringBuilder, 2);
                stringBuilder.AppendLine("{");

                Indent(stringBuilder, 3);
                stringBuilder.AppendLine($"\"Entity\": \"{sceneDescription.Name}\",");

                Indent(stringBuilder, 3);
                stringBuilder.AppendLine("\"Components\": [");

                Indent(stringBuilder, 4);
                stringBuilder.AppendLine("{");

                Indent(stringBuilder, 5);
                stringBuilder.AppendLine("\"Component\": \"CoreEngine.Rendering.Components.MeshComponent, CoreEngine\",");

                Indent(stringBuilder, 5);
                stringBuilder.AppendLine($"\"Data\": {{ \"MeshResourceId\": \"resource:/Data/MoanaIsland/{meshName}/{meshName}.mesh\" }}");

                Indent(stringBuilder, 4);
                stringBuilder.AppendLine("},");

                Indent(stringBuilder, 4);
                stringBuilder.AppendLine("{");

                Indent(stringBuilder, 5);
                stringBuilder.AppendLine("\"Component\": \"CoreEngine.Components.TransformComponent, CoreEngine\",");

                Indent(stringBuilder, 5);
                stringBuilder.AppendLine($"\"Data\": {{ {FormatTransformComponent(sceneDescription.TransformMatrix)} }}");

                Indent(stringBuilder, 4);
                stringBuilder.AppendLine("}");
              
                Indent(stringBuilder, 3);
                stringBuilder.AppendLine("]");

                Indent(stringBuilder, 2);
                stringBuilder.Append("}");

                if (i !=  moanaSceneDescriptions.Length - 1)
                {
                    stringBuilder.Append(",");
                }

                stringBuilder.AppendLine();
            }

            var finalOutput = outputTemplate.Replace("#ADDITIONAL_ENTITIES#", stringBuilder.ToString());
            File.WriteAllText(outputPath, finalOutput);
        }

        static void Main(string[] args)
        {
            const string inputDirectory = @"C:\island-basepackage-v1.1\island";
            const string outputDirectory = @"C:\perso\CoreEngine\samples\SceneViewer\Data";
            
            var outputDataDirectory = Path.Combine(outputDirectory, "MoanaIsland");

            if (!Directory.Exists(inputDirectory))
            {
                Console.WriteLine("ERROR: Input directory doesn't exist.");
            }

            var sceneDescriptionDirectory = Path.Combine(inputDirectory, "json");
            var sceneDirectories = Directory.GetDirectories(sceneDescriptionDirectory);
            var sceneDescriptions = new MoanaSceneDescription[sceneDirectories.Length];

            for (var i = 0; i < sceneDirectories.Length; i++)
            {
                var sceneDirectory = sceneDirectories[i];

                var inputFile = Path.Combine(sceneDirectory, $"{Path.GetFileName(sceneDirectory)}.json");
                Console.WriteLine($"Processing: {inputFile}");

                var options = new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters =
                    {
                        new MatrixJsonDataConverter()
                    }
                };

                if (!File.Exists(inputFile))
                {
                    continue;
                }

                // TODO: Skip isIronWood for now because it is more than 2GB
                if (inputFile.Contains("isIronwood"))
                {
                    continue;
                }

                var inputData = File.ReadAllText(inputFile);
                var sceneDescription = JsonSerializer.Deserialize<MoanaSceneDescription>(inputData, options);
                sceneDescriptions[i] = sceneDescription;

                Console.WriteLine(sceneDescription.TransformMatrix);
                Console.WriteLine(sceneDescription.GeomObjFile);
                Console.WriteLine(sceneDescription.Name);
                
                // TODO: Skip isIronWood for now because it is more than 2GB
                if (sceneDescription.GeomObjFile == null)
                {
                    continue;
                }

                var objFile = Path.Combine(inputDirectory, sceneDescription.GeomObjFile);
                var outputObjDirectory = Path.Combine(outputDataDirectory, Path.GetFileNameWithoutExtension(objFile));
                var outputObjFile = Path.Combine(outputObjDirectory, Path.GetFileName(objFile));

                if (!File.Exists(outputObjFile))
                {
                    Console.WriteLine($"Copying obj file:{objFile} to {outputObjDirectory}");

                    if (!Directory.Exists(outputObjDirectory))
                    {
                        Directory.CreateDirectory(outputObjDirectory);
                    }

                    File.Copy(objFile, outputObjFile);
                }
            }

            GenerateJsonScene(Path.Combine(outputDirectory, "MoanaIsland.cescene"), sceneDescriptions);
        }
    }
}
