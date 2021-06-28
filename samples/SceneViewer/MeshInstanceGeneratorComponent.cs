using System;
using System.Numerics;

namespace CoreEngine.Samples.SceneViewer
{
    // TODO: Add an entity ref to ref the mesh instance
    public partial struct MeshInstanceGeneratorComponent : IComponentData
    {
        public float MeshInstanceCountWidth { get; set; }
        public float Spacing { get; set; }

        public void SetDefaultValues()
        {
            this.MeshInstanceCountWidth = 10;
            this.Spacing = 1;
        }
    }
}