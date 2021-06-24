using System;
using System.Threading.Tasks;

namespace CoreEngine.Tools.Compiler.ResourceCompilers.Meshes
{
    public abstract class MeshDataReader
    {
        public abstract Task<ImportMeshData?> ReadAsync(ReadOnlyMemory<byte> sourceData);
    }
}