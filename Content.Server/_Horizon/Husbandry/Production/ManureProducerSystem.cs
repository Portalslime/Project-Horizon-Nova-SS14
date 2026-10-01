using Content.Server.Stack;
using Content.Shared._Horizon.Husbandry.Core;
using Content.Shared._Horizon.Husbandry.Feeding;
using Content.Shared._Horizon.Husbandry.Production;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Server._Horizon.Husbandry.Production;

/// <summary>
/// Leaves manure for the nutrition an animal ate, as many pieces as the stack holds at most and then the next stack.
/// </summary>
public sealed class ManureProducerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ManureProducerComponent, AnimalAteEvent>(OnAte);
    }

    private void OnAte(Entity<ManureProducerComponent> ent, ref AnimalAteEvent args)
    {
        var units = ProductionRules.Accumulate(ref ent.Comp.Accumulated, args.Nutrition * ent.Comp.UnitsPerNutrition, 1f);
        if (units <= 0)
            return;

        _stack.SpawnMultiple(ent.Comp.Product, units, GetDropCoordinates(ent));
        _audio.PlayPvs(ent.Comp.Sound, ent);
    }

    /// <summary>
    /// Where the product is left: behind the animal by <see cref="ManureProducerComponent.DropOffset"/>.
    /// </summary>
    private EntityCoordinates GetDropCoordinates(Entity<ManureProducerComponent> ent)
    {
        var xform = Transform(ent);
        if (ent.Comp.DropOffset == 0f)
            return xform.Coordinates;

        // The world vector of a rotation is where the entity looks, so the animal's back is the other way.
        var behind = -_transform.GetWorldRotation(xform).ToWorldVec() * ent.Comp.DropOffset;
        return _transform.ToCoordinates(_transform.GetMapCoordinates(xform).Offset(behind));
    }
}
