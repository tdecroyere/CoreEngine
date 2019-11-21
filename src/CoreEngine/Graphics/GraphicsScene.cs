using System;
using System.Collections.Generic;

namespace CoreEngine.Graphics
{
    public class GraphicsScene
    {
        private Camera? activeCamera;

        public GraphicsScene()
        {
            this.activeCamera = null;
            this.Cameras = new Dictionary<Entity, Camera>();
            this.MeshInstances = new Dictionary<Entity, MeshInstance>();
        }

        public Camera? ActiveCamera 
        { 
            get
            {
                if (this.activeCamera == null && this.Cameras.Count > 0)
                {
                    // TODO: Change that to an hybrid list
                    var enumerator = this.Cameras.Values.GetEnumerator();
                    enumerator.MoveNext();
                    return enumerator.Current;
                }

                return this.activeCamera;
            }

            set
            {
                this.activeCamera = value;
            } 
        }

        public Dictionary<Entity, Camera> Cameras { get; }
        public Dictionary<Entity, MeshInstance> MeshInstances { get; }
    }
}