namespace Content.Shared._Horizon.Plumbing;

/// <summary>
/// Reports how full the plumbing network at its node is when examined. Handled by PlumbingSystem.
/// </summary>
[RegisterComponent]
public sealed partial class PlumbingGaugeComponent : Component
{
    /// <summary>
    /// The plumbing node of the NodeContainer this gauge measures.
    /// </summary>
    [DataField]
    public string NodeName = "plumbing";
}
