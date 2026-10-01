using Robust.Shared.Audio;

namespace Content.Shared._Horizon.SoundCues;

/// <summary>
/// What an entity sounds like for one cue, see <see cref="SoundCuesComponent"/>.
/// </summary>
[DataDefinition]
public sealed partial class SoundCueDef
{
    /// <summary>
    /// A single file or a collection to pick from. Volume, pitch and variation go to its <c>params</c>.
    /// </summary>
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    /// <summary>
    /// For how long after it played the cue stays silent: a repeat within this time is ignored.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown;
}
