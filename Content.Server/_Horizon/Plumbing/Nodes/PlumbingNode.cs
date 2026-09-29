using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.FixedPoint;
using Content.Shared.NodeContainer;
using Robust.Shared.Map.Components;

namespace Content.Server._Horizon.Plumbing.Nodes;

/// <summary>
/// A node of the liquid plumbing network. Connects with other plumbing nodes whose direction points back at it.
/// Deliberately not a <see cref="PipeNode"/>: gas code assumes every PipeNode belongs to a gas pipe net.
/// </summary>
[DataDefinition]
public sealed partial class PlumbingNode : Node, IRotatableNode
{
    /// <summary>
    /// The directions this node connects in, for the south-facing (unrotated) entity.
    /// </summary>
    [DataField("pipeDirection")]
    public PipeDirection OriginalPipeDirection;

    /// <summary>
    /// <see cref="OriginalPipeDirection"/> with the entity rotation applied.
    /// </summary>
    public PipeDirection CurrentPipeDirection { get; private set; }

    /// <summary>
    /// How much liquid this node can hold. The network capacity is the sum over all of its nodes.
    /// </summary>
    [DataField("volume")]
    public FixedPoint2 Volume = 50;

    /// <summary>
    /// Connects to the pipes lying on the same tile instead of (or as well as) those in adjacent tiles.
    /// For furniture such as sinks and toilets, which sit on top of the pipe that feeds or drains them.
    /// Two nodes that both have this set never connect to each other.
    /// </summary>
    [DataField]
    public bool ConnectSameTile;

    public override void Initialize(EntityUid owner, IEntityManager entMan)
    {
        base.Initialize(owner, entMan);

        var xform = entMan.GetComponent<TransformComponent>(owner);
        CurrentPipeDirection = OriginalPipeDirection.RotatePipeDirection(xform.LocalRotation);
    }

    bool IRotatableNode.RotateNode(in MoveEvent ev)
    {
        if (OriginalPipeDirection == PipeDirection.Fourway)
            return false;

        var oldDirection = CurrentPipeDirection;
        CurrentPipeDirection = OriginalPipeDirection.RotatePipeDirection(ev.NewRotation);
        return oldDirection != CurrentPipeDirection;
    }

    public override void OnAnchorStateChanged(IEntityManager entityManager, bool anchored)
    {
        if (!anchored)
            return;

        var xform = entityManager.GetComponent<TransformComponent>(Owner);
        CurrentPipeDirection = OriginalPipeDirection.RotatePipeDirection(xform.LocalRotation);
    }

    public override IEnumerable<Node> GetReachableNodes(TransformComponent xform,
        EntityQuery<NodeContainerComponent> nodeQuery,
        EntityQuery<TransformComponent> xformQuery,
        MapGridComponent? grid,
        IEntityManager entMan)
    {
        if (!xform.Anchored || grid == null)
            yield break;

        var pos = grid.TileIndicesFor(xform.Coordinates);

        if (ConnectSameTile)
        {
            foreach (var entity in grid.GetAnchoredEntities(pos))
            {
                if (entity == Owner || !nodeQuery.TryGetComponent(entity, out var container))
                    continue;

                foreach (var node in container.Nodes.Values)
                {
                    if (node is PlumbingNode { ConnectSameTile: false } other && other.NodeGroupID == NodeGroupID)
                        yield return other;
                }
            }
        }

        for (var i = 0; i < PipeDirectionHelpers.PipeDirections; i++)
        {
            var direction = (PipeDirection) (1 << i);
            if (!CurrentPipeDirection.HasDirection(direction))
                continue;

            var neighbour = pos.Offset(direction.ToDirection());
            foreach (var entity in grid.GetAnchoredEntities(neighbour))
            {
                if (!nodeQuery.TryGetComponent(entity, out var container))
                    continue;

                foreach (var node in container.Nodes.Values)
                {
                    if (node is PlumbingNode other &&
                        other.NodeGroupID == NodeGroupID &&
                        other.CurrentPipeDirection.HasDirection(direction.GetOpposite()))
                    {
                        yield return other;
                    }
                }
            }
        }
    }
}
