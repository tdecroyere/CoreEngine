using System;
using System.Numerics;
using CoreEngine;
using CoreEngine.Graphics;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class UpdateActiveCameraSystem : EntitySystem
    {
        private readonly GraphicsManager graphicsManager;
        private readonly Vector3 cameraUpVector = new Vector3(0, 1, 0);

        public UpdateActiveCameraSystem(GraphicsManager graphicsManager)
        {
            this.graphicsManager = graphicsManager;
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Update Active Camera System");

            definition.Parameters.Add(new EntitySystemParameter(typeof(TransformComponent), true));
            definition.Parameters.Add(new EntitySystemParameter(typeof(CameraComponent), true));

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
                var transform = transformArray[i];
                var camera = cameraArray[i];

                var cameraPosition = Vector3.Transform(Vector3.Zero, transform.WorldMatrix);
                var target = Vector3.Transform(new Vector3(0, 0, 20), transform.RotationQuaternion) + cameraPosition;

                var viewMatrix = MathUtils.CreateLookAtMatrix(cameraPosition, target, new Vector3(0, 1, 0));
                graphicsManager.UpdateCamera(viewMatrix);
            }
        }
    }
}