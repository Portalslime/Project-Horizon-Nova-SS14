using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Horizon.SoundCues;

/// <summary>
/// Plays the sounds of <see cref="SoundCuesComponent"/> for <see cref="PlaySoundCueEvent"/> and stops them for
/// <see cref="StopSoundCueEvent"/>. A sound that cannot be played (the file or the collection does not exist) is
/// skipped with one warning in the log, it never throws.
/// </summary>
public sealed class SoundCueSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IResourceManager _resources = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <summary>
    /// What was reported already: a broken sound is one line in the log, not one for every play.
    /// </summary>
    private readonly HashSet<string> _reported = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SoundCuesComponent, PlaySoundCueEvent>(OnPlay);
        SubscribeLocalEvent<SoundCuesComponent, StopSoundCueEvent>(OnStop);
    }

    private void OnPlay(Entity<SoundCuesComponent> ent, ref PlaySoundCueEvent args)
    {
        // The server plays for everybody. A client joins in only for what it predicted itself, otherwise the
        // sound would be heard twice: its own and the one the server sends.
        if (_net.IsClient && args.User == null)
            return;

        if (!ent.Comp.Cues.TryGetValue(args.Cue, out var cue))
            return;

        var now = _timing.CurTime;
        if (ent.Comp.NextPlay.TryGetValue(args.Cue, out var next) && now < next)
            return;

        if (GetProblem(cue.Sound) is { } problem)
        {
            if (_reported.Add(problem))
                Log.Warning($"{ToPrettyString(ent)} stays silent for the cue {args.Cue}: {problem}");

            return;
        }

        ent.Comp.NextPlay[args.Cue] = now + cue.Cooldown;

        if (_audio.PlayPredicted(cue.Sound, ent, args.User) is { } audio)
            ent.Comp.Playing[args.Cue] = audio.Entity;
    }

    private void OnStop(Entity<SoundCuesComponent> ent, ref StopSoundCueEvent args)
    {
        if (ent.Comp.Playing.Remove(args.Cue, out var audio))
            _audio.Stop(audio);
    }

    /// <summary>
    /// Why the sound cannot be played, or null if it can. The same check is what the integration test runs over
    /// every cue of every prototype, so a missing file is found when the tests run and is only quiet in the game.
    /// </summary>
    public string? GetProblem(SoundSpecifier sound)
    {
        switch (sound)
        {
            case SoundPathSpecifier path:
                if (path.Path == default)
                    return "the path is not set";

                return _resources.ContentFileExists(path.Path) ? null : $"the file {path.Path} does not exist";

            case SoundCollectionSpecifier collection:
                if (collection.Collection == null)
                    return "the collection is not set";

                if (!_proto.TryIndex<SoundCollectionPrototype>(collection.Collection, out var prototype))
                    return $"the collection {collection.Collection} does not exist";

                if (prototype.PickFiles.Count == 0)
                    return $"the collection {collection.Collection} is empty";

                foreach (var file in prototype.PickFiles)
                {
                    if (!_resources.ContentFileExists(file))
                        return $"the file {file} of the collection {collection.Collection} does not exist";
                }

                return null;

            default:
                return "the kind of the sound is unknown";
        }
    }
}
