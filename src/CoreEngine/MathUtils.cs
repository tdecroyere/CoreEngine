using System;
using System.Numerics;

namespace CoreEngine
{
    public static class MathUtils
    {
        public static float DegreesToRad(float angle)
        {
            return (angle * MathF.PI) / 180.0f;
        }
        
        public static Matrix4x4 CreateLookAtMatrix(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
        {
            var zAxis = Vector3.Normalize(cameraTarget - cameraPosition);
            var xAxis = Vector3.Normalize(Vector3.Cross(cameraUpVector, zAxis));
            var yAxis = Vector3.Normalize(Vector3.Cross(zAxis, xAxis));

            var row1 = new Vector4(xAxis.X, yAxis.X, zAxis.X, 0);
            var row2 = new Vector4(xAxis.Y, yAxis.Y, zAxis.Y, 0);
            var row3 = new Vector4(xAxis.Z, yAxis.Z, zAxis.Z, 0);
            var row4 = new Vector4(-Vector3.Dot(xAxis, cameraPosition), -Vector3.Dot(yAxis, cameraPosition), -Vector3.Dot(zAxis, cameraPosition), 1);
    
            return new Matrix4x4(row1.X, row2.X, row3.X, row4.X,
                                 row1.Y, row2.Y, row3.Y, row4.Y,
                                 row1.Z, row2.Z, row3.Z, row4.Z,
                                 row1.W, row2.W, row3.W, row4.W);
        }

        public static Matrix4x4 CreatePerspectiveFieldOfViewMatrix(float fieldOfViewY, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
        {
            var height = 1.0f / MathF.Tan(fieldOfViewY / 2.0f);

            var row1 = new Vector4(height / aspectRatio, 0, 0, 0);
            var row2 = new Vector4(0, height, 0, 0);
            var row3 = new Vector4(0, 0, (farPlaneDistance / (farPlaneDistance - nearPlaneDistance)), 1);
            var row4 = new Vector4(0, 0, -nearPlaneDistance * farPlaneDistance / (farPlaneDistance - nearPlaneDistance), 0);

            return new Matrix4x4(row1.X, row2.X, row3.X, row4.X,
                                 row1.Y, row2.Y, row3.Y, row4.Y,
                                 row1.Z, row2.Z, row3.Z, row4.Z,
                                 row1.W, row2.W, row3.W, row4.W);
        }
    }
}