using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DirtVisuals;

// Порт с ss14-wega (wega-team/ss14-wega), Content.Shared/_Wega/Dirt/Components/DirtableComponent.cs
[RegisterComponent, NetworkedComponent]
public sealed partial class DirtableComponent : Component
{
    [DataField("threshold")]
    public FixedPoint2 Threshold { get; set; } = 20;

    [DataField("dirtSprite")]
    public string DirtSpritePath { get; set; } = "_Wega/Mobs/Effects/dirt_overlay.rsi";

    [DataField("dirtState")]
    public string DirtState { get; set; } = "jumpsuit";

    [DataField("foldingDirtState")]
    public string? FoldingDirtState { get; set; }

    [DataField("equippedDirtState")]
    public string EquippedDirtState { get; set; } = "equipped-jumpsuit";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 CurrentDirtLevel { get; set; }

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Color DirtColor { get; set; } = Color.White;

    /// <summary>
    /// HN: влажность одежды, 0..100. Намокает под душем, сохнет со временем.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float CurrentWetness { get; set; }

    /// <summary>
    /// HN: чем пропитана одежда (реагенты) — для осмотра.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public List<ProtoId<ReagentPrototype>> DirtyReagents { get; set; } = new();

    [ViewVariables]
    public bool IsDirty => CurrentDirtLevel >= Threshold;
}

[Serializable, NetSerializable]
public sealed partial class DirtableComponentState : ComponentState
{
    public FixedPoint2 CurrentDirtLevel;
    public Color DirtColor;
    public bool IsDirty;
    public float CurrentWetness;
    public List<string> DirtyReagents;

    public DirtableComponentState(FixedPoint2 currentDirtLevel, Color dirtColor, bool isDirty, float currentWetness,
        List<string> dirtyReagents)
    {
        CurrentDirtLevel = currentDirtLevel;
        DirtColor = dirtColor;
        IsDirty = isDirty;
        CurrentWetness = currentWetness;
        DirtyReagents = dirtyReagents;
    }
}
