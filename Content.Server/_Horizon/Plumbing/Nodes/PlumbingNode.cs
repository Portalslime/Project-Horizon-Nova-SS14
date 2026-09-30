using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
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
    /// If false the direction never changes with the entity rotation, for sprites that do not rotate.
    /// </summary>
    [DataField("rotationsEnabled")]
    public bool RotationsEnabled = true;

    /// <summary>
    /// The fixture side of a port: connects to the portable node of a device anchored on the same tile, besides
    /// the pipes it opens to as usual.
    /// </summary>
    [DataField]
    public bool Port;

    /// <summary>
    /// The device side of a port (a barrel): connects to the port node on its own tile.
    /// </summary>
    [DataField]
    public bool Portable;

    /// <summary>
    /// Whether this node can connect to others at all. Portable devices switch it with their anchoring.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool ConnectionsEnabled
    {
        get => _connectionsEnabled;
        set
        {
            _connectionsEnabled = value;

            if (NodeGroup != null)
                IoCManager.Resolve<IEntityManager>().System<NodeGroupSystem>().QueueRemakeGroup((BaseNodeGroup) NodeGroup);
        }
    }

    [DataField("connectionsEnabled")]
    private bool _connectionsEnabled = true;

    public override bool Connectable(IEntityManager entMan, TransformComponent? xform = null)
    {
        return _connectionsEnabled && base.Connectable(entMan, xform);
    }

    public override void Initialize(EntityUid owner, IEntityManager entMan)
    {
        base.Initialize(owner, entMan);

        CurrentPipeDirection = RotatedDirection(entMan.GetComponent<TransformComponent>(owner).LocalRotation);
    }

    private PipeDirection RotatedDirection(Angle rotation)
    {
        return RotationsEnabled ? OriginalPipeDirection.RotatePipeDirection(rotation) : OriginalPipeDirection;
    }

    bool IRotatableNode.RotateNode(in MoveEvent ev)
    {
        if (OriginalPipeDirection == PipeDirection.Fourway)
            return false;

        var oldDirection = CurrentPipeDirection;
        CurrentPipeDirection = RotatedDirection(ev.NewRotation);
        return oldDirection != CurrentPipeDirection;
    }

    public override void OnAnchorStateChanged(IEntityManager entityManager, bool anchored)
    {
        if (!anchored)
            return;

        CurrentPipeDirection = RotatedDirection(entityManager.GetComponent<TransformComponent>(Owner).LocalRotation);
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

        // A port and a portable device meet on the same tile.
        if (Port || Portable)
        {
            foreach (var entity in grid.GetAnchoredEntities(pos))
            {
                if (entity == Owner || !nodeQuery.TryGetComponent(entity, out var container))
                    continue;

                foreach (var node in container.Nodes.Values)
                {
                    if (node is PlumbingNode other &&
                        other.NodeGroupID == NodeGroupID &&
                        (Port ? other.Portable : other.Port))
                    {
                        yield return other;
                    }
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
