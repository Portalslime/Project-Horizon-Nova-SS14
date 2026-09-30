using Content.Server._Horizon.Plumbing.Nodes;
using Content.Server.NodeContainer.EntitySystems;
using Content.Shared._Horizon.Plumbing;
using Content.Shared.Construction.Components;
using Content.Shared.NodeContainer;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// Lets <see cref="PlumbingPortableComponent"/> devices be anchored only onto a port, and connects them to the
/// network only while they are anchored. Also keeps a <see cref="PlumbingPortComponent"/> from being unanchored
/// while something is connected through it.
/// </summary>
public sealed class PlumbingPortableSystem : EntitySystem
{
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingPortableComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PlumbingPortableComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<PlumbingPortableComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<PlumbingPortComponent, UnanchorAttemptEvent>(OnPortUnanchorAttempt);
    }

    private void OnStartup(Entity<PlumbingPortableComponent> ent, ref ComponentStartup args)
    {
        // A device that is already anchored when it appears (built into a map) gets no anchoring event.
        if (_nodeContainer.TryGetNode(ent.Owner, ent.Comp.NodeName, out PlumbingNode? node))
            node.ConnectionsEnabled = Transform(ent).Anchored;
    }

    private void OnAnchorAttempt(EntityUid uid, PlumbingPortableComponent portable, AnchorAttemptEvent args)
    {
        // Without a port to sit on it cannot be anchored.
        var xform = Transform(uid);
        if (!HasPortIn(xform.GridUid, xform.Coordinates))
            args.Cancel();
    }

    private void OnAnchorStateChanged(Entity<PlumbingPortableComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (_nodeContainer.TryGetNode(ent.Owner, ent.Comp.NodeName, out PlumbingNode? node))
            node.ConnectionsEnabled = args.Anchored;
    }

    private void OnPortUnanchorAttempt(EntityUid uid, PlumbingPortComponent port, UnanchorAttemptEvent args)
    {
        var xform = Transform(uid);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        // Anything anchored here that joins the network through the port (a barrel, a sink, a toilet...).
        foreach (var other in _map.GetAnchoredEntities((xform.GridUid.Value, grid), xform.Coordinates))
        {
            if (other == uid || !TryComp<NodeContainerComponent>(other, out var container))
                continue;

            foreach (var node in container.Nodes.Values)
            {
                if (node is not PlumbingNode { Portable: true })
                    continue;

                _popup.PopupEntity(Loc.GetString("plumbing-port-occupied", ("port", uid), ("other", other)), uid,
                    args.User, PopupType.MediumCaution);
                args.Cancel();
                return;
            }
        }
    }

    private bool HasPortIn(EntityUid? gridUid, EntityCoordinates coordinates)
    {
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        // Only a port that is itself anchored joins the network, so only that counts.
        foreach (var entity in _map.GetAnchoredEntities((gridUid.Value, grid), coordinates))
        {
            if (HasComp<PlumbingPortComponent>(entity))
                return true;
        }

        return false;
    }
}
