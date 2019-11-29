using System;
using System.Numerics;

namespace CoreEngine
{
    public class BoundingFrustum
    {
        public BoundingFrustum(Matrix4x4 matrix)
        {
            ExtractMatrix(matrix);

            this.LeftTopNearPoint = IntersectionPoint(this.LeftPlane, this.TopPlane, this.NearPlane);
            this.LeftTopFarPoint = IntersectionPoint(this.LeftPlane, this.TopPlane, this.FarPlane);
            this.LeftBottomNearPoint = IntersectionPoint(this.LeftPlane, this.BottomPlane, this.NearPlane);
            this.LeftBottomFarPoint = IntersectionPoint(this.LeftPlane, this.BottomPlane, this.FarPlane);
            this.RightTopNearPoint = IntersectionPoint(this.RightPlane, this.TopPlane, this.NearPlane);
            this.RightTopFarPoint = IntersectionPoint(this.RightPlane, this.TopPlane, this.FarPlane);
            this.RightBottomNearPoint = IntersectionPoint(this.RightPlane, this.BottomPlane, this.NearPlane);
            this.RightBottomFarPoint = IntersectionPoint(this.RightPlane, this.BottomPlane, this.FarPlane);
        }
        
        public Plane LeftPlane { get; private set; }
        public Plane RightPlane { get; private set; }
        public Plane TopPlane { get; private set; }
        public Plane BottomPlane { get; private set; }
        public Plane NearPlane { get; private set; }
        public Plane FarPlane { get; private set; }

        public Vector3 LeftTopNearPoint { get; private set; }
        public Vector3 LeftTopFarPoint { get; private set; }
        public Vector3 LeftBottomNearPoint { get; private set; }
        public Vector3 LeftBottomFarPoint { get; private set; }
        public Vector3 RightTopNearPoint { get; private set; }
        public Vector3 RightTopFarPoint { get; private set; }
        public Vector3 RightBottomNearPoint { get; private set; }
        public Vector3 RightBottomFarPoint { get; private set; }

        private void ExtractMatrix(Matrix4x4 matrix)
        {
            // Left plane
            var a = matrix.M14 + matrix.M11;
            var b = matrix.M24 + matrix.M21;
            var c = matrix.M34 + matrix.M31;
            var d = matrix.M44 + matrix.M41;

            this.LeftPlane = Plane.Normalize(new Plane(-a, -b, -c, -d));

            // Right clipping plane
            a = matrix.M14 - matrix.M11; 
            b = matrix.M24 - matrix.M21; 
            c = matrix.M34 - matrix.M31; 
            d = matrix.M44 - matrix.M41;

            this.RightPlane = Plane.Normalize(new Plane(-a, -b, -c, -d));

            // Top clipping plane
            a = matrix.M14 - matrix.M12; 
            b = matrix.M24 - matrix.M22; 
            c = matrix.M34 - matrix.M32; 
            d = matrix.M44 - matrix.M42;

            this.TopPlane = Plane.Normalize(new Plane(-a, -b, -c, -d));

            // Bottom clipping plane
            a = matrix.M14 + matrix.M12; 
            b = matrix.M24 + matrix.M22; 
            c = matrix.M34 + matrix.M32; 
            d = matrix.M44 + matrix.M42;

            this.BottomPlane = Plane.Normalize(new Plane(-a, -b, -c, -d));

            // Near clipping plane
            a = matrix.M13; 
            b = matrix.M23; 
            c = matrix.M33; 
            d = matrix.M43;

            this.NearPlane = Plane.Normalize(new Plane(-a, -b, -c, -d));

            // Far clipping plane
            a = matrix.M14 - matrix.M13; 
            b = matrix.M24 - matrix.M23; 
            c = matrix.M34 - matrix.M33; 
            d = matrix.M44 - matrix.M43;

            this.FarPlane = Plane.Normalize(new Plane(-a, -b, -c, -d));
        }

        private static Vector3 IntersectionPoint(Plane a, Plane b, Plane c)
        {
            // Formula used
            //                d1 ( N2 * N3 ) + d2 ( N3 * N1 ) + d3 ( N1 * N2 )
            //P =   -------------------------------------------------------------------------
            //                             N1 . ( N2 * N3 )
            //
            // Note: N refers to the normal, d refers to the displacement. '.' means dot product. '*' means cross product
            
            var cross = Vector3.Cross(b.Normal, c.Normal);
            var f = Vector3.Dot(a.Normal, cross);
            f *= -1.0f;
            
            cross = Vector3.Cross(b.Normal, c.Normal);
            var v1 = Vector3.Multiply(cross, a.D);
            //v1 = (a.D * (Vector3.Cross(b.Normal, c.Normal)));
            
            cross = Vector3.Cross(c.Normal, a.Normal);
            var v2 = Vector3.Multiply(cross, b.D);
            //v2 = (b.D * (Vector3.Cross(c.Normal, a.Normal)));
            
            cross = Vector3.Cross(a.Normal, b.Normal);
            var v3 = Vector3.Multiply(cross, c.D);
            //v3 = (c.D * (Vector3.Cross(a.Normal, b.Normal)));

            var result = new Vector3();
            
            result.X = (v1.X + v2.X + v3.X) / f;
            result.Y = (v1.Y + v2.Y + v3.Y) / f;
            result.Z = (v1.Z + v2.Z + v3.Z) / f;

            return result;
        }
        

        public override string ToString()
        {
            return $"Left: {this.LeftPlane}, Right: {this.RightPlane}, Top: {this.TopPlane}, Bottom: {this.BottomPlane}, Near: {this.NearPlane}, Far: {this.FarPlane}";
        }
    }
}