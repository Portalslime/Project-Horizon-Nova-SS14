namespace Content.Shared._Horizon.Plumbing;

/// <summary>
/// Stops plumbing pieces from being anchored on top of another one that uses the same connection direction, so two
/// pipes can never share an opening. Handled by PlumbingRestrictOverlapSystem.
/// </summary>
[RegisterComponent]
public sealed partial class PlumbingRestrictOverlapComponent : Component;
