using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Materials;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Plumbing;

/// <summary>
/// A machine that takes a reagent (liquid feces) out of the plumbing network it is connected to and turns it into
/// a material (biomass), which is kept in the machine's material storage until somebody collects it.
/// Handled by PlumbingComposterSystem.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class PlumbingComposterComponent : Component
{
    /// <summary>
    /// The plumbing node of the NodeContainer the machine is connected through.
    /// </summary>
    [DataField]
    public string NodeName = "port";

    /// <summary>
    /// The reagent that is consumed.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> Reagent = "Feces";

    /// <summary>
    /// The material that is produced into the material storage.
    /// </summary>
    [DataField]
    public ProtoId<MaterialPrototype> Material = "Biomass";

    /// <summary>
    /// Material units produced for every unit of reagent.
    /// </summary>
    [DataField]
    public float MaterialPerUnit = 1f;

    /// <summary>
    /// Reagent units consumed per second at most.
    /// </summary>
    [DataField]
    public FixedPoint2 Rate = 10;

    /// <summary>
    /// Material that has been produced but is less than one whole unit so far.
    /// </summary>
    [ViewVariables]
    public float Progress;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField, AutoPausedField]
    public TimeSpan NextUpdate;
}
