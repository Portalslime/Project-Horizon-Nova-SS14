namespace Content.Shared._Horizon.Husbandry.Pulling;

/// <summary>
/// Makes a heavy animal light enough to be dragged around while somebody pulls it: the pull joint moves the lighter
/// body, so a mass of several hundred kilos would not go anywhere behind a person. The animal gets its own mass back
/// as soon as it is let go. Handled by PullMassSystem.
/// </summary>
[RegisterComponent]
public sealed partial class PullMassComponent : Component
{
    /// <summary>
    /// The density of its fixtures while it is being pulled, never more than they have anyway. The default is what
    /// an ordinary mob has.
    /// </summary>
    [DataField]
    public float Density = 50f;

    /// <summary>
    /// The real density of each fixture while the animal is being pulled, empty otherwise.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, float> OriginalDensities = new();
}
