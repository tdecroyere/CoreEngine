using System.IO;
using Microsoft.CodeAnalysis;

namespace CoreEngine.SourceGenerators
{
    public static class Utils
    {
        public static void AddSource(GeneratorExecutionContext context, string fileName, string sourceCode)
        {
            var firstLocation = Path.GetDirectoryName(context.Compilation.Assembly.Locations[0].GetLineSpan().Path);
            var projectPath = FindProjectPath(firstLocation);
            Logger.WriteMessage($"ProjectPath: {projectPath}");

            var filePath = Path.Combine(projectPath, "CoreEngine.SourceGenerators", typeof(GetComponentHashCodeGenerator).FullName, fileName);
            Logger.WriteMessage($"FinalPath: {filePath}");

            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }

            context.AddSource(fileName, sourceCode);
            File.WriteAllText($"{filePath}", sourceCode);
        }
        
        private static string FindProjectPath(string path)
        {
            if (Directory.GetFiles(path, "*.csproj").Length > 0)
            {
                return path;
            }

            var parentDirectory = Directory.GetParent(path);

            if (parentDirectory != null)
            {
                return FindProjectPath(parentDirectory.FullName);
            }

            else
            {
                return path;
            }
        }
    }
}