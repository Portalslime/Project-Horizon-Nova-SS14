using Content.Shared._Horizon.Husbandry.Core;
using Content.Shared._Horizon.Husbandry.Needs;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rejuvenate;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.Husbandry.Needs;

/// <summary>
/// Drains the needs of animals at a constant rate and hurts the ones that ran out of something.
/// </summary>
public sealed class AnimalNeedsSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimalNeedsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AnimalNeedsComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnMapInit(Entity<AnimalNeedsComponent> ent, ref MapInitEvent args)
    {
        var comp = ent.Comp;
        RollStartingValue(comp.Satiety);
        RollStartingValue(comp.Hydration);

        comp.NextUpdate = _timing.CurTime + comp.UpdateRate;
        comp.NextDamage = _timing.CurTime + comp.DamageInterval;
        UpdateLevels(ent);
    }

    private void OnRejuvenate(Entity<AnimalNeedsComponent> ent, ref RejuvenateEvent args)
    {
        ent.Comp.Satiety.Value = ent.Comp.Satiety.Max;
        ent.Comp.Hydration.Value = ent.Comp.Hydration.Max;
        ent.Comp.Satiety.DeprivedMinutes = 0f;
        ent.Comp.Hydration.DeprivedMinutes = 0f;
        UpdateLevels(ent);
    }

    private void RollStartingValue(NeedState need)
    {
        if (need.Value >= 0f)
            return;

        need.Value = Math.Min(need.Max, _random.NextFloat(need.StartingRange.X, need.StartingRange.Y));
    }

    /// <summary>
    /// Adds to the satiety of the animal.
    /// </summary>
    /// <returns>How much it changed.</returns>
    public float ModifySatiety(Entity<AnimalNeedsComponent> ent, float amount)
    {
        var need = ent.Comp.Satiety;
        var change = NeedRules.Modify(ref need.Value, need.Max, amount);
        UpdateLevels(ent);
        return change;
    }

    /// <summary>
    /// Adds to the hydration of the animal.
    /// </summary>
    /// <returns>How much it changed.</returns>
    public float ModifyHydration(Entity<AnimalNeedsComponent> ent, float amount)
    {
        var need = ent.Comp.Hydration;
        var change = NeedRules.Modify(ref need.Value, need.Max, amount);
        UpdateLevels(ent);
        return change;
    }

    private void UpdateLevels(Entity<AnimalNeedsComponent> ent)
    {
        var comp = ent.Comp;
        var satiety = comp.Satiety.GetLevel();
        var hydration = comp.Hydration.GetLevel();

        if (comp.SatietyLevel == satiety && comp.HydrationLevel == hydration)
            return;

        comp.SatietyLevel = satiety;
        comp.HydrationLevel = hydration;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AnimalNeedsComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextUpdate)
                continue;

            comp.NextUpdate += comp.UpdateRate;

            if (_mobState.IsDead(uid))
                continue;

            var seconds = (float) comp.UpdateRate.TotalSeconds;
            comp.Satiety.DeprivedMinutes += NeedRules.Tick(ref comp.Satiety.Value, comp.Satiety.DecayPerMinute, seconds);
            comp.Hydration.DeprivedMinutes += NeedRules.Tick(ref comp.Hydration.Value, comp.Hydration.DecayPerMinute, seconds);

            var ent = (uid, comp);
            UpdateLevels(ent);

            if (_timing.CurTime >= comp.NextDamage)
            {
                comp.NextDamage += comp.DamageInterval;
                ApplyDeprivationDamage(uid, comp);
            }
        }
    }

    private void ApplyDeprivationDamage(EntityUid uid, AnimalNeedsComponent comp)
    {
        var total = new DamageSpecifier();
        AddDeprivation(ref total, comp.Satiety);
        AddDeprivation(ref total, comp.Hydration);

        if (!total.Empty)
            _damageable.TryChangeDamage(uid, total, ignoreResistances: true, interruptsDoAfters: false);
    }

    private static void AddDeprivation(ref DamageSpecifier total, NeedState need)
    {
        if (need.DeprivedMinutes > 0f && !need.DeprivationDamage.Empty)
            total += need.DeprivationDamage * need.DeprivedMinutes;

        need.DeprivedMinutes = 0f;
    }
}
