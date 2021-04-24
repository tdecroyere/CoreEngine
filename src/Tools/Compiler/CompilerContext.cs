namespace CoreEngine.Tools.Compiler
{
    public class CompilerContext
    {
        public CompilerContext(string targetPlatform, string targetFilename, string inputDirectory, string outputDirectory, string rootOutputDirectory)
        {
            this.TargetPlatform = targetPlatform;
            this.SourceFilename = targetFilename;
            this.InputDirectory = inputDirectory;
            this.OutputDirectory = outputDirectory;
            this.RootOutputDirectory = rootOutputDirectory;
        }

        public string TargetPlatform
        {
            get;
        }

        public string SourceFilename
        {
            get;
        }

        public string InputDirectory
        {
            get;
        }

        public string? OutputDirectory
        {
            get;
        }

        public string RootOutputDirectory
        {
            get;
        }
    }
}