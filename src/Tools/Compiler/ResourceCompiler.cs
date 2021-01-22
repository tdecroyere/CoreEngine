using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreEngine.Tools.Compiler
{
    public abstract class ResourceCompiler
    {
        public abstract string Name
        {
            get;
        }

        public abstract IList<string> SupportedSourceExtensions
        {
            get;
        }

        public abstract string DestinationExtension
        {
            get;
        }

        public virtual string? MultipleOutputDirectory
        {
            get
            {
                return null;
            }
        }

        public abstract Task<ReadOnlyMemory<ResourceCompilerOutput>> CompileAsync(ReadOnlyMemory<byte> sourceData, CompilerContext context);
    }
}
