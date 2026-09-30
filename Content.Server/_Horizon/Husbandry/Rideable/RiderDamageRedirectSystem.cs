using Content.Shared._Horizon.Husbandry.Rideable;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;

namespace Content.Server._Horizon.Husbandry.Rideable;

/// <summary>
/// The animal takes the damage meant for its rider.
/// </summary>
public sealed class RiderDamageRedirectSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RiderComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
    }

    private void OnBeforeDamageChanged(Entity<RiderComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled ||
            ent.Comp.Mount is not { } mount ||
            !TryComp<RideableComponent>(mount, out var rideable) ||
            !rideable.RedirectDamage ||
            args.Damage.Empty)
            return;

        // Healing goes to the rider as usual.
        foreach (var value in args.Damage.DamageDict.Values)
        {
            if (value < 0)
                return;
        }

        args.Cancelled = true;
        _damageable.TryChangeDamage(mount, args.Damage, origin: args.Origin);
    }
}
