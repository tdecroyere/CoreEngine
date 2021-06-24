using System;
using System.Numerics;

namespace CoreEngine.Tools.Compiler.ResourceCompilers.Meshes
{
    public class OldBoundingBox
    {
        public OldBoundingBox() : this(new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity), new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity))
        {
        }

		public OldBoundingBox(Vector3 minPoint, Vector3 maxPoint)
		{
			this.MinPoint = minPoint;
			this.MaxPoint = maxPoint;
		}

        public Vector3 MinPoint { get; private set; }
        public Vector3 MaxPoint { get; private set; }

        public bool IsEmpty
		{
			get
			{
                return (this.MinPoint.X > this.MaxPoint.X) || (this.MinPoint.Y > this.MaxPoint.Y) || (this.MinPoint.Z > this.MaxPoint.Z);
			}
		}

        

		public Vector3 Center
		{
			get
			{
                return (this.MinPoint + this.MaxPoint) * 0.5f;
			}
		}

        public override string ToString()
        {
            if (this.IsEmpty)
            {
                return "Empty";
            }

            return $"Min: {this.MinPoint}, Max: {this.MaxPoint}";
        }
    }
}