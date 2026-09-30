using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Husbandry.Needs;

/// <summary>
/// Satiety and hydration of an animal. Both drain at a constant rate and hurt the animal once they are empty.
/// Handled by AnimalNeedsSystem.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class AnimalNeedsComponent : Component
{
    [DataField]
    public NeedState Satiety = new() { DecayPerMinute = 2f };

    [DataField]
    public NeedState Hydration = new() { DecayPerMinute = 1f };

    /// <summary>
    /// What clients see, updated only when the level changes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NeedLevel SatietyLevel;

    [DataField, AutoNetworkedField]
    public NeedLevel HydrationLevel;

    /// <summary>
    /// How often the needs are processed.
    /// </summary>
    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How often the deprivation damage is applied.
    /// </summary>
    [DataField]
    public TimeSpan DamageInterval = TimeSpan.FromSeconds(10);

    [DataField, AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField, AutoPausedField]
    public TimeSpan NextDamage;
}
