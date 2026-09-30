using Content.Shared._Horizon.Plumbing;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.NodeContainer;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// Shares the liquid of a <see cref="PlumbingTankComponent"/> with its network, from the fuller side to the emptier
/// one, like communicating vessels.
/// </summary>
public sealed class PlumbingTankSystem : PlumbingTimedSystem<PlumbingTankComponent>
{
    [Dependency] private readonly PlumbingSystem _plumbing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;

    protected override void Tick(EntityUid uid, PlumbingTankComponent tank, NodeContainerComponent container,
        float seconds)
    {
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
