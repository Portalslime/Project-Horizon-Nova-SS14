using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Husbandry.Production;

/// <summary>
/// Raised on an animal whenever it left something behind, after the product was spawned.
/// </summary>
[ByRefEvent]
public readonly record struct AnimalProducedEvent(EntProtoId Product, int Units);
