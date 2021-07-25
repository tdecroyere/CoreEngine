using System;
using System.Buffers;
using System.Numerics;

namespace CoreEngine
{
    public readonly struct BoundingBox2D
    {
		public BoundingBox2D(Vector2 minPoint, Vector2 maxPoint)
		{
			this.MinPoint = minPoint;
			this.MaxPoint = maxPoint;
		}

        public BoundingBox2D(ReadOnlySpan<Vector2> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            this.MinPoint = new Vector2(float.MaxValue, float.MaxValue);
            this.MaxPoint = new Vector2(float.MinValue, float.MinValue);

            for (var i = 0; i < points.Length; i++)
            {
                var point = points[i];

                var minX = (point.X < this.MinPoint.X) ? point.X : this.MinPoint.X;
                var minY = (point.Y < this.MinPoint.Y) ? point.Y : this.MinPoint.Y;

                var maxX = (point.X > this.MaxPoint.X) ? point.X : this.MaxPoint.X;
                var maxY = (point.Y > this.MaxPoint.Y) ? point.Y : this.MaxPoint.Y;
        
                this.MinPoint = new Vector2(minX, minY);
                this.MaxPoint = new Vector2(maxX, maxY);
            }
        }

        public Vector2 MinPoint { get; }
        public Vector2 MaxPoint { get; }

        public Vector2 Size
		{
			get
			{
				return this.MaxPoint - this.MinPoint;
			}
		}

		public float XSize
		{
			get
			{
                return this.MaxPoint.X - this.MinPoint.X;
			}
		}

		public float YSize
		{
			get
			{
                return this.MaxPoint.Y - this.MinPoint.Y;
			}
		}

		public Vector2 Center
		{
			get
			{
                return (this.MinPoint + this.MaxPoint) * 0.5f;
			}
		}

        public static BoundingBox2D AddPoint(in BoundingBox2D boundingBox, Vector2 point)
		{
			float minX = (point.X < boundingBox.MinPoint.X) ? point.X : boundingBox.MinPoint.X;
			float minY = (point.Y < boundingBox.MinPoint.Y) ? point.Y : boundingBox.MinPoint.Y;

			float maxX = (point.X > boundingBox.MaxPoint.X) ? point.X : boundingBox.MaxPoint.X;
			float maxY = (point.Y > boundingBox.MaxPoint.Y) ? point.Y : boundingBox.MaxPoint.Y;
	
			var minPoint = new Vector2(minX, minY);
			var maxPoint = new Vector2(maxX, maxY);

            return new BoundingBox2D(minPoint, maxPoint);
		}

        public static BoundingBox2D CreateMerged(in BoundingBox2D original, in BoundingBox2D additional) 
        {
            var minX = MathF.Min(original.MinPoint.X, additional.MinPoint.X);
            var minY = MathF.Min(original.MinPoint.Y, additional.MinPoint.Y);

            var maxX = MathF.Max(original.MaxPoint.X, additional.MaxPoint.X);
            var maxY = MathF.Max(original.MaxPoint.Y, additional.MaxPoint.Y);

            var minPoint = new Vector2(minX, minY);
            var maxPoint = new Vector2(maxX, maxY);

            return new BoundingBox2D(minPoint, maxPoint);
        }

        public override string ToString()
        {
            return $"Min: {this.MinPoint}, Max: {this.MaxPoint}";
        }
    }
}