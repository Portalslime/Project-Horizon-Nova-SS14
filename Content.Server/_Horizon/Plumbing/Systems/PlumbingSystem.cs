using System.Diagnostics.CodeAnalysis;
using Content.Server._Horizon.Plumbing.NodeGroups;
using Content.Server.Fluids.EntitySystems;
using Content.Shared._Horizon.Plumbing;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.NodeContainer;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// Moves liquid around the plumbing networks: fills them from inlets, pumps between them and feeds outlets.
/// Everything runs in one fixed-rate step instead of every tick, and only touches devices, never single pipes.
/// </summary>
public sealed class PlumbingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;

    /// <summary>
    /// Seconds between simulation steps.
    /// </summary>
    private const float StepSeconds = 1f;

    private float _accumulator;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingGaugeComponent, ExaminedEvent>(OnGaugeExamined);
        SubscribeLocalEvent<PlumbingPumpComponent, ExaminedEvent>(OnPumpExamined);
        SubscribeLocalEvent<PlumbingPumpComponent, ActivateInWorldEvent>(OnPumpActivated);
    }

    private void OnPumpExamined(Entity<PlumbingPumpComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.Enabled ? "plumbing-pump-examine-on" : "plumbing-pump-examine-off"));
    }

    private void OnPumpActivated(Entity<PlumbingPumpComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        ent.Comp.Enabled = !ent.Comp.Enabled;
        _popup.PopupEntity(Loc.GetString(ent.Comp.Enabled ? "plumbing-pump-enabled" : "plumbing-pump-disabled"),
            ent, args.User);
        args.Handled = true;
    }

    /// <summary>
    /// Finds the plumbing network connected to the named node of an entity.
    /// </summary>
    public bool TryGetNet(EntityUid uid, string nodeName, [NotNullWhen(true)] out PlumbingNet? net,
        NodeContainerComponent? container = null)
    {
        net = null;
        if (!Resolve(uid, ref container, false))
            return false;

        if (!container.Nodes.TryGetValue(nodeName, out var node))
            return false;

        net = node.NodeGroup as PlumbingNet;
        return net != null;
    }

    /// <summary>
    /// Puts liquid on the floor where the entity is.
    /// </summary>
    public void Spill(EntityUid at, Solution solution)
    {
        if (solution.Volume <= 0)
            return;

        _puddle.TrySpillAt(at, solution, out _);
    }

    /// <summary>
    /// Adds liquid to a network. The caller is responsible for checking the free space first.
    /// </summary>
    public void Deposit(PlumbingNet net, Solution solution)
    {
        net.Fluid.AddSolution(solution, _prototype);
    }

    /// <summary>
    /// Moves up to <paramref name="amount"/> from one network to another, limited by what the source holds and
    /// the free space at the destination. Returns how much was actually moved.
    /// </summary>
    public FixedPoint2 Transfer(PlumbingNet from, PlumbingNet to, FixedPoint2 amount)
    {
        if (ReferenceEquals(from, to))
            return FixedPoint2.Zero;

        var moved = FixedPoint2.Min(amount, FixedPoint2.Min(from.Fluid.Volume, to.FreeSpace));
        if (moved <= 0)
            return FixedPoint2.Zero;

        to.Fluid.AddSolution(from.Fluid.SplitSolution(moved), _prototype);
        return moved;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime;
        if (_accumulator < StepSeconds)
            return;

        // If the server lagged, do one step rather than replaying every missed one.
        _accumulator = 0f;

        StepInlets();
        StepPumps();
        StepOutlets();
    }

    private void StepInlets()
    {
        var query = EntityQueryEnumerator<PlumbingInletComponent, NodeContainerComponent>();
        while (query.MoveNext(out var uid, out var inlet, out var container))
        {
            if (!_solutions.TryGetSolution(uid, inlet.Solution, out var soln, out var solution) ||
                solution.Volume <= 0)
                continue;

            var wanted = FixedPoint2.Min(solution.Volume, inlet.Rate * StepSeconds);
            var accepted = FixedPoint2.Zero;

            if (TryGetNet(uid, inlet.NodeName, out var net, container))
            {
                accepted = FixedPoint2.Min(wanted, net.FreeSpace);
                if (accepted > 0)
                    net.Fluid.AddSolution(_solutions.SplitSolution(soln.Value, accepted), _prototype);
            }

            // Whatever the network refuses is pushed back out instead of being swallowed.
            var blocked = wanted - accepted;
            if (inlet.SpillWhenBlocked && blocked > 0)
                Spill(uid, _solutions.SplitSolution(soln.Value, blocked));
        }
    }

    private void StepPumps()
    {
        var query = EntityQueryEnumerator<PlumbingPumpComponent, NodeContainerComponent>();
        while (query.MoveNext(out var uid, out var pump, out var container))
        {
            if (!pump.Enabled)
                continue;

            if (!TryGetNet(uid, pump.InletNodeName, out var from, container) ||
                !TryGetNet(uid, pump.OutletNodeName, out var to, container))
                continue;

            Transfer(from, to, pump.Rate * StepSeconds);
        }
    }

    private void StepOutlets()
    {
        var query = EntityQueryEnumerator<PlumbingOutletComponent, NodeContainerComponent>();
        while (query.MoveNext(out var uid, out var outlet, out var container))
        {
            if (!_solutions.TryGetSolution(uid, outlet.Solution, out var soln, out var solution))
                continue;

            var room = solution.AvailableVolume;
            if (room <= 0)
                continue;

            if (!TryGetNet(uid, outlet.NodeName, out var net, container) || net.Fluid.Volume <= 0)
                continue;

            var amount = FixedPoint2.Min(FixedPoint2.Min(room, outlet.Rate * StepSeconds), net.Fluid.Volume);
            _solutions.TryAddSolution(soln.Value, net.Fluid.SplitSolution(amount));
        }
    }

    private void OnGaugeExamined(Entity<PlumbingGaugeComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryGetNet(ent, ent.Comp.NodeName, out var net))
        {
            args.PushMarkup(Loc.GetString("plumbing-gauge-disconnected"));
            return;
        }

        args.PushMarkup(Loc.GetString("plumbing-gauge-reading",
            ("percent", (int) MathF.Round(net.FillRatio * 100f)),
            ("volume", net.Fluid.Volume),
            ("capacity", net.Capacity)));
    }
}
