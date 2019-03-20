using System;
using System.Numerics;
using CoreEngine;
using CoreEngine.Graphics;

namespace CoreEngine.Tests.EcsTest
{
    public class DebugTriangleSystem : EntitySystem
    {
        private readonly GraphicsManager graphicsManager;

        public DebugTriangleSystem(GraphicsManager graphicsManager)
        {
            this.graphicsManager = graphicsManager;
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

                this.graphicsManager.DebugDrawTriangle(debugTriangle.Color1, debugTriangle.Color2, debugTriangle.Color3, transform.WorldMatrix);
            }
        }
    }
}