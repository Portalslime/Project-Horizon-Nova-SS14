namespace Content.Shared._Horizon.Plumbing;

/// <summary>
/// Marks the entity that holds the liquid of one plumbing network. It carries a regular
/// <see cref="Content.Shared.Chemistry.Components.SolutionComponent"/>, so the liquid in the pipes reacts, overflows
/// and is looked at exactly like the liquid of any other container. The entity is created and deleted together with its
/// network by PlumbingNet and stands where the network was first built, which is where the effects of reactions happen.
/// </summary>
[RegisterComponent]
public sealed partial class PlumbingNetSolutionComponent : Component;
