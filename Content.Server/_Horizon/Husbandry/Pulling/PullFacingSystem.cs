using Content.Shared._Horizon.Husbandry.Pulling;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Components;

namespace Content.Server._Horizon.Husbandry.Pulling;

/// <summary>
/// Turns an animal to face whoever drags it, see <see cref="PullFacingComponent"/>. Server only: the rotation of the
/// animal is networked, the client interpolates it.
/// </summary>
public sealed class PullFacingSystem : EntitySystem
{
    [Dependency] private readonly RotateToFaceSystem _rotateToFace = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PullFacingComponent, PullableComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var facing, out var pullable, out var xform))
        {
            if (pullable.Puller is not { } puller)
                continue;

            var diff = _transform.GetWorldPosition(puller) - _transform.GetWorldPosition(xform);
            if (diff.LengthSquared() < facing.MinDistance * facing.MinDistance)
                continue;

            // The sprite is drawn by the rotation, so this is what makes it look at the one who leads it.
            _rotateToFace.TryRotateTo(uid, Angle.FromWorldVec(diff), frameTime, Angle.FromDegrees(5), facing.RotationSpeed, xform);
        }
    }
}
