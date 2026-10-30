using Content.Shared.Damage;

namespace Content.Server._Arcane.AppearanceTraits;

/// <summary>
/// Временный компонент на части тела: применяет модификатор входящего урона
/// от активной черты внешности к урону именно этой части тела.
/// </summary>
[RegisterComponent]
public sealed partial class AppearanceTraitDamageModifierComponent : Component
{
    [ViewVariables]
    public DamageModifierSet? Modifier;
}
