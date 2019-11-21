using System;
using System.Numerics;
using CoreEngine.Components;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics.Components;
using CoreEngine.Inputs;

namespace CoreEngine.Tests.EcsTest
{
    public class InputsUpdateSystem : EntitySystem
    {
        private readonly InputsManager inputsManager;

        public InputsUpdateSystem(InputsManager inputsManager)
        {
            this.inputsManager = inputsManager;
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Inputs Update System");

            definition.Parameters.Add(new EntitySystemParameter(typeof(PlayerComponent)));
            definition.Parameters.Add(new EntitySystemParameter(typeof(CameraComponent)));

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            var entityArray = this.GetEntityArray();
            var playerArray = this.GetComponentDataArray<PlayerComponent>();
            var cameraArray = this.GetComponentDataArray<CameraComponent>();

            Entity? sceneEntity = null;
            SceneComponent? sceneComponent = null;

            var sceneEntities = entityManager.GetEntitiesByComponentType<SceneComponent>();

            if (sceneEntities.Length > 0)
            {
                sceneEntity = sceneEntities[0];
                sceneComponent = entityManager.GetComponentData<SceneComponent>(sceneEntity.Value);
            }

            var changeCamera = (this.inputsManager.inputsState.Keyboard.Space.Value == 0.0f && this.inputsManager.inputsState.Keyboard.Space.TransitionCount > 0);
            var activeCameraIndex = -1;

            for (var i = 0; i < entityArray.Length; i++)
            {
                ref var playerComponent = ref playerArray[i];
                ref var cameraComponent = ref cameraArray[i];

                playerComponent.MovementVector = new Vector3(this.inputsManager.GetMovementVector(), 0.0f);
                playerComponent.RotationVector = new Vector3(this.inputsManager.GetRotationVector(), 0.0f);

                if(sceneComponent.HasValue)
                {
                    if (changeCamera && sceneComponent.Value.ActiveCamera == entityArray[i])
                    {
                        activeCameraIndex = i;
                    }

                    if (changeCamera && activeCameraIndex != -1 && activeCameraIndex + 1 == i)
                    {
                        var temp = sceneComponent.Value;
                        temp.ActiveCamera = entityArray[i];
                        sceneComponent = temp;
                    }
                }
                // if (this.inputsManager.IsLeftMouseDown())
                // {
                //     this.inputsManager.SendVibrationCommand(1, 1.0f, 0.0f, 0.0f, 0.0f, 1);
                //     var mouseVector = this.inputsManager.GetMouseDelta();
                //     playerArray[i].RotationVector = new Vector3(mouseVector.X, mouseVector.Y, 0.0f);
                // }

                //Logger.WriteMessage($"InputVector: {playerArray[i].InputVector}");
            }

            if (sceneComponent.HasValue && (activeCameraIndex == -1 && changeCamera && entityArray.Length > 1))
            {
                var temp = sceneComponent.Value;
                temp.ActiveCamera = entityArray[1];
                sceneComponent = temp;
            }

            else if (sceneComponent.HasValue && ((activeCameraIndex == -1 && changeCamera) || activeCameraIndex >= (entityArray.Length - 1)))
            {
                var temp = sceneComponent.Value;
                temp.ActiveCamera = entityArray[0];
                sceneComponent = temp;
            }

            if (sceneEntity.HasValue && sceneComponent.HasValue)
            {
                entityManager.SetComponentData<SceneComponent>(sceneEntity.Value, sceneComponent.Value);
            }
        }
    }
}