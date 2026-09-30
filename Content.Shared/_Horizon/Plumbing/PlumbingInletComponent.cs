using Content.Shared.FixedPoint;

namespace Content.Shared._Horizon.Plumbing;

/// <summary>
/// Pushes the contents of one of the entity's solutions (a sink or drain buffer, for example) into the plumbing
/// network at its node. Handled by PlumbingInletSystem.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class PlumbingInletComponent : Component
{
    /// <summary>
    /// The solution that gets drained into the network.
    /// </summary>
    [DataField]
    public string Solution = "drainBuffer";

    /// <summary>
    /// The plumbing node of the NodeContainer this inlet is connected through.
    /// </summary>
    [DataField]
    public string NodeName = "plumbing";

    /// <summary>
    /// Units moved per second at most.
    /// </summary>
    [DataField]
    public FixedPoint2 Rate = 30;

    /// <summary>
    /// If the network is full or missing, spill what would have been moved instead of holding on to it.
    /// </summary>
    [DataField]
    public bool SpillWhenBlocked = true;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField, AutoPausedField]
    public TimeSpan NextUpdate;
}
