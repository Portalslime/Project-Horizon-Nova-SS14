using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Husbandry.Production;

/// <summary>
/// Makes an animal leave something behind for what it ate. Handled by ManureProducerSystem.
/// </summary>
[RegisterComponent]
public sealed partial class ManureProducerComponent : Component
{
    /// <summary>
    /// What is left behind. Should be a stackable entity: the pieces are spawned as stacks.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Product;

    /// <summary>
    /// How many pieces of <see cref="Product"/> the animal leaves for each point of nutrition it ate.
    /// </summary>
    [DataField]
    public float UnitsPerNutrition = 1f;

    /// <summary>
    /// How far behind the animal (opposite to where it looks), in tiles, the product is left. 0 is under its centre.
    /// </summary>
    [DataField]
    public float DropOffset;

    /// <summary>
    /// The part of a piece that is not enough for one yet, it is kept for the next meal.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float Accumulated;
}
