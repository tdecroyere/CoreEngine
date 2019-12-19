using System.Numerics;

namespace CoreEngine
{
    public static class BoundingIntersectUtils
    {
        public static bool Intersect(BoundingFrustum frustum, BoundingBox box)
        {
            if (!Intersect(frustum.LeftPlane, box))
            {
                return false;
            }

            if (!Intersect(frustum.RightPlane, box))
            {
                return false;
            }

            if (!Intersect(frustum.TopPlane, box))
            {
                return false;
            }

            if (!Intersect(frustum.BottomPlane, box))
            {
                return false;
            }

            if (!Intersect(frustum.NearPlane, box))
            {
                return false;
            }

            if (!Intersect(frustum.FarPlane, box))
            {
                return false;
            }

            return true;
        }

        private static bool Intersect(Plane plane, BoundingBox box)
        {
            if (Plane.Dot(plane, new Vector4(box.MinPoint.X, box.MinPoint.Y, box.MinPoint.Z, 1.0f)) <= 0.0f)
            {
                return true;
            }

            if (Plane.Dot(plane, new Vector4(box.MaxPoint.X, box.MinPoint.Y, box.MinPoint.Z, 1.0f)) <= 0.0f)
            {
                return true;
            }

            if (Plane.Dot(plane, new Vector4(box.MinPoint.X, box.MaxPoint.Y, box.MinPoint.Z, 1.0f)) <= 0.0f)
            {
                return true;
            }

            if (Plane.Dot(plane, new Vector4(box.MaxPoint.X, box.MaxPoint.Y, box.MinPoint.Z, 1.0f)) <= 0.0f)
            {
                return true;
            }

            if (Plane.Dot(plane, new Vector4(box.MinPoint.X, box.MinPoint.Y, box.MaxPoint.Z, 1.0f)) <= 0.0f)
            {
                return true;
            }

            if (Plane.Dot(plane, new Vector4(box.MaxPoint.X, box.MinPoint.Y, box.MaxPoint.Z, 1.0f)) <= 0.0f)
            {
                return true;
            }

            if (Plane.Dot(plane, new Vector4(box.MinPoint.X, box.MaxPoint.Y, box.MaxPoint.Z, 1.0f)) <= 0.0f)
            {
                return true;
            }

            if (Plane.Dot(plane, new Vector4(box.MaxPoint.X, box.MaxPoint.Y, box.MaxPoint.Z, 1.0f)) <= 0.0f)
            {
                return true;
            }

            return false;
        }
    }
}