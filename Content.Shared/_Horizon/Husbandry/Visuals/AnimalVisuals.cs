using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Husbandry.Visuals;

[Serializable, NetSerializable]
public enum AnimalVisuals : byte
{
    /// <summary>
    /// Bool, the animal is busy eating or drinking.
    /// </summary>
    Eating,

    /// <summary>
    /// Bool, a saddle sits in the saddle slot of the animal.
    /// </summary>
    Saddled,
}
