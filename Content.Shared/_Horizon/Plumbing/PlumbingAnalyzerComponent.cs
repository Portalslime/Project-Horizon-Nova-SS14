namespace Content.Shared._Horizon.Plumbing;

/// <summary>
/// A handheld device that reports the pressure (how full the network is) and the contents of the plumbing network
/// of whatever it is used on. Handled by PlumbingAnalyzerSystem.
/// </summary>
[RegisterComponent]
public sealed partial class PlumbingAnalyzerComponent : Component;
