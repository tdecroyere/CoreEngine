using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild;
using CoreEngine.Diagnostics;
using System;
using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Runtime.Loader;

namespace CoreEngine.Tools.Compiler
{
    public static class ProjectCompiler
    {
        private static readonly bool logAssemblies;

        public static async Task CompileProject(string projectPath, string searchPattern, bool isWatchMode, bool rebuildAll)
        {
            var inputDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath));

            if (inputDirectory == null)
            {
                Logger.WriteMessage($"Input path is not a directory.", LogMessageTypes.Error);
                return;
            }

            var target = "win-x64";
            var outputDirectory = Path.GetFullPath(Path.Combine(inputDirectory, "bin", target));

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var remainingDestinationFiles = new List<string>(Directory.GetFiles(outputDirectory, "*", SearchOption.AllDirectories));
            var compiledFilesCount = 0;

            if (searchPattern != "*")
            {
                remainingDestinationFiles.Clear();
            } 

            var inputObjDirectory = Path.Combine(inputDirectory, "obj");
            var fileTrackerPath = Path.Combine(inputObjDirectory, "FileTracker");
            var fileTracker = new FileTracker();

            if (!rebuildAll || isWatchMode)
            {
                fileTracker.ReadFile(fileTrackerPath);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            compiledFilesCount += await BuildDotnet(projectPath, outputDirectory, fileTracker, remainingDestinationFiles);

            if (logAssemblies)
            {
                foreach (var assemblyLoadContext in AssemblyLoadContext.All)
                {
                    Logger.BeginAction($"Assembly Load Context: {assemblyLoadContext.Name}");

                    foreach (var assembly in assemblyLoadContext.Assemblies)
                    {
                        Logger.WriteMessage($"Loaded Assembly: {assembly.GetName().FullName}");
                    }

                    Logger.EndAction();
                }
            }

            compiledFilesCount += await BuildResources(projectPath, outputDirectory, searchPattern, fileTracker, remainingDestinationFiles);

            stopwatch.Stop();

            Logger.WriteLine();
            Logger.WriteMessage($"Success: Compiled {compiledFilesCount} file(s) in {stopwatch.Elapsed}.", LogMessageTypes.Success);

            // TODO: Remove deleted files from file tracker
            CleanupOutputDirectory(outputDirectory, remainingDestinationFiles);
            fileTracker.WriteFile(fileTrackerPath);
        }

        private async static Task<int> BuildDotnet(string projectPath, string outputDirectory, FileTracker fileTracker, IList<string> remainingDestinationFiles)
        {
            var recompileProject = false;

            foreach (var sourceFile in Directory.GetFiles(Path.GetDirectoryName(projectPath), "*.cs", new EnumerationOptions() { RecurseSubdirectories = true }))
            {
                var hasFileChanged = fileTracker.HasFileChanged(sourceFile);
                var destinationFiles = fileTracker.GetDestinationFiles(sourceFile);

                var destinationFilesExist = true;

                foreach (var destinationFile in destinationFiles)
                {
                    if (!File.Exists(destinationFile))
                    {
                        destinationFilesExist = false;
                        break;
                    }
                }

                if (hasFileChanged || !destinationFilesExist)
                {
                    recompileProject = true;
                    break;
                }

                else
                {
                    foreach (var destinationFile in destinationFiles)
                    {
                        remainingDestinationFiles.Remove(destinationFile);
                    }
                }
            }

            if (recompileProject)
            {
                using var workspace = MSBuildWorkspace.Create();
                var project = await workspace.OpenProjectAsync(projectPath);

                foreach (var diagnostic in workspace.Diagnostics)
                {
                    Logger.WriteMessage($"{diagnostic.Message}", LogMessageTypes.Warning);
                }

                var compilation = await project.GetCompilationAsync();

                if (compilation != null)
                {
                    Logger.BeginAction($"Recompiling project {project.Name}...");
                    compilation = compilation.WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, 
                                                            optimizationLevel: OptimizationLevel.Debug));

                    var diagnostics = compilation.GetDiagnostics();

                    foreach (var diagnostic in diagnostics)
                    {
                        if (diagnostic.Severity != DiagnosticSeverity.Hidden)
                        {
                            var messageType = diagnostic.Severity switch
                            {
                                DiagnosticSeverity.Warning => LogMessageTypes.Warning,
                                DiagnosticSeverity.Error => LogMessageTypes.Error,
                                _ => LogMessageTypes.Normal,
                            };
                            Logger.WriteMessage($"{diagnostic}", messageType);
                        }
                    }

                    if (!diagnostics.Any(n => n.Severity == DiagnosticSeverity.Error))
                    {
                        using var stream = new MemoryStream();
                        using var pdbStream = new MemoryStream();
                        compilation.Emit(stream, pdbStream: pdbStream, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));

                        await File.WriteAllBytesAsync(Path.Combine(outputDirectory, project.AssemblyName + ".dll"), stream.ToArray());
                        await File.WriteAllBytesAsync(Path.Combine(outputDirectory, project.AssemblyName + ".pdb"), pdbStream.ToArray());

                        stream.Position = 0;
                        AssemblyLoadContext.All.First().LoadFromStream(stream);
                    }

                    Logger.EndAction();
                    return 1;
                }
            }

            var assemblyPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(projectPath) + ".dll");
            AssemblyLoadContext.All.First().LoadFromAssemblyPath(assemblyPath);

            return 0;
        }

        private async static Task<int> BuildResources(string projectPath, string outputDirectory, string searchPattern, FileTracker fileTracker, IList<string> remainingDestinationFiles)
        {
            var compiledResources = 0;

            var inputDirectory = Path.GetDirectoryName(projectPath);

            var targetPlatform = "win";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                targetPlatform = "osx";
            }

            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                targetPlatform = "linux";
            }

            var resourceCompilers = AddInternalDataCompilers();

            foreach (var sourceFile in Directory.GetFiles(inputDirectory, searchPattern, new EnumerationOptions() { RecurseSubdirectories = true }))
            {
                var hasFileChanged = fileTracker.HasFileChanged(sourceFile) || searchPattern != "*";
                var destinationFiles = fileTracker.GetDestinationFiles(sourceFile);

                var destinationFilesExist = false;

                foreach (var destinationFile in destinationFiles)
                {
                    if (File.Exists(destinationFile))
                    {
                        destinationFilesExist = true;
                        break;
                    }
                }

                if ((hasFileChanged || !destinationFilesExist) && resourceCompilers.ContainsKey(Path.GetExtension(sourceFile)))
                {
                    Logger.WriteMessage($"Processing file '{sourceFile}'");

                    var sourceFileAbsoluteDirectory = ConstructSourceFileAbsoluteDirectory(inputDirectory, sourceFile);
                    var destinationPath = Path.Combine(outputDirectory, sourceFileAbsoluteDirectory);

                    if (!Directory.Exists(destinationPath))
                    {
                        Directory.CreateDirectory(destinationPath);
                    }

                    var resourceCompiler = resourceCompilers[Path.GetExtension(sourceFile)];
                    var compilerContext = new CompilerContext(targetPlatform, Path.GetFileName(sourceFile), Path.GetDirectoryName(sourceFile), destinationPath, outputDirectory);

                    var resourceData = await File.ReadAllBytesAsync(sourceFile);
                    var compilerOutput = await resourceCompiler.First().CompileAsync(resourceData, compilerContext);

                    var outputDestinationFiles = new string[compilerOutput.Length];

                    for (var i = 0; i < compilerOutput.Length; i++)
                    {
                        var output = compilerOutput.Span[i];
                        var destinationFile = Path.Combine(destinationPath, output.Filename);

                        Logger.WriteMessage($"Compiler Output: {destinationFile}");
                        remainingDestinationFiles.Remove(destinationFile);

                        await File.WriteAllBytesAsync(destinationFile, output.Data.ToArray());
                        compiledResources++;

                        outputDestinationFiles[i] = destinationFile;
                    }

                    fileTracker.AddDestinationFiles(sourceFile, outputDestinationFiles);
                }

                else
                {
                    foreach (var destinationFile in destinationFiles)
                    {
                        remainingDestinationFiles.Remove(destinationFile);
                    }
                }
            }

            return compiledResources;
        }

        private static Dictionary<string, List<ResourceCompiler>> AddInternalDataCompilers()
        {
            var resourceCompilers = new Dictionary<string, List<ResourceCompiler>>();
            var assembly = Assembly.GetExecutingAssembly();

            foreach (var type in assembly.GetTypes())
            {
                if (type.IsSubclassOf(typeof(ResourceCompiler)) && !type.IsAbstract)
                {
                    var resourceCompiler = (ResourceCompiler)Activator.CreateInstance(type)!;

                    foreach (var supportedExtension in resourceCompiler.SupportedSourceExtensions)
                    {
                        if (!resourceCompilers.ContainsKey(supportedExtension))
                        {
                            resourceCompilers.Add(supportedExtension, new List<ResourceCompiler>());
                        }

                        resourceCompilers[supportedExtension].Add(resourceCompiler);
                    }
                }
            }

            return resourceCompilers;
        }

        private static string ConstructSourceFileAbsoluteDirectory(string inputDirectory, string sourceFile)
        {
            var directoryName = Path.GetDirectoryName(sourceFile);

            if (directoryName == null)
            {
                throw new ArgumentException("Input is not a directory.", nameof(inputDirectory));
            }

            var sourceFileAbsoluteDirectory = directoryName.Replace(inputDirectory, string.Empty, StringComparison.InvariantCulture);

            if (!string.IsNullOrEmpty(sourceFileAbsoluteDirectory))
            {
                sourceFileAbsoluteDirectory = sourceFileAbsoluteDirectory.Substring(1);
            }

            return sourceFileAbsoluteDirectory;
        }

        private static void CleanupOutputDirectory(string outputDirectory, List<string> remainingDestinationFiles)
        {
            foreach (var remainingDestinationFile in remainingDestinationFiles)
            {
                if (Path.GetFileName(remainingDestinationFile)[0] != '.' && Path.GetExtension(remainingDestinationFile) != ".dll" && Path.GetExtension(remainingDestinationFile) != ".pdb")
                {
                    Logger.WriteMessage($"Cleaning file '{remainingDestinationFile}...", LogMessageTypes.Debug);
                    File.Delete(remainingDestinationFile);
                }
            }

            foreach (var directory in Directory.GetDirectories(outputDirectory))
            {
                if (Directory.GetFiles(directory).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0)
                {
                    Logger.WriteMessage($"Cleaning empty directory '{directory}...", LogMessageTypes.Debug);
                    Directory.Delete(directory, false);
                }
            }
        }
    }
}