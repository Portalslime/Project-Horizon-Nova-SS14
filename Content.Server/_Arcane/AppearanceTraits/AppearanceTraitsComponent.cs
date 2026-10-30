using Content.Shared._Arcane.AppearanceTraits;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Server._Arcane.AppearanceTraits;

/// <summary>
/// Хранит набор «черт внешности», которые сейчас активны на персонаже,
/// и закэшированные эффекты этих черт.
///
/// Компонент вешается автоматически на каждого гуманоида
/// (см. <see cref="AppearanceTraitsSystem"/>) и пересчитывается при любом
/// изменении marking'ов. Игровая логика других систем должна спрашивать
/// активные черты через <see cref="AppearanceTraitsSystem.HasTrait"/>,
/// а не лазить в <c>HumanoidAppearanceComponent.MarkingSet</c> напрямую.
/// </summary>
[RegisterComponent]
public sealed partial class AppearanceTraitsComponent : Component
{
    /// <summary>
    /// Идентификаторы активных черт внешности.
    /// </summary>
    [ViewVariables]
    public readonly HashSet<ProtoId<AppearanceTraitPrototype>> ActiveTraits = new();

    /// <summary>
    /// Типы компонентов, которые выдала именно эта система.
    /// Нужно, чтобы при снятии черты не удалить компонент, который был у моба изначально.
    /// </summary>
    [ViewVariables]
    public readonly HashSet<Type> AddedComponents = new();

    /// <summary>
    /// Суммарный бонус к урону без оружия от активных черт.
    /// </summary>
    [ViewVariables]
    public DamageSpecifier MeleeDamage = new();

    /// <summary>
    /// Суммарный модификатор входящего урона от активных черт.
    /// </summary>
    [ViewVariables]
    public DamageModifierSet? DamageModifier;

    /// <summary>
    /// Суммарный множитель скорости ходьбы.
    /// </summary>
    [ViewVariables]
    public float WalkSpeedModifier = 1f;

    /// <summary>
    /// Суммарный множитель скорости бега.
    /// </summary>
    [ViewVariables]
    public float SprintSpeedModifier = 1f;
}
