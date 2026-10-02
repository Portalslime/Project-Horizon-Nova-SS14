/*
 * Horizon Nova — AGPLv3
 * Copyright (c) 2026 Horizon Nova Contributors
 *
 * Порт с ss14-wega (Content.Shared/Flash/Components/FlashModifierComponent.cs).
 * Умножает длительность вспышки для носителя компонента.
 */

using Robust.Shared.GameStates;

namespace Content.Shared._HorizonNova.Flash;

/// <summary>
/// Множитель длительности вспышек для сущности.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FlashModifierComponent : Component
{
    [DataField]
    public float Modifier = 1f;
}
