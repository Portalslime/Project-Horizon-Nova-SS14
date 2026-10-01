using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.SoundCues;

/// <summary>
/// Raised on an entity to play one of its <see cref="SoundCuesComponent"/> cues. Raise it for what happened, not for
/// what should be heard: the entity decides that, and one without the cue (or without sounds at all) stays silent.
/// </summary>
/// <param name="Cue">What happened.</param>
/// <param name="User">
/// The entity whose client predicted this and plays the sound itself, so the server does not send it there.
/// Null for what is raised on the server only.
/// </param>
[ByRefEvent]
public readonly record struct PlaySoundCueEvent(ProtoId<SoundCuePrototype> Cue, EntityUid? User = null);

/// <summary>
/// Raised on an entity to cut short the sound of a cue that is still playing, like chewing that was interrupted.
/// Nothing happens if the cue is not playing.
/// </summary>
[ByRefEvent]
public readonly record struct StopSoundCueEvent(ProtoId<SoundCuePrototype> Cue);
