using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Nutrition.EntitySystems
{
    [UsedImplicitly]
    public sealed class CreamPiedSystem : SharedCreamPieSystem
    {
        private const string CreamLayer = "clownedon";

        [Dependency] private readonly SpriteSystem _sprite = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CreamPiedComponent, AppearanceChangeEvent>(OnAppearanceChange);
        }

        /// <summary>
        /// Tints the smear layer with the colour of whatever hit the entity.
        /// </summary>
        private void OnAppearanceChange(Entity<CreamPiedComponent> ent, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            if (!_sprite.LayerMapTryGet((ent, args.Sprite), CreamLayer, out var layer, false))
                return;

            if (!_appearanceSystem.TryGetData<Color>(ent, CreamPiedVisuals.Color, out var color, args.Component))
                color = Color.White;

            _sprite.LayerSetColor((ent, args.Sprite), layer, color);
        }
    }
}
