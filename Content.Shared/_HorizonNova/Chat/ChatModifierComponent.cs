/*
 * Horizon Nova — AGPLv3
 * Copyright (c) 2026 Horizon Nova Contributors
 *
 * Порт с ss14-wega (Content.Shared/Chat/ChatModifierComponent.cs).
 * Позволяет расе слышать шёпот дальше обычного радиуса.
 */

using Robust.Shared.GameStates;

namespace Content.Shared._HorizonNova.Chat;

/// <summary>
/// Модификатор чата: расширяет радиус, в котором сущность разбирает шёпот.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChatModifierComponent : Component
{
    /// <summary>
    /// Радиус, в котором шёпот слышен полностью (в тайлах).
    /// Обычный персонаж разбирает шёпот только в упор.
    /// </summary>
    [DataField]
    public int WhisperListeningRange = 2;
}
