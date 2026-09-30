using Content.Shared._Horizon.Husbandry.Core;
using Content.Shared._Horizon.Husbandry.Sex;
using Robust.Shared.Random;

namespace Content.Server._Horizon.Husbandry.Sex;

/// <summary>
/// Picks the sex of animals when they are born.
/// </summary>
public sealed class AnimalSexSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimalSexComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<AnimalSexComponent> ent, ref MapInitEvent args)
    {
        EnsureRolled(ent);
    }

    /// <summary>
    /// Rolls the sex if it was not rolled yet. Safe to call many times, other systems call it when they need the
    /// sex during map init, where the order of components is not defined.
    /// </summary>
    public void EnsureRolled(Entity<AnimalSexComponent> ent)
    {
        if (!ent.Comp.Randomize)
            return;

        ent.Comp.Randomize = false;
        ent.Comp.Sex = GrowthRules.RollSex(_random.NextDouble(), ent.Comp.MaleChance);
        Dirty(ent);
    }
}
