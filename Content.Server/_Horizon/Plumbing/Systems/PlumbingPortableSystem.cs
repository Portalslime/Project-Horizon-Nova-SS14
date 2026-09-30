using Content.Server._Horizon.Plumbing.Nodes;
using Content.Server.NodeContainer.EntitySystems;
using Content.Shared._Horizon.Plumbing;
using Content.Shared.Construction.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// Lets <see cref="PlumbingPortableComponent"/> devices be anchored only onto a port, and connects them to the
/// network only while they are anchored.
/// </summary>
public sealed class PlumbingPortableSystem : EntitySystem
{
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingPortableComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<PlumbingPortableComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
    }

    private void OnAnchorAttempt(Entity<PlumbingPortableComponent> ent, ref AnchorAttemptEvent args)
    {
        // Without a port to sit on it cannot be anchored.
        var xform = Transform(ent);
        if (!HasPortIn(xform.GridUid, xform.Coordinates))
            args.Cancel();
    }

    private void OnAnchorStateChanged(Entity<PlumbingPortableComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (_nodeContainer.TryGetNode(ent.Owner, ent.Comp.NodeName, out PlumbingNode? node))
            node.ConnectionsEnabled = args.Anchored;
    }

    private bool HasPortIn(EntityUid? gridUid, EntityCoordinates coordinates)
    {
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        foreach (var entity in _map.GetLocal(gridUid.Value, grid, coordinates))
        {
            if (HasComp<PlumbingPortComponent>(entity))
                return true;
        }

        return false;
    }
}
