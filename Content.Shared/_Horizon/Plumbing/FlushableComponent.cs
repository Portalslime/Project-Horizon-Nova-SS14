using Robust.Shared.Audio;

namespace Content.Shared._Horizon.Plumbing;

/// <summary>
/// Can be flushed: the whole buffer is sent into the plumbing network in one go. Whatever the network cannot take
/// (full, or not connected at all) is spilled on the floor. Handled by FlushableSystem.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class FlushableComponent : Component
{
    /// <summary>
    /// The solution that gets flushed.
    /// </summary>
    [DataField]
    public string Solution = "drainBuffer";

    /// <summary>
    /// The plumbing node of the NodeContainer the flush goes through.
    /// </summary>
    [DataField]
    public string NodeName = "plumbing";

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/flush.ogg");

    /// <summary>
    /// Minimum time between two flushes.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(3);

    [DataField, AutoPausedField]
    public TimeSpan NextFlush;
}
