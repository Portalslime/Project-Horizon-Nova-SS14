using Content.Shared.Atmos;
using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Atmos;

/// <summary>
/// Continuously releases gases into the surrounding air. Handled by GasEmitterSystem.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class GasEmitterComponent : Component
{
    /// <summary>
    /// Moles of each gas released per second.
    /// </summary>
    [DataField]
    public Dictionary<Gas, float> Gases = new();

    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(2);

    [DataField, AutoPausedField]
    public TimeSpan NextUpdateTime;

    /// <summary>
    /// If set, the emitter stops (and is removed) after this time since it started.
    /// </summary>
    [DataField]
    public TimeSpan? Lifetime;

    [DataField, AutoPausedField]
    public TimeSpan? ExpireTime;
}
