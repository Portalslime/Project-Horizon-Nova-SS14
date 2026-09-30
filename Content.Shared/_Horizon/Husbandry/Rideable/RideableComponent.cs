using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Husbandry.Rideable;

/// <summary>
/// An animal that can be ridden. Needs a <c>Strap</c> and an item slot for the saddle, the slot is locked while
/// somebody rides. Handled by RideableSystem.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RideableComponent : Component
{
    /// <summary>
    /// The item slot that holds the saddle. Without a saddle in it nobody can climb on.
    /// </summary>
    [DataField]
    public string SaddleSlot = "saddle_slot";

    /// <summary>
    /// How many hands the reins take.
    /// </summary>
    [DataField]
    public int RequiredHands = 1;

    /// <summary>
    /// Whether the damage meant for the rider is taken by the animal instead.
    /// </summary>
    [DataField]
    public bool RedirectDamage = true;

    /// <summary>
    /// Whether the animal opens doors by walking into them while it is ridden, with the access of its rider.
    /// Without a rider it never does.
    /// </summary>
    [DataField]
    public bool RiderOpensDoors = true;

    /// <summary>
    /// The bump tag was added for the ride, so that is what has to be taken away afterwards.
    /// </summary>
    [ViewVariables]
    public bool AddedBumpTag;

    /// <summary>
    /// How far the rider is drawn from the animal while it faces each way. Only a visual thing.
    /// </summary>
    [DataField]
    public Vector2 SouthOffset = Vector2.Zero;

    [DataField]
    public Vector2 NorthOffset = Vector2.Zero;

    [DataField]
    public Vector2 EastOffset = Vector2.Zero;

    [DataField]
    public Vector2 WestOffset = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public EntityUid? Rider;
}
