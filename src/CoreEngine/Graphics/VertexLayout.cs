using System;

namespace CoreEngine.Graphics
{
    public struct VertexLayout
    {
        //private readonly uint rawVertexLayout;

        public VertexLayout(params VertexElementType[] elementTypes)
        {
            if (elementTypes == null)
            {
                throw new ArgumentNullException(nameof(elementTypes));
            }

            if (elementTypes.Length > 8)
            {
                throw new NotSupportedException("Vertex layout doesn't support more than 8 vertex elements.");
            }

            //this.rawVertexLayout = 0;
        }
    }
}