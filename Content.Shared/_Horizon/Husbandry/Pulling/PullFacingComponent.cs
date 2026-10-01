namespace Content.Shared._Horizon.Husbandry.Pulling;

/// <summary>
/// An animal that turns to face whoever drags it, so it is led head first and not backwards. Handled by
/// PullFacingSystem.
/// </summary>
[RegisterComponent]
public sealed partial class PullFacingComponent : Component
{
    /// <summary>
    /// How fast the animal turns, radians per second.
    /// </summary>
    [DataField]
    public float RotationSpeed = 8f;

    /// <summary>
    /// It does not turn while the one who pulls it is closer than this, the direction would jump around.
    /// </summary>
    [DataField]
    public float MinDistance = 0.4f;
}
