using Content.Shared.Damage;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Arcane.AppearanceTraits;

/// <summary>
/// Описывает «черту внешности» — набор marking'ов, которые сами по себе являются
/// только картинкой, но при наличии на персонаже дают ему игровой эффект.
///
/// Одна и та же черта может срабатывать как по списку конкретных marking'ов
/// (<see cref="Markings"/>), так и по целой категории (<see cref="Categories"/>),
/// например «любые рога» или «любой хвост».
///
/// Эффекты:
/// <list type="bullet">
/// <item><see cref="MeleeDamage"/> — бонус к урону без оружия (когти, рога и т.п.).</item>
/// <item><see cref="DamageModifier"/> — сопротивляемость входящему урону (шипы, чешуя).</item>
/// <item><see cref="WalkSpeedModifier"/> / <see cref="SprintSpeedModifier"/> — множители скорости.</item>
/// <item><see cref="Components"/> — любые компоненты, которые выдаются персонажу на время действия черты.</item>
/// </list>
///
/// Отслеживание делает <c>AppearanceTraitsSystem</c>: при появлении/изменении marking'ов
/// он пересчитывает активные черты и применяет/снимает эффекты.
/// </summary>
[Prototype("appearanceTrait")]
public sealed partial class AppearanceTraitPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Локализационный ключ названия черты (для осмотра/UI).
    /// </summary>
    [DataField]
    public LocId Name { get; private set; } = string.Empty;

    /// <summary>
    /// Локализационный ключ описания черты (необязательно).
    /// </summary>
    [DataField]
    public LocId? Description { get; private set; }

    /// <summary>
    /// Marking'и, дающие эту черту. Достаточно любого, если <see cref="RequireAllMarkings"/> не включён.
    /// </summary>
    [DataField]
    public List<ProtoId<MarkingPrototype>> Markings { get; private set; } = new();

    /// <summary>
    /// Активировать черту при наличии любого marking'а из перечисленных категорий.
    /// Если заданы и <see cref="Markings"/>, и <see cref="Categories"/>, достаточно совпадения любого из условий.
    /// </summary>
    [DataField]
    public List<MarkingCategories> Categories { get; private set; } = new();

    /// <summary>
    /// Требовать все marking'и из <see cref="Markings"/>, а не хотя бы один.
    /// </summary>
    [DataField]
    public bool RequireAllMarkings { get; private set; }

    /// <summary>
    /// Расы, которым доступна черта. Пустой список — любая раса.
    /// </summary>
    [DataField]
    public List<ProtoId<SpeciesPrototype>> Species { get; private set; } = new();

    /// <summary>
    /// Бонусный урон, который добавляется к атакам без оружия.
    /// </summary>
    [DataField]
    public DamageSpecifier? MeleeDamage { get; private set; }

    /// <summary>
    /// Модификатор входящего урона (множители и плоские вычеты).
    /// </summary>
    [DataField]
    public DamageModifierSet? DamageModifier { get; private set; }

    /// <summary>
    /// Множитель скорости ходьбы. 1 — без изменений.
    /// </summary>
    [DataField]
    public float WalkSpeedModifier { get; private set; } = 1f;

    /// <summary>
    /// Множитель скорости бега. 1 — без изменений.
    /// </summary>
    [DataField]
    public float SprintSpeedModifier { get; private set; } = 1f;

    /// <summary>
    /// Компоненты, выдаваемые персонажу, пока черта активна.
    /// Если два разных черты дают компонент одного типа, применяется черта с большим <see cref="Priority"/>.
    /// </summary>
    [DataField]
    public ComponentRegistry Components { get; private set; } = new();

    /// <summary>
    /// Приоритет применения. Чем больше, тем раньше применяются компоненты черты.
    /// </summary>
    [DataField]
    public int Priority { get; private set; }
}
