namespace Content.Shared._Horizon.Husbandry.Rideable;

/// <summary>
/// Raised on an animal when somebody climbs on it.
/// </summary>
[ByRefEvent]
public readonly record struct RiderMountedEvent(EntityUid Rider);

/// <summary>
/// Raised on an animal when its rider gets off.
/// </summary>
[ByRefEvent]
public readonly record struct RiderDismountedEvent(EntityUid Rider);
