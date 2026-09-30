using Content.Shared.FixedPoint;

namespace Content.Shared._Horizon.Plumbing;

/// <summary>
/// Marks a plumbing port: a fixture on the pipe network that portable devices, such as barrels, are anchored onto.
/// The port's plumbing node has the "port" flag set. Handled by PlumbingPortableSystem.
/// </summary>
[RegisterComponent]
public sealed partial class PlumbingPortComponent : Component;

/// <summary>
/// A device that can only be anchored on top of a <see cref="PlumbingPortComponent"/> and joins the network through
/// it while anchored. Handled by PlumbingPortableSystem.
/// </summary>
[RegisterComponent]
public sealed partial class PlumbingPortableComponent : Component
{
    /// <summary>
    /// The portable plumbing node of the NodeContainer that connects to the port.
    /// </summary>
    [DataField]
    public string NodeName = "port";
}

/// <summary>
/// A tank that shares its liquid with the network it is connected to: liquid flows from the fuller to the emptier
/// side, so it fills when the network is full and empties into it when the network runs low.
/// Handled by PlumbingTankSystem.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class PlumbingTankComponent : Component, IPlumbingTimed
{
    [DataField]
    public string Solution = "barrel";

    [DataField]
    public string NodeName = "port";

    /// <summary>
    /// Units moved per second at most.
    /// </summary>
    [DataField]
    public FixedPoint2 Rate = 50;

    [DataField]
    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromSeconds(1);

    [DataField, AutoPausedField]
    public TimeSpan NextUpdate { get; set; }
}
