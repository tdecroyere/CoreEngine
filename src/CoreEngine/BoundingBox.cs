using System;
using System.Buffers;
using System.Numerics;

namespace CoreEngine
{
    public readonly struct BoundingBox
    {
		public BoundingBox(Vector3 minPoint, Vector3 maxPoint)
		{
			this.MinPoint = minPoint;
			this.MaxPoint = maxPoint;
		}

        public BoundingBox(ReadOnlySpan<Vector3> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            this.MinPoint = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            this.MaxPoint = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (var i = 0; i < points.Length; i++)
            {
                var point = points[i];

                var minX = (point.X < this.MinPoint.X) ? point.X : this.MinPoint.X;
                var minY = (point.Y < this.MinPoint.Y) ? point.Y : this.MinPoint.Y;
                var minZ = (point.Z < this.MinPoint.Z) ? point.Z : this.MinPoint.Z;

                var maxX = (point.X > this.MaxPoint.X) ? point.X : this.MaxPoint.X;
                var maxY = (point.Y > this.MaxPoint.Y) ? point.Y : this.MaxPoint.Y;
                var maxZ = (point.Z > this.MaxPoint.Z) ? point.Z : this.MaxPoint.Z;
        
                this.MinPoint = new Vector3(minX, minY, minZ);
                this.MaxPoint = new Vector3(maxX, maxY, maxZ);
            }
        }

        public Vector3 MinPoint { get; }
        public Vector3 MaxPoint { get; }

        public Vector3 Size
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

		public float ZSize
		{
			get
			{
                return this.MaxPoint.Z - this.MinPoint.Z;
			}
        }

		public Vector3 Center
		{
			get
			{
                return (this.MinPoint + this.MaxPoint) * 0.5f;
			}
		}

        public static BoundingBox AddPoint(in BoundingBox boundingBox, Vector3 point)
		{
			float minX = (point.X < boundingBox.MinPoint.X) ? point.X : boundingBox.MinPoint.X;
			float minY = (point.Y < boundingBox.MinPoint.Y) ? point.Y : boundingBox.MinPoint.Y;
			float minZ = (point.Z < boundingBox.MinPoint.Z) ? point.Z : boundingBox.MinPoint.Z;

			float maxX = (point.X > boundingBox.MaxPoint.X) ? point.X : boundingBox.MaxPoint.X;
			float maxY = (point.Y > boundingBox.MaxPoint.Y) ? point.Y : boundingBox.MaxPoint.Y;
			float maxZ = (point.Z > boundingBox.MaxPoint.Z) ? point.Z : boundingBox.MaxPoint.Z;
	
			var minPoint = new Vector3(minX, minY, minZ);
			var maxPoint = new Vector3(maxX, maxY, maxZ);

            return new BoundingBox(minPoint, maxPoint);
		}

        public static BoundingBox CreateMerged(in BoundingBox original, in BoundingBox additional) 
        {
            var minX = MathF.Min(original.MinPoint.X, additional.MinPoint.X);
            var minY = MathF.Min(original.MinPoint.Y, additional.MinPoint.Y);
            var minZ = MathF.Min(original.MinPoint.Z, additional.MinPoint.Z);

            var maxX = MathF.Max(original.MaxPoint.X, additional.MaxPoint.X);
            var maxY = MathF.Max(original.MaxPoint.Y, additional.MaxPoint.Y);
            var maxZ = MathF.Max(original.MaxPoint.Z, additional.MaxPoint.Z);

            var minPoint = new Vector3(minX, minY, minZ);
            var maxPoint = new Vector3(maxX, maxY, maxZ);

            return new BoundingBox(minPoint, maxPoint);
        }

        public static BoundingBox CreateTransformed(in BoundingBox boundingBox, Matrix4x4 matrix)
        {
            var pointList = ArrayPool<Vector3>.Shared.Rent(8);

            pointList[0] = Vector3.Transform(boundingBox.MinPoint + new Vector3(0, 0, 0), matrix);
            pointList[1] = Vector3.Transform(boundingBox.MinPoint + new Vector3(boundingBox.XSize, 0, 0), matrix);
            pointList[2] = Vector3.Transform(boundingBox.MinPoint + new Vector3(0, boundingBox.YSize, 0), matrix);
            pointList[3] = Vector3.Transform(boundingBox.MinPoint + new Vector3(boundingBox.XSize, boundingBox.YSize, 0), matrix);
            pointList[4] = Vector3.Transform(boundingBox.MinPoint + new Vector3(0, 0, boundingBox.ZSize), matrix);
            pointList[5] = Vector3.Transform(boundingBox.MinPoint + new Vector3(boundingBox.XSize, 0, boundingBox.ZSize), matrix);
            pointList[6] = Vector3.Transform(boundingBox.MinPoint + new Vector3(0, boundingBox.YSize, boundingBox.ZSize), matrix);
            pointList[7] = Vector3.Transform(boundingBox.MinPoint + new Vector3(boundingBox.XSize, boundingBox.YSize, boundingBox.ZSize), matrix);

            var result = new BoundingBox(pointList.AsSpan(0, 8));
            ArrayPool<Vector3>.Shared.Return(pointList);

            return result;
        }

        public static BoundingBox2D CreateTransformed2D(in BoundingBox boundingBox, Matrix4x4 matrix)
        {
            var pointList = ArrayPool<Vector2>.Shared.Rent(8);
            var currentPointIndex = 0;

            var sourcePoint = boundingBox.MinPoint + new Vector3(0, 0, 0);
            var point = Vector4.Transform(new Vector4(sourcePoint, 1.0f), matrix);

            if (point.Z / point.W > 0.0f)
            {
                pointList[currentPointIndex++] = new Vector2(point.X, point.Y) / point.W;
            }

            sourcePoint = boundingBox.MinPoint + new Vector3(boundingBox.XSize, 0, 0);
            point = Vector4.Transform(new Vector4(sourcePoint, 1.0f), matrix);

            if (point.Z / point.W > 0.0f)
            {
                pointList[currentPointIndex++] = new Vector2(point.X, point.Y) / point.W;
            }

            sourcePoint = boundingBox.MinPoint + new Vector3(0, boundingBox.YSize, 0);
            point = Vector4.Transform(new Vector4(sourcePoint, 1.0f), matrix);

            if (point.Z / point.W > 0.0f)
            {
                pointList[currentPointIndex++] = new Vector2(point.X, point.Y) / point.W;
            }

            sourcePoint = boundingBox.MinPoint + new Vector3(boundingBox.XSize, boundingBox.YSize, 0);
            point = Vector4.Transform(new Vector4(sourcePoint, 1.0f), matrix);

            if (point.Z / point.W > 0.0f)
            {
                pointList[currentPointIndex++] = new Vector2(point.X, point.Y) / point.W;
            }

            sourcePoint = boundingBox.MinPoint + new Vector3(0, 0, boundingBox.ZSize);
            point = Vector4.Transform(new Vector4(sourcePoint, 1.0f), matrix);

            if (point.Z / point.W > 0.0f)
            {
                pointList[currentPointIndex++] = new Vector2(point.X, point.Y) / point.W;
            }

            sourcePoint = boundingBox.MinPoint + new Vector3(boundingBox.XSize, 0, boundingBox.ZSize);
            point = Vector4.Transform(new Vector4(sourcePoint, 1.0f), matrix);

            if (point.Z / point.W > 0.0f)
            {
                pointList[currentPointIndex++] = new Vector2(point.X, point.Y) / point.W;
            }

            sourcePoint = boundingBox.MinPoint + new Vector3(0, boundingBox.YSize, boundingBox.ZSize);
            point = Vector4.Transform(new Vector4(sourcePoint, 1.0f), matrix);
            
            if (point.Z / point.W > 0.0f)
            {
                pointList[currentPointIndex++] = new Vector2(point.X, point.Y) / point.W;
            }

            sourcePoint = boundingBox.MinPoint + new Vector3(boundingBox.XSize, boundingBox.YSize, boundingBox.ZSize);
            point = Vector4.Transform(new Vector4(sourcePoint, 1.0f), matrix);

            if (point.Z / point.W > 0.0f)
            {
                pointList[currentPointIndex++] = new Vector2(point.X, point.Y) / point.W;
            }

            var result = new BoundingBox2D(pointList.AsSpan(0, currentPointIndex));
            ArrayPool<Vector2>.Shared.Return(pointList);

            return result;
        }

        public override string ToString()
        {
            return $"Min: {this.MinPoint}, Max: {this.MaxPoint}";
        }
    }
}