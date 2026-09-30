using Content.Shared._Horizon.Plumbing;
using Content.Shared.NodeContainer;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// The base of the systems of <see cref="IPlumbingTimed"/> devices: it spreads the devices over their interval, so
/// that they do not all work on the same tick, and calls <see cref="Tick"/> once per interval for each of them.
/// </summary>
public abstract class PlumbingTimedSystem<T> : EntitySystem where T : Component, IPlumbingTimed
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<T> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = Timing.CurTime + ent.Comp.UpdateInterval * Random.NextFloat();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = Timing.CurTime;
        var query = EntityQueryEnumerator<T, NodeContainerComponent>();
        while (query.MoveNext(out var uid, out var device, out var container))
        {
            if (curTime < device.NextUpdate)
                continue;

            device.NextUpdate = curTime + device.UpdateInterval;
            Tick(uid, device, container, (float) device.UpdateInterval.TotalSeconds);
        }
    }

    /// <summary>
    /// Does the work of one interval.
    /// </summary>
    /// <param name="seconds">The length of the interval, to scale rates by.</param>
    protected abstract void Tick(EntityUid uid, T device, NodeContainerComponent container, float seconds);
}
