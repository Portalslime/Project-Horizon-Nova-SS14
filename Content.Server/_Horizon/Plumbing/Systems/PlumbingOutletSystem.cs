using Content.Shared._Horizon.Plumbing;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.NodeContainer;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// Keeps the solution of <see cref="PlumbingOutletComponent"/> owners topped up from their plumbing network.
/// </summary>
public sealed class PlumbingOutletSystem : PlumbingTimedSystem<PlumbingOutletComponent>
{
    [Dependency] private readonly PlumbingSystem _plumbing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingOutletComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<PlumbingOutletComponent> ent, ref ExaminedEvent args)
    {
        var connected = _plumbing.TryGetNet(ent, ent.Comp.NodeName, out _);
        args.PushMarkup(Loc.GetString(connected ? "plumbing-examine-connected" : "plumbing-examine-disconnected"));
    }

    protected override void Tick(EntityUid uid, PlumbingOutletComponent outlet, NodeContainerComponent container,
        float seconds)
    {
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
