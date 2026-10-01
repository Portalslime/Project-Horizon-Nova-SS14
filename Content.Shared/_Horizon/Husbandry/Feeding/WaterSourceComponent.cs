namespace Content.Shared._Horizon.Husbandry.Feeding;

/// <summary>
/// Something animals can drink from, like a trough. Handled by AnimalFeedingSystem.
/// </summary>
[RegisterComponent]
public sealed partial class WaterSourceComponent : Component
{
    [DataField]
    public string Solution = "tank";

    /// <summary>
    /// How much is taken from the solution per drink.
    /// </summary>
    [DataField]
    public float AmountPerDrink = 15f;

    /// <summary>
    /// How much hydration a unit of the solution is worth.
    /// </summary>
    [DataField]
    public float HydrationPerUnit = 1f;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);
}
