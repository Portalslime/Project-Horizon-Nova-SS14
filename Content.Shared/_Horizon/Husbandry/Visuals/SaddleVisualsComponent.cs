namespace Content.Shared._Horizon.Husbandry.Visuals;

/// <summary>
/// Reports through <see cref="AnimalVisuals.Saddled"/> whether a saddle sits in the item slot, so that the saddle layer
/// of the sprite can be shown (GenericVisualizer in the prototype). Does not depend on riding: any animal with a saddle
/// slot can have it.
/// </summary>
[RegisterComponent]
public sealed partial class SaddleVisualsComponent : Component
{
    /// <summary>
    /// The item slot that holds the saddle.
    /// </summary>
    [DataField]
    public string Slot = "saddle_slot";
}
