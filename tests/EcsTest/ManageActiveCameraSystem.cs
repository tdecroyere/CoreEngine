using System;
using System.Numerics;
using CoreEngine.Components;
using CoreEngine.Diagnostics;
using CoreEngine.Rendering.Components;
using CoreEngine.Inputs;

namespace CoreEngine.Tests.EcsTest
{
    public class ManageActiveCameraSystem : EntitySystem
    {
        private readonly InputsManager inputsManager;

        public ManageActiveCameraSystem(InputsManager inputsManager)
        {
            this.inputsManager = inputsManager;
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Manage Camera System");

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
            // var activeCameraIndex = -1;

            for (var i = 0; i < entityArray.Length; i++)
            {
                ref var cameraComponent = ref cameraArray[i];

                // if(sceneComponent.HasValue)
                // {
                //     if (changeCamera && sceneComponent.Value.ActiveCamera == entityArray[i])
                //     {
                //         activeCameraIndex = i;
                //     }

                //     if (changeCamera && activeCameraIndex != -1 && activeCameraIndex + 1 == i)
                //     {
                //         var temp = sceneComponent.Value;
                //         temp.ActiveCamera = entityArray[i];
                //         sceneComponent = temp;
                //     }
                // }
                // if (this.inputsManager.IsLeftMouseDown())
                // {
                //     this.inputsManager.SendVibrationCommand(1, 1.0f, 0.0f, 0.0f, 0.0f, 1);
                //     var mouseVector = this.inputsManager.GetMouseDelta();
                //     playerArray[i].RotationVector = new Vector3(mouseVector.X, mouseVector.Y, 0.0f);
                // }

                //Logger.WriteMessage($"InputVector: {playerArray[i].InputVector}");
            }

            if (sceneComponent.HasValue && changeCamera && entityArray.Length > 1)
            {
                var temp = sceneComponent.Value;

                if (temp.DebugCamera == null)
                {
                    temp.DebugCamera = entityArray[1];
                }

                else
                {
                    temp.DebugCamera = null;
                }

                sceneComponent = temp;
            }

            // else if (sceneComponent.HasValue && ((activeCameraIndex == -1 && changeCamera) || activeCameraIndex >= (entityArray.Length - 1)))
            // {
            //     var temp = sceneComponent.Value;
            //     temp.ActiveCamera = entityArray[0];
            //     sceneComponent = temp;
            // }

            if (sceneEntity.HasValue && sceneComponent.HasValue)
            {
                entityManager.SetComponentData<SceneComponent>(sceneEntity.Value, sceneComponent.Value);
            }
        }
    }
}