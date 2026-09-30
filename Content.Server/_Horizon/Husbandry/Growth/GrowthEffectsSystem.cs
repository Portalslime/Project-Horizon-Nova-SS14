using Content.Server._NF.Cargo.Components;
using Content.Shared._Horizon.Husbandry.Core;
using Content.Shared._Horizon.Husbandry.Growth;
using Content.Shared._Horizon.Husbandry.Needs;
using Content.Shared._Horizon.Husbandry.Production;
using Content.Shared._Horizon.Husbandry.Sex;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Sprite;
using Content.Shared.Storage;

namespace Content.Server._Horizon.Husbandry.Growth;

/// <summary>
/// Applies what a growth stage means to an animal: its name, size, health, price, how much meat it gives,
/// when it gets hungry, how much it eats and how much manure it leaves.
/// </summary>
public sealed class GrowthEffectsSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly SharedScaleVisualsSystem _scale = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrowthComponent, GrowthStageChangedEvent>(OnStageChanged);
    }

    private void OnStageChanged(Entity<GrowthComponent> ent, ref GrowthStageChangedEvent args)
    {
        var stage = args.Stage;

        var sex = TryComp<AnimalSexComponent>(ent, out var sexComp) ? sexComp.Sex : AnimalSexKind.Female;
        if (stage.Names.TryGetValue(sex, out var name))
            _meta.SetEntityName(ent, Loc.GetString(name));

        _scale.SetSpriteScale(ent, stage.Scale);

        if (stage.MaxHealth > 0f)
            _thresholds.SetMobStateThreshold(ent, FixedPoint2.New(stage.MaxHealth), MobState.Dead);

        if (TryComp<ButcherableComponent>(ent, out var butcherable))
        {
            butcherable.SpawnedEntities.Clear();
            if (stage.MeatCount > 0)
                butcherable.SpawnedEntities.Add(new EntitySpawnEntry { PrototypeId = ent.Comp.Meat, Amount = stage.MeatCount });
        }

        if (TryComp<MobPriceComponent>(ent, out var price))
            price.Price = stage.Price;

        if (TryComp<AnimalNeedsComponent>(ent, out var needs))
        {
            if (stage.SatietySeekBelow is { } seekBelow)
                needs.Satiety.SeekBelow = seekBelow;

            if (stage.SatietyDecayPerMinute is { } decay)
                needs.Satiety.DecayPerMinute = decay;
        }

        if (stage.ManureUnitsPerNutrition is { } units && TryComp<ManureProducerComponent>(ent, out var manure))
            manure.UnitsPerNutrition = units;
    }
}
