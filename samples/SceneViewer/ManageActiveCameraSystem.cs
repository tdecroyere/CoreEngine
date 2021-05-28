using System;
using System.Numerics;
using CoreEngine.Components;
using CoreEngine.Diagnostics;
using CoreEngine.Rendering.Components;
using CoreEngine.Inputs;

namespace CoreEngine.Samples.SceneViewer
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

            definition.Parameters.Add(new EntitySystemParameter<PlayerComponent>());
            definition.Parameters.Add(new EntitySystemParameter<CameraComponent>());

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            if (entityManager == null)
            {
                return;
            }
        
            var changeCamera = this.inputsManager.InputsState.Keyboard.Space.Value == 0.0f && this.inputsManager.InputsState.Keyboard.Space.TransitionCount > 0;
            var changePlayer = this.inputsManager.InputsState.Keyboard.F1.Value == 0.0f && this.inputsManager.InputsState.Keyboard.F1.TransitionCount > 0;

            var entityArray = this.GetEntityArray();
            var cameraArray = this.GetComponentDataArray<CameraComponent>();
            var playerArray = this.GetComponentDataArray<PlayerComponent>();

            Entity? sceneEntity = null;
            SceneComponent? sceneComponent = null;

            var sceneEntities = entityManager.GetEntitiesByComponentType<SceneComponent>();

            if (sceneEntities.Length > 0)
            {
                sceneEntity = sceneEntities[0];
                sceneComponent = entityManager.GetComponentData<SceneComponent>(sceneEntity.Value);
            }

            if (changeCamera)
            {
                if (sceneComponent.HasValue)
                {
                    // var activeCameraIndex = -1;

                    // for (var i = 0; i < entityArray.Length; i++)
                    // {
                    //     ref var cameraComponent = ref cameraArray[i];

                    //     if (sceneComponent.Value.ActiveCamera == entityArray[i])
                    //     {
                    //         activeCameraIndex = i;
                    //         break;
                    //     }

                    //     // if (this.inputsManager.IsLeftMouseDown())
                    //     // {
                    //     //     this.inputsManager.SendVibrationCommand(1, 1.0f, 0.0f, 0.0f, 0.0f, 1);
                    //     //     var mouseVector = this.inputsManager.GetMouseDelta();
                    //     //     playerArray[i].RotationVector = new Vector3(mouseVector.X, mouseVector.Y, 0.0f);
                    //     // }

                    //     //Logger.WriteMessage($"InputVector: {playerArray[i].InputVector}");
                    // }

                    // if (activeCameraIndex != -1)
                    // {
                    //     activeCameraIndex = (activeCameraIndex + 1) % entityArray.Length;
                    //     var temp = sceneComponent.Value;
                    //     temp.ActiveCamera = entityArray[activeCameraIndex];
                    //     sceneComponent = temp;
                    // }

                    if (entityArray.Length > 1)
                    {
                        var temp = sceneComponent.Value;

                        if (temp.DebugCamera == null)
                        {
                            temp.DebugCamera = entityArray[1];
                        }

                        else
                        {
                            temp.DebugCamera = null;

                            ref var playerComponent = ref playerArray[1];
                            playerComponent.IsActive = false;
                            entityManager.SetComponentData(entityArray[1], playerComponent);

                            playerComponent = ref playerArray[0];
                            playerComponent.IsActive = true;
                            entityManager.SetComponentData(entityArray[0], playerComponent);
                        }

                        sceneComponent = temp;
                    }

                    // else if (sceneComponent.HasValue && ((activeCameraIndex == -1 && changeCamera) || activeCameraIndex >= (entityArray.Length - 1)))
                    // {
                    //     var temp = sceneComponent.Value;
                    //     temp.ActiveCamera = entityArray[0];
                    //     sceneComponent = temp;
                    // }

                    if (sceneEntity.HasValue)
                    {
                        entityManager.SetComponentData(sceneEntity.Value, sceneComponent.Value);
                    }
                }
            }

            if (changePlayer && sceneComponent.HasValue && entityArray.Length > 1)
            {
                var temp = sceneComponent.Value;

                if (temp.DebugCamera != null)
                {
                    ref var playerComponent = ref playerArray[1];
                    playerComponent.IsActive = !playerComponent.IsActive;
                    entityManager.SetComponentData(entityArray[1], playerComponent);

                    playerComponent = ref playerArray[0];
                    playerComponent.IsActive = !playerComponent.IsActive;
                    entityManager.SetComponentData(entityArray[0], playerComponent);
                }
            }
        }
    }
}