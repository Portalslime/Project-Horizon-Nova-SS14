using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Defecation;

/// <summary>
/// A seat (a toilet) that receives the waste of whoever relieves themselves while buckled to it: instead of an item
/// appearing, liquid is added to one of the seat's solutions. What does not fit is spilled on the floor.
/// Handled by DefecationSystem.
/// </summary>
[RegisterComponent]
public sealed partial class DefecationSeatComponent : Component
{
    /// <summary>
    /// The solution that receives the waste, usually the toilet buffer.
    /// </summary>
    [DataField]
    public string Solution = "drainBuffer";

    [DataField]
    public ProtoId<ReagentPrototype> Reagent = "Sewage";

    /// <summary>
    /// Units added per use, the same amount as the item holds.
    /// </summary>
    [DataField]
    public FixedPoint2 Amount = 20;
}
