using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild;
using CoreEngine.Diagnostics;
using System;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace CoreEngine.Tools.Compiler
{
    public class ProjectCompiler
    {
        public ProjectCompiler()
        {
        }

        public async Task CompileProject(string projectPath, string searchPattern, bool isWatchMode, bool rebuildAll)
        {
            var inputDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath));

            if (inputDirectory == null)
            {
                Logger.WriteMessage($"Input path is not a directory.", LogMessageTypes.Error);
                return;
            }

            var inputObjDirectory = Path.Combine(inputDirectory, "obj");

            var fileTrackerPath = Path.Combine(inputObjDirectory, "FileTracker");
            var fileTracker = new FileTracker();

            if (!rebuildAll || isWatchMode)
            {
                fileTracker.ReadFile(fileTrackerPath);
            }

            using var workspace = MSBuildWorkspace.Create();

            foreach (var diagnostic in workspace.Diagnostics)
            {
                Logger.WriteMessage($"{diagnostic.Message}", LogMessageTypes.Warning);
            }

            var project = await workspace.OpenProjectAsync(projectPath);
            var target = "win-x64";
            var outputDirectory = Path.GetFullPath(Path.Combine(inputDirectory, "bin", target));

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var recompileProject = false;

            foreach (var document in project.Documents)
            {
                if (fileTracker.HasFileChanged(document.FilePath))
                {
                    recompileProject = true;
                    break;
                }
            }

            if (recompileProject)
            {
                Logger.BeginAction($"Recompiling project {project.Name}...");

                var compilation = await project.GetCompilationAsync();

                if (compilation != null)
                {
                    var diagnostics = compilation.GetDiagnostics();

                    foreach (var diagnostic in diagnostics)
                    {
                        if (diagnostic.Severity != DiagnosticSeverity.Hidden)
                        {
                            LogMessageTypes messageType;

                            switch(diagnostic.Severity)
                            {
                                case DiagnosticSeverity.Warning:
                                    messageType = LogMessageTypes.Warning;
                                    break;

                                case DiagnosticSeverity.Error:
                                    messageType = LogMessageTypes.Error;
                                    break;

                                default:
                                    messageType = LogMessageTypes.Normal;
                                    break;
                            }

                            Logger.WriteMessage($"{diagnostic}", messageType);
                        }
                    }

                    if (diagnostics.Count(n => n.Severity == DiagnosticSeverity.Error) == 0)
                    {
                        using var stream = new FileStream(Path.Combine(outputDirectory, project.AssemblyName + ".dll"), FileMode.Create);
                        compilation.Emit(stream);
                    }
                }

                Logger.EndAction();
            }

            // TODO: Remove deleted files from file tracker
            //CleanupOutputDirectory(outputDirectory, remainingDestinationFiles);
            fileTracker.WriteFile(fileTrackerPath);
        }
    }
}