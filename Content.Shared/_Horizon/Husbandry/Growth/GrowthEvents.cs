namespace Content.Shared._Horizon.Husbandry.Growth;

/// <summary>
/// Raised on an animal when it enters a growth stage, including the first one.
/// </summary>
[ByRefEvent]
public readonly record struct GrowthStageChangedEvent(int Index, GrowthStageDef Stage);
