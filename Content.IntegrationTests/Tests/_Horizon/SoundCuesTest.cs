#nullable enable
using System.Collections.Generic;
using System.Numerics;
using Content.IntegrationTests.Tests.Movement;
using Content.Server._Horizon.Husbandry.Feeding;
using Content.Server.NPC.HTN;
using Content.Shared._Horizon.SoundCues;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._Horizon;

/// <summary>
/// Sound cues: that every sound of every cue exists, that a missing one stays silent instead of failing, and that
/// a horse chews while it eats and stops when it is interrupted.
/// </summary>
public sealed class SoundCuesTest : MovementTest
{
    protected override int Tiles => 10;

    private void Raise(EntityUid uid, string cue)
    {
        var ev = new PlaySoundCueEvent(cue);
        SEntMan.EventBus.RaiseLocalEvent(uid, ref ev);
    }

    private int CountPlaying(string fileNamePart)
    {
        var count = 0;
        var query = SEntMan.EntityQueryEnumerator<AudioComponent>();
        while (query.MoveNext(out _, out var audio))
        {
            // The sandbox lets a test read the field but not call anything on it directly.
            var fileName = audio.FileName;
            if (fileName.Contains(fileNamePart))
                count++;
        }

        return count;
    }

    /// <summary>
    /// In the game a missing sound is only silent (see <see cref="MissingSoundStaysSilent"/>), this is where it is noticed.
    /// </summary>
    [Test]
    public async Task EveryCueCanBePlayed()
    {
        await Server.WaitAssertion(() =>
        {
            var system = SEntMan.System<SoundCueSystem>();
            var tables = 0;
            var problems = new List<string>();

            foreach (var proto in ProtoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (!proto.TryGetComponent<SoundCuesComponent>(out var table, SEntMan.ComponentFactory))
                    continue;

                tables++;
                foreach (var (cue, def) in table.Cues)
                {
                    if (system.GetProblem(def.Sound) is { } problem)
                        problems.Add($"{proto.ID}, cue {cue}: {problem}");
                }
            }

            Assert.That(tables, Is.GreaterThan(0), "no entity has a SoundCues table, is the search broken?");
            Assert.That(problems, Is.Empty, string.Join('\n', problems));
        });
    }

    /// <summary>
    /// A cue whose sound does not exist, and a cue that is not in the table at all, play nothing and throw nothing.
    /// </summary>
    [Test]
    public async Task MissingSoundStaysSilent()
    {
        // The warning about the missing sound is the point here, in the other tests it would fail them.
        var config = Server.CfgMan;
        var failureLevel = config.GetCVar(RTCVars.FailureLogLevel);
        config.SetCVar(RTCVars.FailureLogLevel, LogLevel.Error);

        try
        {
            await Server.WaitAssertion(() =>
            {
                var entity = SEntMan.SpawnEntity(null, Transform.GetMapCoordinates(SPlayer));
                var table = SEntMan.AddComponent<SoundCuesComponent>(entity);
                table.Cues["TestNoSuchCollection"] = new SoundCueDef { Sound = new SoundCollectionSpecifier("SoundCuesTestNoSuchCollection") };
                table.Cues["TestNoSuchFile"] = new SoundCueDef { Sound = new SoundPathSpecifier("/Audio/_Horizon/Animals/no_such_sound.ogg") };
                table.Cues["TestWorks"] = new SoundCueDef { Sound = new SoundPathSpecifier("/Audio/_Horizon/Animals/horse_eat_1.ogg") };

                var before = SEntMan.Count<AudioComponent>();

                foreach (var cue in new[] { "TestNoSuchCollection", "TestNoSuchFile", "TestNotInTheTable" })
                {
                    Assert.DoesNotThrow(() => Raise(entity, cue), cue);
                }

                Assert.That(SEntMan.Count<AudioComponent>(), Is.EqualTo(before), "something was played for a sound that is missing");

                Raise(entity, "TestWorks");
                Assert.That(SEntMan.Count<AudioComponent>(), Is.EqualTo(before + 1), "the cue with a working sound played nothing");
            });
        }
        finally
        {
            config.SetCVar(RTCVars.FailureLogLevel, failureLevel);
        }
    }

    /// <summary>
    /// The chewing starts with the meal and is cut short with it when the horse is interrupted.
    /// </summary>
    [Test]
    public async Task ChewingStopsWhenTheMealIsInterrupted()
    {
        await SpawnTarget("MobHorse");
        var horse = STarget!.Value;
        var food = EntityUid.Invalid;

        await Server.WaitPost(() =>
        {
            // Its own brain would walk about and break off the meal.
            SEntMan.System<HTNSystem>().SetHTNEnabled((horse, SEntMan.GetComponent<HTNComponent>(horse)), false);
            food = SEntMan.SpawnEntity("FoodApple", Transform.GetMapCoordinates(horse));
        });
        await RunTicks(2);

        await Server.WaitAssertion(() =>
            Assert.That(SEntMan.System<AnimalFeedingSystem>().TryEat(horse, food, out _), Is.True, "the horse would not eat"));
        await RunTicks(2);
        Assert.That(CountPlaying("horse_eat_"), Is.EqualTo(1), "no chewing at the start of the meal");

        // Taken away from the food, the meal breaks off and so does the chewing.
        await Server.WaitPost(() => Transform.SetWorldPosition(horse, Transform.GetWorldPosition(horse) + new Vector2(1, 0)));
        await RunTicks(5);
        Assert.That(CountPlaying("horse_eat_"), Is.Zero, "the chewing went on after the meal was interrupted");
    }
}
