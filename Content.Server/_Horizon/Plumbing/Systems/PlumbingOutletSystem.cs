using Content.Shared._Horizon.Plumbing;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.NodeContainer;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// Keeps the solution of <see cref="PlumbingOutletComponent"/> owners topped up from their plumbing network.
/// </summary>
public sealed class PlumbingOutletSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlumbingSystem _plumbing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingOutletComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PlumbingOutletComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<PlumbingOutletComponent> ent, ref ExaminedEvent args)
    {
        var connected = _plumbing.TryGetNet(ent, ent.Comp.NodeName, out _);
        args.PushMarkup(Loc.GetString(connected ? "plumbing-examine-connected" : "plumbing-examine-disconnected"));
    }

    private void OnMapInit(Entity<PlumbingOutletComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval * _random.NextFloat();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<PlumbingOutletComponent, NodeContainerComponent>();
        while (query.MoveNext(out var uid, out var outlet, out var container))
        {
            if (curTime < outlet.NextUpdate)
                continue;

            outlet.NextUpdate = curTime + outlet.UpdateInterval;
            Fill((uid, outlet, container), (float) outlet.UpdateInterval.TotalSeconds);
        }
    }

    private void Fill(Entity<PlumbingOutletComponent, NodeContainerComponent> ent, float seconds)
    {
        var (uid, outlet, container) = ent;

        if (!_solutions.TryGetSolution(uid, outlet.Solution, out var soln, out var solution))
            return;

        var room = solution.AvailableVolume;
        if (room <= 0)
            return;

        if (!_plumbing.TryGetNet(uid, outlet.NodeName, out var net, container) || net.Fluid.Volume <= 0)
            return;

        var amount = FixedPoint2.Min(FixedPoint2.Min(room, outlet.Rate * seconds), net.Fluid.Volume);
        _solutions.TryAddSolution(soln.Value, net.Fluid.SplitSolution(amount));
    }
}
