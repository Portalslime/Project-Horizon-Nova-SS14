using Content.Shared.FixedPoint;

namespace Content.Shared._Horizon.Plumbing;

/// <summary>
/// Pulls liquid out of the plumbing network into one of the entity's solutions, keeping it topped up.
/// This is what feeds sinks and showers from a supply line instead of conjuring liquid out of thin air.
/// Handled by PlumbingSystem.
/// </summary>
[RegisterComponent]
public sealed partial class PlumbingOutletComponent : Component
{
    /// <summary>
    /// The solution that gets filled from the network.
    /// </summary>
    [DataField]
    public string Solution = "tank";

    /// <summary>
    /// The plumbing node of the NodeContainer this outlet is connected through.
    /// </summary>
    [DataField]
    public string NodeName = "plumbing";

    /// <summary>
    /// Units taken per second at most.
    /// </summary>
    [DataField]
    public FixedPoint2 Rate = 20;
}
