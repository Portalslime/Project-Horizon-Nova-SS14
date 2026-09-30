using Content.Shared._Horizon.Husbandry.Core;
using Content.Shared._Horizon.Husbandry.Feeding;
using Content.Shared._Horizon.Husbandry.Production;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Horizon.Husbandry.Production;

/// <summary>
/// Drops manure for the nutrition an animal ate.
/// </summary>
public sealed class ManureProducerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ManureProducerComponent, AnimalAteEvent>(OnAte);
    }

    private void OnAte(Entity<ManureProducerComponent> ent, ref AnimalAteEvent args)
    {
        var drops = ProductionRules.Accumulate(ref ent.Comp.Accumulated, args.Nutrition, ent.Comp.NutritionPerDrop);
        if (drops <= 0)
            return;

        var coordinates = Transform(ent).Coordinates;
        for (var i = 0; i < drops; i++)
        {
            Spawn(ent.Comp.Product, coordinates);
        }

        _audio.PlayPvs(ent.Comp.Sound, ent);
    }
}
