using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Plumbing;

/// <summary>
/// Splits the liquid of the network at its inlet: the chosen reagent goes to the network at the filtered node, and
/// everything else goes on to the network at the outlet node. With no reagent chosen everything goes on.
/// Handled by PlumbingFilterSystem.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class PlumbingFilterComponent : Component, IPlumbingTimed
{
    [DataField]
    public string InletNodeName = "inlet";

    [DataField]
    public string FilteredNodeName = "filtered";

    [DataField]
    public string OutletNodeName = "outlet";

    /// <summary>
    /// The reagent that is sent to the filtered side.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype>? Reagent;

    /// <summary>
    /// Units moved per second at most, filtered and passed on together.
    /// </summary>
    [DataField]
    public FixedPoint2 Rate = 25;

    [DataField]
    public bool Enabled = true;

    [DataField]
    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromSeconds(1);

    [DataField, AutoPausedField]
    public TimeSpan NextUpdate { get; set; }
}

[Serializable, NetSerializable]
public enum PlumbingFilterUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class PlumbingFilterBoundUserInterfaceState(bool enabled, ProtoId<ReagentPrototype>? reagent)
    : BoundUserInterfaceState
{
    public readonly bool Enabled = enabled;
    public readonly ProtoId<ReagentPrototype>? Reagent = reagent;
}

/// <summary>
/// Chooses the reagent to filter, or none.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlumbingFilterSelectReagentMessage(ProtoId<ReagentPrototype>? reagent) : BoundUserInterfaceMessage
{
    public readonly ProtoId<ReagentPrototype>? Reagent = reagent;
}

[Serializable, NetSerializable]
public sealed class PlumbingFilterToggleMessage(bool enabled) : BoundUserInterfaceMessage
{
    public readonly bool Enabled = enabled;
}
