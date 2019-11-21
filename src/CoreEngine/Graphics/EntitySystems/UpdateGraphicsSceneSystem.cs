using System;
using System.Numerics;
using CoreEngine.Components;
using CoreEngine.Graphics.Components;
using CoreEngine.Resources;

namespace CoreEngine.Graphics.EntitySystems
{
    public class UpdateGraphicsSceneSystem : EntitySystem
    {
        private readonly GraphicsSceneRenderer sceneRenderer;

        public UpdateGraphicsSceneSystem(GraphicsSceneRenderer sceneRenderer)
        {
            this.sceneRenderer = sceneRenderer;
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Update Graphics Scene System");

            definition.Parameters.Add(new EntitySystemParameter(typeof(SceneComponent)));

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            var sceneArray = this.GetComponentDataArray<SceneComponent>();
           
            sceneRenderer.UpdateScene(null);

            for (var i = 0; i < sceneArray.Length; i++)
            {
                var scene = sceneArray[i];
                sceneRenderer.UpdateScene(scene.ActiveCamera);
            }
        }
    }
}