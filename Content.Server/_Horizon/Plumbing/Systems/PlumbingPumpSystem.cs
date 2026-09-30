using Content.Shared._Horizon.Plumbing;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.NodeContainer;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// Moves liquid from the network at the inlet of a <see cref="PlumbingPumpComponent"/> to the one at its outlet.
/// </summary>
public sealed class PlumbingPumpSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlumbingSystem _plumbing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingPumpComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PlumbingPumpComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PlumbingPumpComponent, ActivateInWorldEvent>(OnActivated);
    }

    private void OnMapInit(Entity<PlumbingPumpComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval * _random.NextFloat();
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<PlumbingPumpComponent, NodeContainerComponent>();
        while (query.MoveNext(out var uid, out var pump, out var container))
        {
            if (curTime < pump.NextUpdate)
                continue;

            pump.NextUpdate = curTime + pump.UpdateInterval;

            if (!pump.Enabled ||
                !_plumbing.TryGetNet(uid, pump.InletNodeName, out var from, container) ||
                !_plumbing.TryGetNet(uid, pump.OutletNodeName, out var to, container))
                continue;

            _plumbing.Transfer(from, to, pump.Rate * (float) pump.UpdateInterval.TotalSeconds);
        }
    }
}
