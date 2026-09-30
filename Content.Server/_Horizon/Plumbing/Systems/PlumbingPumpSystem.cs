using Content.Shared._Horizon.Plumbing;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.NodeContainer;
using Content.Shared.Popups;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// Moves liquid from the network at the inlet of a <see cref="PlumbingPumpComponent"/> to the one at its outlet.
/// </summary>
public sealed class PlumbingPumpSystem : PlumbingTimedSystem<PlumbingPumpComponent>
{
    [Dependency] private readonly PlumbingSystem _plumbing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingPumpComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PlumbingPumpComponent, ActivateInWorldEvent>(OnActivated);
    }

    private void OnExamined(Entity<PlumbingPumpComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.Enabled ? "plumbing-pump-examine-on" : "plumbing-pump-examine-off"));
    }

    private void OnActivated(Entity<PlumbingPumpComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        ent.Comp.Enabled = !ent.Comp.Enabled;
        _popup.PopupEntity(Loc.GetString(ent.Comp.Enabled ? "plumbing-pump-enabled" : "plumbing-pump-disabled"),
            ent, args.User);
        args.Handled = true;
    }

    protected override void Tick(EntityUid uid, PlumbingPumpComponent pump, NodeContainerComponent container,
        float seconds)
    {
        if (!pump.Enabled ||
            !_plumbing.TryGetNet(uid, pump.InletNodeName, out var from, container) ||
            !_plumbing.TryGetNet(uid, pump.OutletNodeName, out var to, container))
            return;

        _plumbing.Transfer(from, to, pump.Rate * seconds);
    }
}
