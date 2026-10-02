namespace Content.Shared._Horizon.Urination;

/// <summary>
/// Поднимается на сущности, которая справила малую нужду, после сброса потребности.
/// Кто обработал — ставит <see cref="Handled"/>: сиденье туалета, иначе лужа на полу.
/// </summary>
[ByRefEvent]
public record struct UrinateEvent(bool Accident, bool Handled = false);

/// <summary>
/// Поднимается на сущности, у которой случилась авария (нужда перетерплена).
/// </summary>
[ByRefEvent]
public record struct UrinationAccidentEvent;
