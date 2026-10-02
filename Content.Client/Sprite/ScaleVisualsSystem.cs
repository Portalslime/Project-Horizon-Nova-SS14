using System.Numerics;
using Content.Shared.Humanoid;
using Content.Shared.Sprite;
using Robust.Client.GameObjects;

namespace Content.Client.Sprite;

public sealed class ScaleVisualsSystem : SharedScaleVisualsSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScaleVisualsComponent, AppearanceChangeEvent>(OnChangeData);
    }

    private void OnChangeData(Entity<ScaleVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!args.AppearanceData.TryGetValue(ScaleVisuals.Scale, out var scale) ||
            args.Sprite == null) return;

        // save the original scale and offset
        ent.Comp.OriginalScale ??= args.Sprite.Scale;
        ent.Comp.OriginalOffset ??= args.Sprite.Offset;

        var vecScale = (Vector2)scale;
        _sprite.SetScale((ent.Owner, args.Sprite), vecScale);

        // Horizon: у humanoid'ов при масштабировании удерживаем ноги на тайле,
        // иначе спрайт "всплывает"/уходит под пол.
        if (HasComp<HumanoidAppearanceComponent>(ent.Owner))
        {
            var baseOffset = ent.Comp.OriginalOffset ?? Vector2.Zero;
            _sprite.SetOffset((ent.Owner, args.Sprite), baseOffset + new Vector2(0f, (vecScale.Y - 1f) * 0.5f));
        }
    }

    // revert to the original scale
    protected override void ResetScale(Entity<ScaleVisualsComponent> ent)
    {
        base.ResetScale(ent);

        if (ent.Comp.OriginalScale != null)
            _sprite.SetScale(ent.Owner, ent.Comp.OriginalScale.Value);

        if (ent.Comp.OriginalOffset != null)
            _sprite.SetOffset(ent.Owner, ent.Comp.OriginalOffset.Value);
    }
}
