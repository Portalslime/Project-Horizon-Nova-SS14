using Content.Shared._Horizon.Plumbing;
using Content.Shared.FixedPoint;
using Content.Shared.NodeContainer;
using Robust.Server.GameObjects;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// Splits the liquid at the inlet of a <see cref="PlumbingFilterComponent"/>: the chosen reagent goes to the
/// filtered side, everything else to the outlet. Also serves the window that chooses the reagent.
/// </summary>
public sealed class PlumbingFilterSystem : PlumbingTimedSystem<PlumbingFilterComponent>
{
    [Dependency] private readonly PlumbingSystem _plumbing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingFilterComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<PlumbingFilterComponent, PlumbingFilterSelectReagentMessage>(OnSelectReagent);
        SubscribeLocalEvent<PlumbingFilterComponent, PlumbingFilterToggleMessage>(OnToggle);
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

    protected override void Tick(EntityUid uid, PlumbingFilterComponent filter, NodeContainerComponent container,
        float seconds)
    {
        if (!filter.Enabled)
            return;

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
