namespace CoreEngine
{
    public static class BoundingIntersectUtils
    {
        public static bool Intersect(BoundingFrustum frustum, in BoundingBox box)
        {
            if (frustum == null)
            {
                throw new ArgumentNullException(nameof(frustum));
            }
            
            if (!Intersect(frustum.LeftPlane, in box))
            {
                return false;
            }

            if (!Intersect(frustum.RightPlane, in box))
            {
                return false;
            }

            if (!Intersect(frustum.TopPlane, in box))
            {
                return false;
            }

            if (!Intersect(frustum.BottomPlane, in box))
            {
                return false;
            }

            if (!Intersect(frustum.NearPlane, in box))
            {
                return false;
            }

            if (!Intersect(frustum.FarPlane, in box))
            {
                return false;
            }

            return true;
        }

        private static bool Intersect(Plane plane, in BoundingBox box)
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