using Robust.Shared.Audio;
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
}

[Serializable, NetSerializable]
public enum WashingMachineVisuals : byte
{
    Washing,
    IsWashing
}
