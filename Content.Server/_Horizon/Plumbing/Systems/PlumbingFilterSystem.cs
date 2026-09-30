using Content.Shared._Horizon.Plumbing;
using Content.Shared.FixedPoint;
using Content.Shared.NodeContainer;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// Splits the liquid at the inlet of a <see cref="PlumbingFilterComponent"/>: the chosen reagent goes to the
/// filtered side, everything else to the outlet. Also serves the window that chooses the reagent.
/// </summary>
public sealed class PlumbingFilterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlumbingSystem _plumbing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingFilterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PlumbingFilterComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<PlumbingFilterComponent, PlumbingFilterSelectReagentMessage>(OnSelectReagent);
        SubscribeLocalEvent<PlumbingFilterComponent, PlumbingFilterToggleMessage>(OnToggle);
    }

    private void OnMapInit(Entity<PlumbingFilterComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval * _random.NextFloat();
    }

    private void OnUiOpened(Entity<PlumbingFilterComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnSelectReagent(Entity<PlumbingFilterComponent> ent, ref PlumbingFilterSelectReagentMessage args)
    {
        ent.Comp.Reagent = args.Reagent;
        UpdateUi(ent);
    }

    private void OnToggle(Entity<PlumbingFilterComponent> ent, ref PlumbingFilterToggleMessage args)
    {
        ent.Comp.Enabled = args.Enabled;
        UpdateUi(ent);
    }

    private void UpdateUi(Entity<PlumbingFilterComponent> ent)
    {
        _ui.SetUiState(ent.Owner, PlumbingFilterUiKey.Key,
            new PlumbingFilterBoundUserInterfaceState(ent.Comp.Enabled, ent.Comp.Reagent));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<PlumbingFilterComponent, NodeContainerComponent>();
        while (query.MoveNext(out var uid, out var filter, out var container))
        {
            if (curTime < filter.NextUpdate)
                continue;

            filter.NextUpdate = curTime + filter.UpdateInterval;

            if (filter.Enabled)
                Split((uid, filter, container), (float) filter.UpdateInterval.TotalSeconds);
        }
    }

    private void Split(Entity<PlumbingFilterComponent, NodeContainerComponent> ent, float seconds)
    {
        var (uid, filter, container) = ent;

        if (!_plumbing.TryGetNet(uid, filter.InletNodeName, out var inlet, container) || inlet.Fluid.Volume <= 0)
            return;

        var budget = filter.Rate * seconds;

        // The chosen reagent goes sideways.
        if (filter.Reagent is { } reagent &&
            _plumbing.TryGetNet(uid, filter.FilteredNodeName, out var filtered, container))
        {
            var wanted = FixedPoint2.Min(budget, inlet.Fluid.GetTotalPrototypeQuantity(reagent));
            var amount = FixedPoint2.Min(wanted, filtered.FreeSpace);
            if (amount > 0)
            {
                _plumbing.Deposit(filtered, inlet.Fluid.SplitSolutionWithOnly(amount, reagent));
                budget -= amount;
            }
        }

        // Everything else goes on. With nothing chosen that is all of it.
        if (budget <= 0 || !_plumbing.TryGetNet(uid, filter.OutletNodeName, out var outlet, container))
            return;

        var rest = filter.Reagent is { } excluded
            ? inlet.Fluid.SplitSolutionWithout(FixedPoint2.Min(budget, outlet.FreeSpace), excluded)
            : inlet.Fluid.SplitSolution(FixedPoint2.Min(budget, outlet.FreeSpace));
        _plumbing.Deposit(outlet, rest);
    }
}
