using Content.Shared._Horizon.Husbandry.Core;
using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Husbandry.Sex;

/// <summary>
/// The sex of an animal. Rolled on map init unless <see cref="Randomize"/> is turned off.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnimalSexComponent : Component
{
    [DataField, AutoNetworkedField]
    public AnimalSexKind Sex = AnimalSexKind.Female;

    [DataField]
    public bool Randomize = true;

    /// <summary>
    /// Chance of rolling a male.
    /// </summary>
    [DataField]
    public float MaleChance = 0.5f;
}
