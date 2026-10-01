using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Husbandry.Feeding;

/// <summary>
/// Raised on an animal when it starts eating. The food is still there, the animal gets it when it is done.
/// </summary>
[ByRefEvent]
public readonly record struct AnimalEatStartedEvent(EntityUid Food);

/// <summary>
/// Raised on an animal when it was interrupted while eating and got nothing.
/// </summary>
[ByRefEvent]
public readonly record struct AnimalEatInterruptedEvent;

/// <summary>
/// Raised on an animal whenever it ate something, after its satiety was updated.
/// </summary>
[ByRefEvent]
public readonly record struct AnimalAteEvent(EntityUid Food, float Nutrition);

/// <summary>
/// Raised on an animal whenever it drank something, after its hydration was updated.
/// </summary>
[ByRefEvent]
public readonly record struct AnimalDrankEvent(EntityUid Source, float Amount);

[Serializable, NetSerializable]
public sealed partial class AnimalEatDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class AnimalDrinkDoAfterEvent : SimpleDoAfterEvent;
