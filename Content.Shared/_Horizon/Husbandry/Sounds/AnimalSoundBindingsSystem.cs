using Content.Shared._Horizon.Husbandry.Feeding;
using Content.Shared._Horizon.Husbandry.Production;
using Content.Shared._Horizon.Husbandry.Rideable;
using Content.Shared._Horizon.SoundCues;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Husbandry.Sounds;

/// <summary>
/// Turns what happens to an animal into sound cues. This is the only place where Husbandry meets the sound system:
/// the rules raise their own events and know nothing about sounds, and what is heard for a cue is up to the table of
/// the animal itself (<see cref="SoundCuesComponent"/>). An animal without the table, or without the cue in it, is silent.
/// </summary>
public sealed class AnimalSoundBindingsSystem : EntitySystem
{
    private static readonly ProtoId<SoundCuePrototype> Mounted = "Mounted";
    private static readonly ProtoId<SoundCuePrototype> EatStarted = "EatStarted";
    private static readonly ProtoId<SoundCuePrototype> Drank = "Drank";
    private static readonly ProtoId<SoundCuePrototype> Produced = "Produced";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SoundCuesComponent, RiderMountedEvent>(OnMounted);
        SubscribeLocalEvent<SoundCuesComponent, AnimalEatStartedEvent>(OnEatStarted);
        SubscribeLocalEvent<SoundCuesComponent, AnimalEatInterruptedEvent>(OnEatInterrupted);
        SubscribeLocalEvent<SoundCuesComponent, AnimalDrankEvent>(OnDrank);
        SubscribeLocalEvent<SoundCuesComponent, AnimalProducedEvent>(OnProduced);
    }

    // Mounting is predicted, so the rider's own client plays it and the server does not send it to them.
    private void OnMounted(Entity<SoundCuesComponent> ent, ref RiderMountedEvent args)
    {
        Play(ent, Mounted, args.Rider);
    }

    private void OnEatStarted(Entity<SoundCuesComponent> ent, ref AnimalEatStartedEvent args)
    {
        Play(ent, EatStarted);
    }

    // The chewing lasts as long as the meal, if the animal is interrupted it stops with it.
    private void OnEatInterrupted(Entity<SoundCuesComponent> ent, ref AnimalEatInterruptedEvent args)
    {
        var ev = new StopSoundCueEvent(EatStarted);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnDrank(Entity<SoundCuesComponent> ent, ref AnimalDrankEvent args)
    {
        Play(ent, Drank);
    }

    private void OnProduced(Entity<SoundCuesComponent> ent, ref AnimalProducedEvent args)
    {
        Play(ent, Produced);
    }

    private void Play(EntityUid animal, ProtoId<SoundCuePrototype> cue, EntityUid? user = null)
    {
        var ev = new PlaySoundCueEvent(cue, user);
        RaiseLocalEvent(animal, ref ev);
    }
}
