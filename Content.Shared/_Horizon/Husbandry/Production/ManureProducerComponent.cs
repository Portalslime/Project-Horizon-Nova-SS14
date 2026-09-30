using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Husbandry.Production;

/// <summary>
/// Makes an animal leave something behind for what it ate. Handled by ManureProducerSystem.
/// </summary>
[RegisterComponent]
public sealed partial class ManureProducerComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Product;

    /// <summary>
    /// How much nutrition the animal has to eat for one piece of <see cref="Product"/>.
    /// </summary>
    [DataField]
    public float NutritionPerDrop = 50f;

    /// <summary>
    /// What has been eaten and not turned into a product yet.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float Accumulated;

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");
}
