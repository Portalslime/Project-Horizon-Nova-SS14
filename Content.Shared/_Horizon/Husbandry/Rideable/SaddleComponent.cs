using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Husbandry.Rideable;

/// <summary>
/// Marks an item as a saddle, see <see cref="RideableComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SaddleComponent : Component;
