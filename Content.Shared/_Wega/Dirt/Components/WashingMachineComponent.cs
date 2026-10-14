using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DirtVisuals;

// Порт с ss14-wega (wega-team/ss14-wega), Content.Shared/_Wega/Dirt/Components/WashingMachineComponent.cs
[RegisterComponent]
public sealed partial class WashingMachineComponent : Component
{
    [DataField("washTime")]
    public float WashTime = 20f;

    [ViewVariables]
    public float RemainingTime;

    [ViewVariables]
    public bool IsWashing;

    [DataField("finishSound")]
    public SoundSpecifier FinishSound = new SoundPathSpecifier("/Audio/Machines/ding.ogg");

    // --- HN: моющее средство ---

    /// <summary>Имя раствора со средством (должно совпадать с прототипом).</summary>
    public const string CleanerSolutionName = "cleaner";

    /// <summary>Реагент, который считается моющим средством.</summary>
    [DataField("cleanerReagent")]
    public ProtoId<ReagentPrototype> CleanerReagent = "SpaceCleaner";

    /// <summary>Сколько средства тратится за полную стирку.</summary>
    [DataField("cleanerPerWash")]
    public float CleanerPerWash = 5f;

    /// <summary>Сколько грязи снимает полная стирка.</summary>
    [DataField("fullCleanAmount")]
    public float FullCleanAmount = 100f;

    /// <summary>Сколько грязи снимает стирка без средства.</summary>
    [DataField("partialCleanAmount")]
    public float PartialCleanAmount = 50f;
}

[Serializable, NetSerializable]
public enum WashingMachineVisuals : byte
{
    Washing,
    IsWashing
}
