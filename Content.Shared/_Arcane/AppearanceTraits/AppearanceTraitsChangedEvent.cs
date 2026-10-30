using Robust.Shared.Prototypes;

namespace Content.Shared._Arcane.AppearanceTraits;

/// <summary>
/// Поднимается на сущности, у которой изменился набор активных черт внешности.
/// Позволяет другим системам реагировать на появление/исчезновение эффекта
/// (например, выдать действие или пересчитать характеристики).
/// </summary>
public sealed class AppearanceTraitsChangedEvent(
    IReadOnlySet<ProtoId<AppearanceTraitPrototype>> added,
    IReadOnlySet<ProtoId<AppearanceTraitPrototype>> removed)
    : EntityEventArgs
{
    /// <summary>
    /// Черты, которые появились.
    /// </summary>
    public readonly IReadOnlySet<ProtoId<AppearanceTraitPrototype>> Added = added;

    /// <summary>
    /// Черты, которые пропали.
    /// </summary>
    public readonly IReadOnlySet<ProtoId<AppearanceTraitPrototype>> Removed = removed;
}
