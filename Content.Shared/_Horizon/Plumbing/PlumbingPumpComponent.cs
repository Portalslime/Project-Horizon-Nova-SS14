using Content.Shared.FixedPoint;

namespace Content.Shared._Horizon.Plumbing;

/// <summary>
/// A directional pump between two plumbing nodes. Every step it tries to move liquid from the network at the
/// inlet node to the network at the outlet node, limited by its rate, by what the inlet side holds and by the
/// free space on the outlet side. Needs no power. Handled by PlumbingSystem.
/// </summary>
[RegisterComponent]
public sealed partial class PlumbingPumpComponent : Component
{
    [DataField]
    public string InletNodeName = "inlet";

    [DataField]
    public string OutletNodeName = "outlet";

    /// <summary>
    /// Units moved per second at most.
    /// </summary>
    [DataField]
    public FixedPoint2 Rate = 25;

    [DataField]
    public bool Enabled = true;
}
