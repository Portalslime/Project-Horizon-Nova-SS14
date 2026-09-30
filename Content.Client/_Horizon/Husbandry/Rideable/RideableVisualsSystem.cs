using System.Numerics;
using Content.Shared._Horizon.Husbandry.Rideable;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client._Horizon.Husbandry.Rideable;

/// <summary>
/// Draws the rider a bit higher than the animal they sit on, and in front of or behind it, depending on where the animal is facing.
/// </summary>
public sealed class RideableVisualsSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RiderComponent, ComponentShutdown>(OnRiderShutdown);
    }

    private void OnRiderShutdown(Entity<RiderComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.SetOffset((ent, sprite), Vector2.Zero);

        if (ent.Comp.OriginalDrawDepth is { } depth)
            _sprite.SetDrawDepth((ent, sprite), depth);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var rotation = _eye.CurrentEye.Rotation;
        var query = EntityQueryEnumerator<RiderComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var rider, out var sprite))
        {
            if (rider.Mount is not { } mount || !TryComp<RideableComponent>(mount, out var rideable))
                continue;

            var facing = (_transform.GetWorldRotation(mount) + rotation).GetCardinalDir();
            var offset = facing switch
            {
                Direction.North => rideable.NorthOffset,
                Direction.East => rideable.EastOffset,
                Direction.West => rideable.WestOffset,
                _ => rideable.SouthOffset,
            };

            // Avoid recalculating a matrix if we can help it.
            if (sprite.Offset != offset)
                _sprite.SetOffset((uid, sprite), offset);

            // The rider sits over the animal, except when it faces us: then its head and neck are in front of them.
            if (!TryComp<SpriteComponent>(mount, out var mountSprite))
                continue;

            var depth = facing == Direction.South ? mountSprite.DrawDepth - 1 : mountSprite.DrawDepth + 1;
            if (sprite.DrawDepth == depth)
                continue;

            rider.OriginalDrawDepth ??= sprite.DrawDepth;
            _sprite.SetDrawDepth((uid, sprite), depth);
        }
    }
}
