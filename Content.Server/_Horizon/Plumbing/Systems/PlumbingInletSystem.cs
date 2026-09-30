using Content.Shared._Horizon.Plumbing;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.NodeContainer;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// Drains the buffer of <see cref="PlumbingInletComponent"/> owners into their plumbing network.
/// </summary>
public sealed class PlumbingInletSystem : PlumbingTimedSystem<PlumbingInletComponent>
{
    [Dependency] private readonly PlumbingSystem _plumbing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingInletComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<PlumbingInletComponent> ent, ref ExaminedEvent args)
    {
        var connected = _plumbing.TryGetNet(ent, ent.Comp.NodeName, out _);
        args.PushMarkup(Loc.GetString(connected ? "plumbing-examine-connected" : "plumbing-examine-disconnected"));
    }

    protected override void Tick(EntityUid uid, PlumbingInletComponent inlet, NodeContainerComponent container,
        float seconds)
    {
        if (!_solutions.TryGetSolution(uid, inlet.Solution, out var soln, out var solution) ||
            solution.Volume <= 0)
            return;

        var wanted = FixedPoint2.Min(solution.Volume, inlet.Rate * seconds);
        var accepted = FixedPoint2.Zero;

        if (_plumbing.TryGetNet(uid, inlet.NodeName, out var net, container))
        {
            accepted = FixedPoint2.Min(wanted, net.FreeSpace);
            if (accepted > 0)
                _plumbing.Deposit(net, _solutions.SplitSolution(soln.Value, accepted));
        }

        // Whatever the network refuses is pushed back out instead of being swallowed.
        var blocked = wanted - accepted;
        if (inlet.SpillWhenBlocked && blocked > 0)
            _plumbing.Spill(uid, _solutions.SplitSolution(soln.Value, blocked));
    }
}
