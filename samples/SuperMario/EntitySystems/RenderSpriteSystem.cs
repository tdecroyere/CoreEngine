using System;
using System.Numerics;
using CoreEngine.Collections;
using CoreEngine.Components;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.Rendering;
using CoreEngine.Rendering.Components;
using CoreEngine.Resources;
using CoreEngine.Samples.SuperMario.Components;

namespace CoreEngine.Samples.SuperMario.EntitySystems
{
    public class RenderSpriteSystem : EntitySystem
    {
        private readonly Graphics2DRenderer renderer;
        private readonly ResourcesManager resourcesManager;
        private Texture spriteSheet;

        public RenderSpriteSystem(Graphics2DRenderer renderer, ResourcesManager resourceManager)
        {
            this.renderer = renderer;
            this.resourcesManager = resourceManager;
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Render Sprite System");

            definition.Parameters.Add(new EntitySystemParameter<TransformComponent>());
            definition.Parameters.Add(new EntitySystemParameter<SpriteComponent>());

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            if (entityManager == null)
            {
                throw new ArgumentNullException(nameof(entityManager));
            }

            if (this.spriteSheet == null)
            {
                this.spriteSheet = resourcesManager.LoadResourceAsync<Texture>("/Textures/Mario.texture");
            }

            var entityArray = this.GetEntityArray();
            var transformArray = this.GetComponentDataArray<TransformComponent>();
            var spriteArray = this.GetComponentDataArray<SpriteComponent>();

            for (var i = 0; i < entityArray.Length; i++)
            {
                var entity = entityArray[i];
                ref var transformComponent = ref transformArray[i];
                ref var SpriteComponent = ref spriteArray[i];

                var position = new Vector2(transformComponent.Position.X, (int)transformComponent.Position.Y);
                var size = new Vector2(130, 160);
                var textureMinPoint = new Vector2(178, 32);
                var textureMaxPoint = textureMinPoint + new Vector2(13, 16);
                var textureSize = new Vector2(this.spriteSheet.Width, this.spriteSheet.Height);

                // TODO: When using a render pipeline put a camera aligned to the original pixels
                // See: https://stackoverflow.com/questions/35785291/getting-giggly-effect-when-slowly-moving-a-sprite/35829604

                this.renderer.DrawRectangleSurface(position, position + size, this.spriteSheet, textureMinPoint / textureSize, textureMaxPoint / textureSize, false);
            }
        }
    }
}