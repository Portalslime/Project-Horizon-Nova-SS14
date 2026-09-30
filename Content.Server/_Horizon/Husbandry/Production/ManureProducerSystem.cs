using Content.Server.Stack;
using Content.Shared._Horizon.Husbandry.Core;
using Content.Shared._Horizon.Husbandry.Feeding;
using Content.Shared._Horizon.Husbandry.Production;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Horizon.Husbandry.Production;

/// <summary>
/// Leaves manure for the nutrition an animal ate, as many pieces as the stack holds at most and then the next stack.
/// </summary>
public sealed class ManureProducerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StackSystem _stack = default!;

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

        _stack.SpawnMultiple(ent.Comp.Product, units, Transform(ent).Coordinates);
        _audio.PlayPvs(ent.Comp.Sound, ent);
    }
}
