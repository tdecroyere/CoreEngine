using System;
using System.Numerics;
using CoreEngine;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class UpdateActiveCameraSystem : EntitySystem
    {
        private readonly GraphicsManager graphicsManager;

        public UpdateActiveCameraSystem(GraphicsManager graphicsManager)
        {
            this.graphicsManager = graphicsManager;
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Update Active Camera System");

            definition.Parameters.Add(new EntitySystemParameter(typeof(TransformComponent)));
            definition.Parameters.Add(new EntitySystemParameter(typeof(CameraComponent)));

            return definition;
        }

        public override void Process(float deltaTime)
        {
            var entityArray = this.GetEntityArray();
            var transformArray = this.GetComponentDataArray<TransformComponent>();
            var cameraArray = this.GetComponentDataArray<CameraComponent>();

            for (var i = 0; i < entityArray.Length; i++)
            {
                var entity = entityArray[i];

                if (cameraArray[i].EyePosition != Vector3.Zero || cameraArray[i].LookAtPosition != Vector3.Zero)
                {
                    transformArray[i].Position = cameraArray[i].EyePosition;

                    var cameraViewMatrix = MathUtils.CreateLookAtMatrix(cameraArray[i].EyePosition, cameraArray[i].LookAtPosition, new Vector3(0, 1, 0));
                    
                    var cameraRotationX = (float) Math.Asin( -cameraViewMatrix.M23 );
                    var cameraRotationY = (float) Math.Atan2( -cameraViewMatrix.M13, cameraViewMatrix.M33 );
                    
                    transformArray[i].RotationX = MathUtils.RadToDegrees(cameraRotationX);
                    transformArray[i].RotationY = MathUtils.RadToDegrees(cameraRotationY);

                    Logger.WriteMessage($"Camera Setup: {transformArray[i].RotationX} - {transformArray[i].RotationY}");

                    // TODO: Move that to an util method
                    var scale = Matrix4x4.CreateScale(transformArray[i].Scale);
                    var rotationX = MathUtils.DegreesToRad(transformArray[i].RotationX);
                    var rotationY = MathUtils.DegreesToRad(transformArray[i].RotationY);
                    var rotationZ = MathUtils.DegreesToRad(transformArray[i].RotationZ);
                    var translation = Matrix4x4.CreateTranslation(transformArray[i].Position);

                    var rotationQuaternion = Quaternion.CreateFromYawPitchRoll(rotationY, rotationX, rotationZ);

                    transformArray[i].RotationQuaternion = rotationQuaternion;
                    transformArray[i].WorldMatrix = Matrix4x4.Transform(scale, transformArray[i].RotationQuaternion) * translation;

                    cameraArray[i].EyePosition = Vector3.Zero;
                    cameraArray[i].LookAtPosition = Vector3.Zero;
                }

                var transform = transformArray[i];
                var camera = cameraArray[i];

                var cameraPosition = Vector3.Transform(Vector3.Zero, transform.WorldMatrix);
                var target = Vector3.Transform(new Vector3(0, 0, 1), transform.RotationQuaternion) + cameraPosition;

                var viewMatrix = MathUtils.CreateLookAtMatrix(cameraPosition, target, new Vector3(0, 1, 0));
                graphicsManager.UpdateCamera(viewMatrix);
            }
        }
    }
}