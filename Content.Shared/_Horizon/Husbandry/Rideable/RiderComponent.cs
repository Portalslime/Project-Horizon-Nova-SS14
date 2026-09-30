using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Husbandry.Rideable;

/// <summary>
/// Added to whoever is riding an animal.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RiderComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Mount;

    /// <summary>
    /// The draw depth of the rider before it was put over the animal. Only used on the client.
    /// </summary>
    [ViewVariables]
    public int? OriginalDrawDepth;
}
