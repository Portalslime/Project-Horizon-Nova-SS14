using Content.Shared._Horizon.Plumbing;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.NodeContainer;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// Shares the liquid of a <see cref="PlumbingTankComponent"/> with its network, from the fuller side to the emptier
/// one, like communicating vessels.
/// </summary>
public sealed class PlumbingTankSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlumbingSystem _plumbing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingTankComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<PlumbingTankComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval * _random.NextFloat();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<PlumbingTankComponent, NodeContainerComponent>();
        while (query.MoveNext(out var uid, out var tank, out var container))
        {
            if (curTime < tank.NextUpdate)
                continue;

            tank.NextUpdate = curTime + tank.UpdateInterval;
            Balance((uid, tank, container), (float) tank.UpdateInterval.TotalSeconds);
        }
    }

    private void Balance(Entity<PlumbingTankComponent, NodeContainerComponent> ent, float seconds)
    {
        var (uid, tank, container) = ent;

        if (!_plumbing.TryGetNet(uid, tank.NodeName, out var net, container) ||
            !_solutions.TryGetSolution(uid, tank.Solution, out var soln, out var solution) ||
            solution.MaxVolume <= 0 || net.Capacity <= 0)
            return;

        // Move enough to meet halfway, but never more than the rate, what the source holds or the target has room for.
        var tankFill = solution.Volume.Float() / solution.MaxVolume.Float();
        var difference = tankFill - net.FillRatio;
        var budget = tank.Rate * seconds;

        if (difference > 0)
        {
            var toMove = FixedPoint2.Min(FixedPoint2.New(difference * solution.MaxVolume.Float() / 2f), budget);
            toMove = FixedPoint2.Min(toMove, FixedPoint2.Min(solution.Volume, net.FreeSpace));
            if (toMove > 0)
                _plumbing.Deposit(net, _solutions.SplitSolution(soln.Value, toMove));
        }
        else if (difference < 0)
        {
            var toMove = FixedPoint2.Min(FixedPoint2.New(-difference * net.Capacity.Float() / 2f), budget);
            toMove = FixedPoint2.Min(toMove, FixedPoint2.Min(net.Fluid.Volume, solution.AvailableVolume));
            if (toMove > 0)
                _solutions.TryAddSolution(soln.Value, net.Fluid.SplitSolution(toMove));
        }
    }
}
