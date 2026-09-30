#nullable enable
using System.Linq;
using System.Numerics;
using System.Text;
using Content.IntegrationTests.Tests.Movement;
using Content.Server._Horizon.Husbandry.Wander;
using Content.Server.NPC.HTN;
using Content.Shared._Horizon.Husbandry.Needs;
using Content.Shared._Horizon.Husbandry.Wander;
using Content.Shared._Horizon.Husbandry.Production;
using Content.Shared._Horizon.Husbandry.Rideable;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Doors;
using Content.Shared.Doors.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Hands.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._Horizon;

/// <summary>
/// Riding a horse: climbing on, moving, taking the saddle off and who can be saddled at all.
/// </summary>
public sealed class HorseRideTest : MovementTest
{
    protected override int Tiles => 25;

    private async Task<EntityUid> Mount()
    {
        await SpawnTarget("MobHorse");
        var horse = STarget!.Value;
        var saddle = EntityUid.Invalid;

        await Server.WaitPost(() =>
        {
            saddle = SEntMan.SpawnEntity("HorseSaddle", Transform.GetMapCoordinates(horse));
            var slots = SEntMan.System<ItemSlotsSystem>();
            Assert.That(slots.TryInsert(horse, "saddle_slot", saddle, null), Is.True, "saddle did not go in");
        });

        await RunTicks(5);

        await Server.WaitAssertion(() =>
        {
            var buckle = SEntMan.System<SharedBuckleSystem>();
            Assert.That(buckle.TryBuckle(SPlayer, SPlayer, horse), Is.True, "could not climb on the horse");
        });

        await RunTicks(5);
        return horse;
    }

    [Test]
    public async Task RideStraight()
    {
        var horse = await Mount();
        var xform = Transform;
        var cxform = CEntMan.System<SharedTransformSystem>();
        var cHorse = ToClient(horse);

        var sb = new StringBuilder();
        var last = xform.GetWorldPosition(horse);
        var reversals = 0;
        var maxGap = 0f;
        var moved = 0f;

        await SetMovementKey(DirectionFlag.East, BoundKeyState.Down);
        for (var i = 0; i < 90; i++)
        {
            await RunTicks(1);
            var s = xform.GetWorldPosition(horse);
            var c = cxform.GetWorldPosition(cHorse);
            var dx = s.X - last.X;
            if (i > 5 && dx < -0.0001f)
                reversals++;
            moved += dx;
            maxGap = Math.Max(maxGap, Vector2.Distance(s, c));
            sb.AppendLine($"t{i}: server=({s.X:F3},{s.Y:F3}) client=({c.X:F3},{c.Y:F3}) dx={dx:F3} rot={xform.GetWorldRotation(horse).Degrees:F1}");
            last = s;
        }
        await SetMovementKey(DirectionFlag.East, BoundKeyState.Up);
        await RunTicks(1);

        TestContext.Out.WriteLine(sb.ToString());
        TestContext.Out.WriteLine($"moved={moved:F2} reversals={reversals} maxServerClientGap={maxGap:F3}");
        TestContext.Out.WriteLine($"buckled={Comp<BuckleComponent>(Player).Buckled}");

        Assert.That(Comp<BuckleComponent>(Player).Buckled, Is.True, "rider fell off");
        Assert.That(moved, Is.GreaterThan(3f), "horse barely moved");
        Assert.That(reversals, Is.EqualTo(0), "horse went backwards");
        Assert.That(maxGap, Is.LessThan(0.5f), "client and server disagree");
    }

    [Test]
    public async Task PullingLightensTheHorse()
    {
        await SpawnTarget("MobHorse");
        var horse = STarget!.Value;
        var pulling = SEntMan.System<PullingSystem>();
        var physics = SEntMan.GetComponent<PhysicsComponent>(horse);
        var heavy = physics.Mass;

        await Server.WaitAssertion(() => Assert.That(pulling.TryStartPull(SPlayer, horse), Is.True, "could not pull the horse"));
        await RunTicks(2);
        Assert.That(physics.Mass, Is.LessThan(heavy / 2f), "the horse is as heavy as before");

        await Server.WaitAssertion(() => Assert.That(pulling.TryStopPull(horse, SEntMan.GetComponent<PullableComponent>(horse)), Is.True));
        await RunTicks(2);
        Assert.That(physics.Mass, Is.EqualTo(heavy).Within(0.01f), "the horse did not get its weight back");
    }

    [Test]
    public async Task HorseWalksToAPlaceInOneGo()
    {
        await SpawnTarget("MobHorse");
        var horse = STarget!.Value;
        var xform = Transform;
        var wander = SEntMan.System<AnimalWanderSystem>();

        // Its own brain stays out of this, only the walk itself is looked at.
        await Server.WaitPost(() =>
        {
            SEntMan.System<HTNSystem>().SetHTNEnabled((horse, SEntMan.GetComponent<HTNComponent>(horse)), false);
            var comp = SEntMan.GetComponent<AnimalWanderComponent>(horse);
            comp.WalkChance = 1f;
            wander.StartOrStand((horse, comp));
        });
        Assert.That(wander.IsWalking((horse, SEntMan.GetComponent<AnimalWanderComponent>(horse))), Is.True,
            "the horse found no place to walk to in an empty room");

        var start = xform.GetWorldPosition(horse);
        var last = start;
        var lastStep = Vector2.Zero;
        var reversals = 0;
        var travelled = 0f;

        for (var i = 0; i < 3000 && wander.IsWalking((horse, SEntMan.GetComponent<AnimalWanderComponent>(horse))); i++)
        {
            await RunTicks(1);
            var now = xform.GetWorldPosition(horse);
            var step = now - last;

            // The legs turn a bit, but the horse never turns around.
            if (step.Length() > 0.01f)
            {
                if (lastStep != Vector2.Zero && Vector2.Dot(step, lastStep) < 0f)
                    reversals++;
                lastStep = step;
            }

            travelled += step.Length();
            last = now;
        }

        Assert.That(wander.IsWalking((horse, SEntMan.GetComponent<AnimalWanderComponent>(horse))), Is.False, "the stroll never ended");
        Assert.That(reversals, Is.EqualTo(0), "the horse went back and forth");
        Assert.That(travelled, Is.GreaterThan(6f), "the horse barely walked, a stroll is several legs");
    }

    [Test]
    public async Task HungryHorseWalksToFoodAndEatsIt()
    {
        await SpawnTarget("MobHorse");
        var horse = STarget!.Value;
        var xform = Transform;
        var apple = EntityUid.Invalid;
        var start = Vector2.Zero;

        await Server.WaitPost(() =>
        {
            var position = xform.GetMapCoordinates(horse);
            start = position.Position;
            apple = SEntMan.SpawnEntity("FoodApple", new MapCoordinates(position.Position + new Vector2(6f, 0f), position.MapId));

            // Hungry enough to look for food, the rest of the brain is the usual one.
            SEntMan.GetComponent<AnimalNeedsComponent>(horse).Satiety.Value = 10f;
        });

        // The way there is walked by the horse itself and then it eats, a minute of game time is plenty.
        for (var i = 0; i < 3600 && !SEntMan.Deleted(apple); i++)
        {
            await RunTicks(1);
        }

        Assert.That(SEntMan.Deleted(apple), Is.True, "the horse never ate the apple");
        Assert.That(Vector2.Distance(start, xform.GetWorldPosition(horse)), Is.GreaterThan(3f), "the horse did not walk to it");
    }

    [Test]
    public async Task PulledHorseFacesThePuller()
    {
        await SpawnTarget("MobHorse");
        var horse = STarget!.Value;
        var xform = Transform;
        var pulling = SEntMan.System<PullingSystem>();

        // Start with the horse turned away from the player.
        await Server.WaitPost(() =>
        {
            var toPlayer = Angle.FromWorldVec(xform.GetWorldPosition(SPlayer) - xform.GetWorldPosition(horse));
            xform.SetWorldRotation(horse, toPlayer + Angle.FromDegrees(180));
            Assert.That(pulling.TryStartPull(SPlayer, horse), Is.True, "could not pull the horse");
        });
        await RunTicks(60);

        var expected = Angle.FromWorldVec(xform.GetWorldPosition(SPlayer) - xform.GetWorldPosition(horse));
        var diff = Math.Abs(Angle.ShortestDistance(xform.GetWorldRotation(horse), expected).Degrees);
        Assert.That(diff, Is.LessThan(15), "the horse does not face the one who pulls it");
    }

    [Test]
    public async Task RiderDoesNotPushTheHorse()
    {
        var horse = await Mount();
        var pushed = 0;

        await SetMovementKey(DirectionFlag.East, BoundKeyState.Down);
        for (var i = 0; i < 60; i++)
        {
            await RunTicks(1);
            if (SEntMan.GetComponent<MobCollisionComponent>(horse).Colliding)
                pushed++;
        }
        await SetMovementKey(DirectionFlag.East, BoundKeyState.Up);

        Assert.That(pushed, Is.EqualTo(0), "the horse was pushed by its own rider for this many ticks");
    }

    [Test]
    public async Task ClientPredictsTheRiddenHorse()
    {
        var horse = await Mount();
        var cHorse = ToClient(horse);
        await RunTicks(5);

        Assert.That(CEntMan.GetComponent<PhysicsComponent>(cHorse).Predict, Is.True, "the client does not predict the horse");
    }

    [Test]
    public async Task ReinsDoNotTakeTheActiveHand()
    {
        await Mount();

        // The active hand stays free for doors and the like, the reins are in the other one.
        var hands = SEntMan.GetComponent<HandsComponent>(SPlayer);
        Assert.That(hands.ActiveHandId, Is.Not.Null);
        Assert.That(HandSys.TryGetHeldItem(SPlayer, hands.ActiveHandId!, out _), Is.False, "the active hand holds the reins");
        Assert.That(hands.Hands.Keys.Where(id => HandSys.TryGetHeldItem(SPlayer, id, out _)).Count(), Is.EqualTo(1), "the reins are not in a hand");
    }

    [Test]
    public async Task DoorOpensWhenRiddenIntoIt()
    {
        var horse = await Mount();
        EntityUid door = default;
        await Server.WaitPost(() =>
        {
            var pos = Transform.GetMapCoordinates(horse);
            door = SEntMan.SpawnEntity("Airlock", new MapCoordinates(pos.Position + new Vector2(3, 0), pos.MapId));
        });
        await RunTicks(5);
        await ToggleNeedPower(SEntMan.GetNetEntity(door));

        var opened = false;
        await SetMovementKey(DirectionFlag.East, BoundKeyState.Down);
        for (var i = 0; i < 40; i++)
        {
            await RunTicks(1);
            opened |= SEntMan.GetComponent<DoorComponent>(door).State is DoorState.Open or DoorState.Opening;
        }
        await SetMovementKey(DirectionFlag.East, BoundKeyState.Up);

        Assert.That(opened, Is.True, "the door did not open for a rider bumping into it");
    }

    [Test]
    public async Task HorseWithoutRiderDoesNotOpenDoors()
    {
        await SpawnTarget("MobHorse");
        var horse = STarget!.Value;
        Assert.That(SEntMan.System<Content.Shared.Tag.TagSystem>().HasTag(horse, SharedDoorSystem.DoorBumpTag), Is.False);

        // With a rider the tag shows up and goes away with them.
        await Server.WaitPost(() =>
        {
            var saddle = SEntMan.SpawnEntity("HorseSaddle", Transform.GetMapCoordinates(horse));
            SEntMan.System<ItemSlotsSystem>().TryInsert(horse, "saddle_slot", saddle, null);
        });
        await RunTicks(5);
        await Server.WaitAssertion(() => Assert.That(SEntMan.System<SharedBuckleSystem>().TryBuckle(SPlayer, SPlayer, horse), Is.True));
        await RunTicks(5);
        Assert.That(SEntMan.System<Content.Shared.Tag.TagSystem>().HasTag(horse, SharedDoorSystem.DoorBumpTag), Is.True);

        await Server.WaitPost(() => SEntMan.System<SharedBuckleSystem>().Unbuckle(SPlayer, null));
        await RunTicks(5);
        Assert.That(SEntMan.System<Content.Shared.Tag.TagSystem>().HasTag(horse, SharedDoorSystem.DoorBumpTag), Is.False);
    }

    [Test]
    public async Task SaddleRemovedWhileRiding()
    {
        var horse = await Mount();
        var saddleSlots = SEntMan.System<ItemSlotsSystem>();
        EntityUid saddle = EntityUid.Invalid;

        await Server.WaitPost(() =>
        {
            saddle = saddleSlots.GetItemOrNull(horse, "saddle_slot")!.Value;
            // The slot is locked while somebody rides.
            Assert.That(saddleSlots.TryEject(horse, "saddle_slot", SPlayer, out _), Is.False, "saddle came off a ridden horse");
        });
        Assert.That(Comp<BuckleComponent>(Player).Buckled, Is.True);

        // Whatever takes the saddle away by force, the rider gets off.
        await Server.WaitPost(() => SEntMan.System<SharedContainerSystem>().Remove(saddle, SEntMan.GetComponent<ItemSlotsComponent>(horse).Slots["saddle_slot"].ContainerSlot!));
        await RunTicks(5);
        Assert.That(Comp<BuckleComponent>(Player).Buckled, Is.False, "rider is still sitting");
        Assert.That(SEntMan.HasComponent<RiderComponent>(SPlayer), Is.False);
        Assert.That(SEntMan.GetComponent<RideableComponent>(horse).Rider, Is.Null);
    }

    [Test]
    public async Task FoalAndYoung()
    {
        foreach (var (proto, health, rideable, seekBelow, decay, manure) in new[]
                 {
                     ("MobHorseFoal", 80, false, 50f, 1f, 0.25f),
                     ("MobHorseYoung", 140, false, 50f, 1.5f, 0.5f),
                     ("MobHorse", 200, true, 90f, 2.5f, 1f),
                 })
        {
            var horse = EntityUid.Invalid;
            await Server.WaitPost(() => horse = SEntMan.SpawnEntity(proto, Transform.GetMapCoordinates(SPlayer)));
            await RunTicks(2);

            await Server.WaitAssertion(() =>
            {
                var dead = SEntMan.System<MobThresholdSystem>().GetThresholdForState(horse, MobState.Dead);
                TestContext.Out.WriteLine($"{proto}: name={SEntMan.GetComponent<MetaDataComponent>(horse).EntityName} dead at {dead} rideable={SEntMan.HasComponent<RideableComponent>(horse)} slots={SEntMan.HasComponent<ItemSlotsComponent>(horse)}");
                Assert.That((float) dead, Is.EqualTo(health), proto);
                Assert.That(SEntMan.HasComponent<RideableComponent>(horse), Is.EqualTo(rideable), proto);
                Assert.That(SEntMan.HasComponent<ItemSlotsComponent>(horse), Is.EqualTo(rideable), proto);
                Assert.That(SEntMan.HasComponent<StrapComponent>(horse), Is.EqualTo(rideable), proto);

                // A grown horse looks for food sooner, eats more and leaves more manure for it.
                var satiety = SEntMan.GetComponent<AnimalNeedsComponent>(horse).Satiety;
                Assert.That(satiety.SeekBelow, Is.EqualTo(seekBelow), proto);
                Assert.That(satiety.DecayPerMinute, Is.EqualTo(decay), proto);
                Assert.That(SEntMan.GetComponent<ManureProducerComponent>(horse).UnitsPerNutrition, Is.EqualTo(manure), proto);
            });
        }
    }
}
