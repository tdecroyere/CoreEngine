using System;

namespace CoreEngine.Graphics
{
    public struct VertexLayout
    {
        private uint rawVertexLayout;

        public VertexLayout(VertexElementType[] elementTypes)
        {
            this.rawVertexLayout = 0;
        }
    }
}