/*
 * Horizon Nova — AGPLv3
 * Copyright (c) 2026 Horizon Nova Contributors
 *
 * Раса «Резоми». Увеличивает разброс двуручного огнестрела у носителя
 * <see cref="TwoHandedSpreadPenaltyComponent"/>. Лазерное (hitscan) оружие не затронуто.
 */

using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._HorizonNova.Weapons;

public sealed partial class TwoHandedSpreadPenaltySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Событие поднимается на самой пушке, поэтому подписка компонентная, а не broadcast.
        SubscribeLocalEvent<GunComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
    }

    private void OnGunRefreshModifiers(Entity<GunComponent> ent, ref GunRefreshModifiersEvent args)
    {
        var gun = ent.Owner;

        // Штраф только для двуручного оружия.
        if (!TryComp<WieldableComponent>(gun, out var wieldable) || wieldable.FreeHandsRequired < 1)
            return;

        // Оружие должно быть в руках у сущности со штрафом.
        var holder = Transform(gun).ParentUid;
        if (!TryComp<TwoHandedSpreadPenaltyComponent>(holder, out var penalty))
            return;

        if (IsHitscanWeapon(gun))
            return;

        args.MinAngle *= penalty.SpreadMultiplier;
        args.MaxAngle *= penalty.SpreadMultiplier;
        args.AngleIncrease *= penalty.SpreadMultiplier;
    }

    /// <summary>
    /// Лазерное оружие в этой сборке бьёт хитсканом (HitscanBatteryAmmoProvider либо
    /// энтити-боеприпас с HitscanAmmo), поэтому его разброс не портим.
    /// </summary>
    private bool IsHitscanWeapon(EntityUid gun)
    {
        if (HasComp<HitscanBatteryAmmoProviderComponent>(gun))
            return true;

        if (TryComp<BasicEntityAmmoProviderComponent>(gun, out var basic)
            && _prototypes.TryIndex<EntityPrototype>(basic.Proto, out var ammoProto)
            && ammoProto.Components.ContainsKey("HitscanAmmo"))
            return true;

        return false;
    }
}
