/*
 * Horizon Nova — AGPLv3
 * Copyright (c) 2026 Horizon Nova Contributors
 *
 * Раса «Резоми». «Четырёхканальный слух» — адаптация ss14-wega
 * (Content.Shared/_Wega/Resomi/Abilities/Hearing), расширено цветовыми каналами.
 */

using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._HorizonNova.Resomi.Hearing;

/// <summary>
/// Навык «Четырёхканальный слух»: выдаёт экшен-переключатель.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ResomiHearingSkillComponent : Component
{
    [DataField]
    public EntProtoId HearingAction = "ActionResomiFourChannelHearing";

    [DataField]
    public EntityUid? HearingActionEntity;
}

/// <summary>
/// Висит на резоми, пока включён «Четырёхканальный слух».
/// Пока компонент активен, стамина не восстанавливается.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ResomiHearingComponent : Component
{
    /// <summary>
    /// Радиус, в котором слышны источники звука (тайлы).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Radius = 10f;
}

/// <summary>
/// Временный маркер источника звука для «Четырёхканального слуха».
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ResomiSoundMarkerComponent : Component
{
    [DataField, AutoNetworkedField]
    public ResomiSoundChannel Channel = ResomiSoundChannel.Combat;

    /// <summary>
    /// Сколько секунд маркер живёт на сервере.
    /// </summary>
    [DataField]
    public float Lifetime = 1.2f;

    [DataField]
    public TimeSpan ExpireAt;
}

/// <summary>
/// Канал звука: определяет цвет маркера на клиенте.
/// </summary>
public enum ResomiSoundChannel : byte
{
    /// <summary>Стрельба, нанесение урона, взрывы — красный.</summary>
    Combat,

    /// <summary>Падения сущностей — жёлтый.</summary>
    Fall,

    /// <summary>Речь, крики — зелёный.</summary>
    Voice,

    /// <summary>Шаги от бега — серый.</summary>
    Footstep,
}

/// <summary>
/// Событие экшена «Четырёхканальный слух».
/// </summary>
public sealed partial class ResomiHearingToggleEvent : InstantActionEvent;
