using System;
using System.Numerics;
using CoreEngine;
using CoreEngine.Graphics;
using CoreEngine.Resources;

namespace CoreEngine.Tests.EcsTest
{
    public class DebugTriangleSystem : EntitySystem
    {
        private readonly GraphicsManager graphicsManager;
        private Mesh testMesh;

        public DebugTriangleSystem(GraphicsManager graphicsManager, ResourcesManager resourcesManager)
        {
            this.graphicsManager = graphicsManager;
            this.testMesh = resourcesManager.LoadResourceAsync<Mesh>("/teapot.mesh");
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Debug Triangle System");

            definition.Parameters.Add(new EntitySystemParameter(typeof(TransformComponent)));
            definition.Parameters.Add(new EntitySystemParameter(typeof(DebugTriangleComponent)));

            return definition;
        }

        public override void Process(float deltaTime)
        {
            var entityArray = this.GetEntityArray();
            var transformArray = this.GetComponentDataArray<TransformComponent>();
            var debugTriangleArray = this.GetComponentDataArray<DebugTriangleComponent>();

            for (var i = 0; i < entityArray.Length; i++)
            {
                var transform = transformArray[i];
                var debugTriangle = debugTriangleArray[i];

                if (this.testMesh != null)
                {
                    // TODO: Move that to a component systerm
                    graphicsManager.DrawMesh(this.testMesh, transform.WorldMatrix);
                }
                //this.graphicsManager.DebugDrawTriangle(debugTriangle.Color1, debugTriangle.Color2, debugTriangle.Color3, transform.WorldMatrix);
            }
        }
    }
}