namespace Content.Shared._Horizon.Defecation;

/// <summary>
/// Raised on an entity that relieves itself, after its need was reset. Whoever takes care of it sets
/// <see cref="Handled"/>: a toilet seat, otherwise the default of leaving an item on the floor.
/// </summary>
[ByRefEvent]
public record struct DefecateEvent(bool Accident, bool Handled = false);

/// <summary>
/// Raised on an entity that had an accident on the floor, so that the consequences can be applied
/// (it starts to smell, for one).
/// </summary>
[ByRefEvent]
public record struct DefecationAccidentEvent;
