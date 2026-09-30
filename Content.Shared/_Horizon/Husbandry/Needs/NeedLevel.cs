using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Husbandry.Needs;

/// <summary>
/// A coarse state of a need, this is what clients know about.
/// </summary>
[Serializable, NetSerializable]
public enum NeedLevel : byte
{
    Satisfied,
    Low,
    Empty,
}
