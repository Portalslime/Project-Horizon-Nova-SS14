using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.SoundCues;

/// <summary>
/// The name of something an entity can make a sound for, like being mounted or eating. Only a name: what is heard
/// for it is up to the <see cref="SoundCuesComponent"/> of each entity. It is a prototype, so the YAML linter catches
/// a misspelled cue.
/// </summary>
[Prototype]
public sealed partial class SoundCuePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
