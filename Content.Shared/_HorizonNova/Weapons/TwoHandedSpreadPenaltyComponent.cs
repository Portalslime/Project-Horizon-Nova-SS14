/*
 * Horizon Nova — AGPLv3
 * Copyright (c) 2026 Horizon Nova Contributors
 *
 * Раса «Резоми». Идея с ss14-wega (Content.Shared/_Wega/Resomi/Abilities/WeaponsUseInabilityComponent.cs):
 * двуручное оружие в руках резоми даёт больший разброс. Здесь это множитель +30%,
 * лазерное (hitscan) оружие исключено.
 */

using Robust.Shared.GameStates;

namespace Content.Shared._HorizonNova.Weapons;

/// <summary>
/// Носитель хуже обращается с двуручным огнестрелом: разброс умножается на <see cref="SpreadMultiplier"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TwoHandedSpreadPenaltyComponent : Component
{
    /// <summary>
    /// Множитель разброса двуручного огнестрела. 1.3 = +30%.
    /// </summary>
    [DataField]
    public float SpreadMultiplier = 1.3f;
}
