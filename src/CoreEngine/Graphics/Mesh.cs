using System;
using System.Collections.Generic;
using CoreEngine.Diagnostics;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class Mesh : Resource
    {
        public Mesh(GeometryPacket geometryPacket) : base(0, string.Empty)
        {
            this.GeometryPacket = geometryPacket;
        }

        internal Mesh(uint resourceId, string path) : base(resourceId, path)
        {

        }

        public GeometryPacket GeometryPacket { get; internal set; }
        public IList<GeometryInstance> GeometryInstances { get; } = new List<GeometryInstance>();
    }
}