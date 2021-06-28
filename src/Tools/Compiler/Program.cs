using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CoreEngine;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.HostServices;
using CoreEngine.Resources;
using CoreEngine.Tools.Compiler;

public static class Program
{
    [UnmanagedCallersOnly(EntryPoint = "main")]
    public static void Main(HostPlatform hostPlatform)
    {
        Logger.BeginAction("Starting CoreEngine Compiler");

        using var resourcesManager = new ResourcesManager();
        resourcesManager.AddResourceStorage(new FileSystemResourceStorage("./Resources"));

        using var graphicsManager = new GraphicsManager(hostPlatform.GraphicsService, resourcesManager);


        Logger.EndAction();

        var args = Utils.GetCommandLineArguments();
        var commandLineParser = SetupCommandLineParameters();
        commandLineParser.Invoke(args);
    }

    private static async Task RunCompilePass(string projectPath, string? searchPattern, bool isWatchMode, bool rebuildAll)
    {
        if (!isWatchMode)
        {
            Logger.WriteMessage($"Compiling '{projectPath}'...", LogMessageTypes.Action);
        }

        try
        {
            await ProjectCompiler.CompileProject(projectPath, searchPattern, isWatchMode, rebuildAll);
        }

        catch (Exception e)
        {
            Logger.WriteMessage($"{e.ToString()}", LogMessageTypes.Error);
        }
    }

    private static async Task CompileProject(string projectPath, string searchPattern, bool isWatchMode, bool rebuildAll)
    {
        if (!isWatchMode)
        {
            await RunCompilePass(projectPath, searchPattern, isWatchMode, rebuildAll);
        }

        else
        {
            Logger.WriteMessage("Entering watch mode...", LogMessageTypes.Action);

            while (true)
            {
                await RunCompilePass(projectPath, searchPattern, isWatchMode, rebuildAll);
                Thread.Sleep(1000);
            }
        }
    }

    private static Parser SetupCommandLineParameters()
    {
        var rootCommand = new RootCommand()
        {
            Name = "Compiler",
            Description = "CoreEngine Compiler",
            Handler = CommandHandler.Create((string projectPath, string searchPattern, bool watch, bool rebuild) =>
            {
                return CompileProject(projectPath, searchPattern, watch, rebuild);
            })
        };

        var projectPathArgument = new Argument("projectPath");
        projectPathArgument.SetDefaultValue(".");
        projectPathArgument.Description = "Path of the project to compile.";

        var searchPatternArgument = new Argument("searchPattern");
        searchPatternArgument.SetDefaultValue("*");
        searchPatternArgument.Description = "Search pattern that the compiler will use.";

        return new CommandLineBuilder(rootCommand)
            .AddOption(new Option<bool>("--watch", () => false, "Run the compiler in watch mode."))
            .AddOption(new Option<bool>("--rebuild", () => false, "Rebuild all project."))
            .AddArgument(projectPathArgument)
            .AddArgument(searchPatternArgument)
            .UseDefaults()
            .Build();
    }
}
