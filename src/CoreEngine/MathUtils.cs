using System;
using System.Numerics;

namespace CoreEngine
{
    public static class MathUtils
    {
        public static float DegreesToRad(float angle)
        {
            return angle * MathF.PI / 180.0f;
        }

        public static float RadToDegrees(float angle)
        {
            return angle * 180.0f / MathF.PI;
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
    
            return new Matrix4x4(row1.X, row1.Y, row1.Z, row1.W,
                                 row2.X, row2.Y, row2.Z, row2.W,
                                 row3.X, row3.Y, row3.Z, row3.W,
                                 row4.X, row4.Y, row4.Z, row4.W);
        }

        public static Matrix4x4 CreatePerspectiveFieldOfViewMatrix(float fieldOfViewY, float aspectRatio, float nearPlaneDistance)
        {
            var height = 1.0f / MathF.Tan(fieldOfViewY / 2.0f);

            var row1 = new Vector4(height / aspectRatio, 0, 0, 0);
            var row2 = new Vector4(0, height, 0, 0);
            var row3 = new Vector4(0, 0, 0, 1);
            var row4 = new Vector4(0, 0, nearPlaneDistance, 0);

            return new Matrix4x4(row1.X, row1.Y, row1.Z, row1.W,
                                 row2.X, row2.Y, row2.Z, row2.W,
                                 row3.X, row3.Y, row3.Z, row3.W,
                                 row4.X, row4.Y, row4.Z, row4.W);
        }

        public static Matrix4x4 CreateOrthographicMatrix(float width, float height, float nearPlaneDistance, float farPlaneDistance)
		{
            var result = new Matrix4x4();

			if(width != 0)
			{
                result.M11 = 2.0f / width;
			}

			if(height != 0)
			{
                result.M22 = 2.0f / height;
			}

            result.M33 = 1.0f - 1.0f / (farPlaneDistance - nearPlaneDistance);
            result.M43 = 1.0f - nearPlaneDistance / (nearPlaneDistance - farPlaneDistance);
            result.M44 = 1.0f;

			return result;
		}

        public static Matrix4x4 CreateOrthographicMatrixOffCenter(float minPlaneX, float maxPlaneX, float minPlaneY, float maxPlaneY, float minPlaneZ, float maxPlaneZ)
        {
            var result = new Matrix4x4();

            result.M11 = 2.0f / (maxPlaneX - minPlaneX);
            result.M22 = -2.0f / (maxPlaneY - minPlaneY);
            result.M33 = 1.0f / (maxPlaneZ - minPlaneZ);

            result.M41 = (minPlaneX + maxPlaneX) / (minPlaneX - maxPlaneX);
            result.M42 = -(minPlaneY + maxPlaneY) / (minPlaneY - maxPlaneY);
            result.M43 = minPlaneZ / (minPlaneZ - maxPlaneZ);
            result.M44 = 1.0f;

            return result;
        }

        public static Matrix4x4 CreateScaleTranslation(Vector3 scale, Vector3 translationVector)
		{
			return new Matrix4x4(scale.X, 0, 0, 0,
                                 0, scale.Y, 0, 0,
                                 0, 0, scale.Z, 0,
                                 translationVector.X, translationVector.Y, translationVector.Z, 1);
		}

        public static Matrix4x4 CreateTranslation(Vector3 translationVector)
		{
			return new Matrix4x4(1, 0, 0, 0,
                                 0, 1, 0, 0,
                                 0, 0, 1, 0,
                                 translationVector.X, translationVector.Y, translationVector.Z, 1);
		}

        public static uint ComputeTextureMipLevels(uint width, uint height)
        {
            var result = 0u;

            while (width > 1 || height > 1)
            {
                result++;
                width /= 2;
                height /= 2;
            }

            return result;
        }
    }
}