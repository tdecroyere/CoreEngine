using System;

namespace CoreEngine.Tools.Compiler
{
    public class ResourceCompilerOutput
    {
        public ResourceCompilerOutput(string filename, ReadOnlyMemory<byte> data)
        {
            this.Filename = filename;
            this.Data = data;
        }

        public string Filename { get; }
        public ReadOnlyMemory<byte> Data { get; }
    }
}