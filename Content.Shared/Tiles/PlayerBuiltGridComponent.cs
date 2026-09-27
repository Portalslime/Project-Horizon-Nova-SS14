namespace Content.Shared.Tiles;

/// <summary>
/// Marks a grid that was created by a player placing a floor tile in empty space,
/// rather than spawned from a shuttle/vessel template. Exempts the grid from the
/// automatic grid cleanup systems, which would otherwise treat an unfinished or
/// unpowered player-built shuttle the same as abandoned debris.
/// </summary>
[RegisterComponent]
public sealed partial class PlayerBuiltGridComponent : Component
{
}
