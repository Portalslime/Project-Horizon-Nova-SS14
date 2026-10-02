/*
 * Horizon Nova — AGPLv3
 * Copyright (c) 2026 Horizon Nova Contributors
 *
 * Раса «Резоми». Механика спринта — адаптация ss14-wega (Content.Shared/_Wega/Resomi/Abilities),
 * стамина — LuaSprint из этой сборки.
 */

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._HorizonNova.Resomi.Sprint;

/// <summary>
/// Экшен-переключатель «Спринт»: ускоряет передвижение на <see cref="SpeedModifier"/>
/// и тратит стамину (<see cref="Content.Shared._Lua.Sprint.LuaSprintComponent"/>).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ResomiSprintComponent : Component
{
    /// <summary>
    /// Активен ли спринт в данный момент.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField]
    public EntProtoId SprintAction = "ActionResomiSprint";

    [DataField, AutoNetworkedField]
    public EntityUid? SprintActionEntity;

    /// <summary>
    /// Множитель скорости передвижения. 1.25 = +25%.
    /// </summary>
    [DataField]
    public float SpeedModifier = 1.25f;

    /// <summary>
    /// Расход стамины в секунду, пока спринт активен.
    /// </summary>
    [DataField]
    public float StaminaDrainPerSecond = 18f;

    /// <summary>
    /// Базовое восстановление стамины в секунду (до множителей быстрого/медленного восстановления).
    /// </summary>
    [DataField]
    public float StaminaRegenPerSecond = 9f;

    /// <summary>
    /// При опустошении стамины до этого значения спринт отключается сам.
    /// </summary>
    [DataField]
    public float AutoDeactivateThreshold = 1f;

    /// <summary>
    /// Если отключить спринт, пока стамины больше этого порога, восстановление ускоряется.
    /// </summary>
    [DataField]
    public float FastRecoveryThreshold = 40f;

    /// <summary>
    /// Множитель восстановления стамины после ручного отключения выше порога.
    /// </summary>
    [DataField]
    public float FastRecoveryMultiplier = 1.2f;

    /// <summary>
    /// Множитель восстановления стамины после полного истощения.
    /// </summary>
    [DataField]
    public float SlowRecoveryMultiplier = 0.8f;

    /// <summary>
    /// Идёт ускоренное восстановление (спринт отключён вручную выше порога).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool FastRecovery;

    /// <summary>
    /// Идёт замедленное восстановление (стамина была опустошена).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SlowRecovery;
}
