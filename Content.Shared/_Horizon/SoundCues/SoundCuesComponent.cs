using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.SoundCues;

/// <summary>
/// What an entity sounds like: a table from the name of a cue (what happened) to the sound it plays. Systems never
/// pick sounds, they raise <see cref="PlaySoundCueEvent"/> with the name and the entity decides what is heard.
/// An entity without the cue stays silent. Handled by SoundCueSystem.
/// </summary>
/// <remarks>
/// Not networked: like the sound files, the table is local, every client has it from the prototype. So declare it in
/// the prototype itself, a component added by the server at runtime is unknown to the client and its sounds that the
/// client predicts would be lost.
/// </remarks>
[RegisterComponent]
public sealed partial class SoundCuesComponent : Component
{
    [DataField(required: true)]
    public Dictionary<ProtoId<SoundCuePrototype>, SoundCueDef> Cues = new();

    /// <summary>
    /// When each cue may play again.
    /// </summary>
    [ViewVariables]
    public Dictionary<ProtoId<SoundCuePrototype>, TimeSpan> NextPlay = new();

    /// <summary>
    /// The sound each cue is playing right now, to be able to stop it.
    /// </summary>
    [ViewVariables]
    public Dictionary<ProtoId<SoundCuePrototype>, EntityUid> Playing = new();
}
