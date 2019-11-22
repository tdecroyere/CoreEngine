using System;
using System.Numerics;
using CoreEngine.Collections;
using CoreEngine.Components;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics.Components;

namespace CoreEngine.Graphics.EntitySystems
{
    public class UpdateCameraSystem : EntitySystem
    {
        private readonly GraphicsManager graphicsManager;
        private readonly GraphicsSceneRenderer sceneRenderer;

        public UpdateCameraSystem(GraphicsManager graphicsManager, GraphicsSceneRenderer sceneRenderer)
        {
            this.graphicsManager = graphicsManager;
            this.sceneRenderer = sceneRenderer;
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Update Camera System");

            definition.Parameters.Add(new EntitySystemParameter(typeof(TransformComponent), true));
            definition.Parameters.Add(new EntitySystemParameter(typeof(CameraComponent)));

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            if (entityManager == null)
            {
                return;
            }
            
            var entityArray = this.GetEntityArray();
            var transformArray = this.GetComponentDataArray<TransformComponent>();
            var cameraArray = this.GetComponentDataArray<CameraComponent>();

            Entity? sceneEntity = null;
            SceneComponent? sceneComponent = null;

            var sceneEntities = entityManager.GetEntitiesByComponentType<SceneComponent>();

            if (sceneEntities.Length > 0)
            {
                sceneEntity = sceneEntities[0];
                sceneComponent = entityManager.GetComponentData<SceneComponent>(sceneEntity.Value);
            }

            var renderSize = this.graphicsManager.GetRenderSize();
            var renderWidth = renderSize.X;
            var renderHeight = renderSize.Y;

            var projectionMatrix = MathUtils.CreatePerspectiveFieldOfViewMatrix(MathUtils.DegreesToRad(54.43f), renderWidth / renderHeight, 10.0f, 100000.0f);
            // var projectionMatrix = MathUtils.CreatePerspectiveFieldOfViewMatrix(MathUtils.DegreesToRad(39.375f), renderWidth / renderHeight, 10.0f, 100000.0f);


            for (var i = 0; i < entityArray.Length; i++)
            {
                var entity = entityArray[i];
                ref var transformComponent = ref transformArray[i];
                ref var cameraComponent = ref cameraArray[i];

                if (cameraArray[i].EyePosition != Vector3.Zero || cameraArray[i].LookAtPosition != Vector3.Zero)
                {
                    SetupCamera(ref cameraComponent, ref transformComponent);
                }

                var cameraPosition = transformComponent.Position;
                var target = Vector3.Transform(new Vector3(0, 0, 1), transformComponent.RotationQuaternion) + cameraPosition;

                var viewMatrix = MathUtils.CreateLookAtMatrix(cameraPosition, target, new Vector3(0, 1, 0));
                
                if (!sceneRenderer.CurrentScene.MeshInstances.Contains(cameraComponent.Camera))
                {
                    var camera = new Camera(viewMatrix, projectionMatrix);
                    cameraComponent.Camera = sceneRenderer.CurrentScene.Cameras.Add(camera);
                }

                else
                {
                    var camera = sceneRenderer.CurrentScene.Cameras[cameraComponent.Camera];

                    camera.ViewMatrix = viewMatrix;
                    camera.ProjectionMatrix = projectionMatrix;
                }
            }
        }

        private static void SetupCamera(ref CameraComponent camera, ref TransformComponent transform)
        {
            transform.Position = camera.EyePosition;

            var cameraViewMatrix = MathUtils.CreateLookAtMatrix(camera.EyePosition, camera.LookAtPosition, new Vector3(0, 1, 0));

            var cameraRotationX = (float)Math.Asin(-cameraViewMatrix.M23);
            var cameraRotationY = (float)Math.Atan2(-cameraViewMatrix.M13, cameraViewMatrix.M33);

            transform.RotationX = MathUtils.RadToDegrees(cameraRotationX);
            transform.RotationY = MathUtils.RadToDegrees(cameraRotationY);

            Logger.WriteMessage($"Camera Setup: {transform.RotationX} - {transform.RotationY}");

            // TODO: Move that to an util method
            var scale = Matrix4x4.CreateScale(transform.Scale);
            var rotationX = MathUtils.DegreesToRad(transform.RotationX);
            var rotationY = MathUtils.DegreesToRad(transform.RotationY);
            var rotationZ = MathUtils.DegreesToRad(transform.RotationZ);
            var translation = Matrix4x4.CreateTranslation(transform.Position);

            var rotationQuaternion = Quaternion.CreateFromYawPitchRoll(rotationY, rotationX, rotationZ);

            transform.RotationQuaternion = rotationQuaternion;
            transform.WorldMatrix = Matrix4x4.Transform(scale, transform.RotationQuaternion) * translation;

            camera.EyePosition = Vector3.Zero;
            camera.LookAtPosition = Vector3.Zero;
        }
    }
}