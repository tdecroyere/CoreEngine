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
        private Entity? sceneEntity;
        private SceneComponent? sceneComponent;
        private bool isFirstRun = true;

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
            var changeMeshletView = this.inputsManager.InputsState.Keyboard.F2.Value == 0.0f && this.inputsManager.InputsState.Keyboard.F2.TransitionCount > 0;
            var changeOcclusionCulling = this.inputsManager.InputsState.Keyboard.F3.Value == 0.0f && this.inputsManager.InputsState.Keyboard.F3.TransitionCount > 0;

            var memoryChunks = GetMemoryChunks();

            var entityArray = GetEntityArray(memoryChunks.Span[0]);
            var cameraArray = GetComponentArray<CameraComponent>(memoryChunks.Span[0]);
            var playerArray = GetComponentArray<PlayerComponent>(memoryChunks.Span[0]);

            if (this.isFirstRun)
            {
                var sceneEntities = entityManager.GetEntitiesByComponentType<SceneComponent>();

                if (sceneEntities.Length > 0)
                {
                    sceneEntity = sceneEntities[0];
                    sceneComponent = entityManager.GetComponentData<SceneComponent>(sceneEntity.Value);
                }

                this.isFirstRun = false;
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

            if (changeMeshletView && sceneEntity.HasValue&& sceneComponent.HasValue)
            {
                var temp = sceneComponent.Value;
                temp.ShowMeshlets = (uint)(temp.ShowMeshlets == 0 ? 1 : 0);
                sceneComponent = temp;

                entityManager.SetComponentData(sceneEntity.Value, sceneComponent.Value);
            }

            if (changeOcclusionCulling && sceneEntity.HasValue&& sceneComponent.HasValue)
            {
                var temp = sceneComponent.Value;
                temp.IsOcclusionCullingEnabled = (uint)(temp.IsOcclusionCullingEnabled == 0 ? 1 : 0);
                sceneComponent = temp;

                entityManager.SetComponentData(sceneEntity.Value, sceneComponent.Value);
            }
        }
    }
}