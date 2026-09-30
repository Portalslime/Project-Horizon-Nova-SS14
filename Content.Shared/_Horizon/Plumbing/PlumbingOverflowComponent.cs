using Content.Shared.FixedPoint;

namespace Content.Shared._Horizon.Plumbing;

/// <summary>
/// A drain-type device (toilet, drain, drain sink) that lets liquid out of a network that is full and being pushed
/// into, spilling it on the floor next to the device. That makes room, so the pump feeding the network can keep
/// going. Without any such device on the network the pump just stops. Handled by PlumbingOverflowSystem.
/// </summary>
[RegisterComponent]
public sealed partial class PlumbingOverflowComponent : Component
{
    /// <summary>
    /// The plumbing node of the NodeContainer this device is connected through.
    /// </summary>
    [DataField]
    public string NodeName = "plumbing";

    /// <summary>
    /// Units let out per second at most.
    /// </summary>
    [DataField]
    public FixedPoint2 Rate = 20;
}
