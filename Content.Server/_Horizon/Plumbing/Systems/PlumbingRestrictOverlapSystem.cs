using Content.Server._Horizon.Plumbing.Nodes;
using Content.Server.Popups;
using Content.Shared._Horizon.Plumbing;
using Content.Shared.Atmos;
using Content.Shared.NodeContainer;
using Content.Shared.Construction.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// Keeps two plumbing pieces from being anchored on the same tile with an overlapping connection direction.
/// </summary>
public sealed class PlumbingRestrictOverlapSystem : EntitySystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    private readonly List<EntityUid> _anchoredEntities = new();
    private EntityQuery<NodeContainerComponent> _nodeContainerQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingRestrictOverlapComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<PlumbingRestrictOverlapComponent, AnchorAttemptEvent>(OnAnchorAttempt);

        _nodeContainerQuery = GetEntityQuery<NodeContainerComponent>();
    }

    private void OnAnchorStateChanged(Entity<PlumbingRestrictOverlapComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            return;

        if (HasComp<AnchorableComponent>(ent) && CheckOverlap(ent))
        {
            _popup.PopupEntity(Loc.GetString("pipe-restrict-overlap-popup-blocked", ("pipe", ent.Owner)), ent);
            _xform.Unanchor(ent, Transform(ent));
        }
    }

    private void OnAnchorAttempt(Entity<PlumbingRestrictOverlapComponent> ent, ref AnchorAttemptEvent args)
    {
        if (args.Cancelled || !CheckOverlap(ent))
            return;

        _popup.PopupEntity(Loc.GetString("pipe-restrict-overlap-popup-blocked", ("pipe", ent.Owner)), ent, args.User);
        args.Cancel();
    }

    public bool CheckOverlap(EntityUid uid)
    {
        if (!_nodeContainerQuery.TryComp(uid, out var container))
            return false;

        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        _anchoredEntities.Clear();
        _map.GetAnchoredEntities((gridUid, grid), tile, _anchoredEntities);

        var directions = GetDirections(container, xform);
        if (directions == PipeDirection.None)
            return false;

        foreach (var other in _anchoredEntities)
        {
            if (other == uid || !_nodeContainerQuery.TryComp(other, out var otherContainer))
                continue;

            if ((directions & GetDirections(otherContainer, Transform(other))) != PipeDirection.None)
                return true;
        }

        return false;
    }

    /// <summary>
    /// All directions the entity's plumbing nodes open to. The rotation is applied by hand because it is not kept
    /// up to date for unanchored pieces.
    /// </summary>
    private static PipeDirection GetDirections(NodeContainerComponent container, TransformComponent xform)
    {
        var result = PipeDirection.None;
        foreach (var node in container.Nodes.Values)
        {
            if (node is PlumbingNode plumbing)
                result |= plumbing.OriginalPipeDirection.RotatePipeDirection(xform.LocalRotation);
        }

        return result;
    }
}
