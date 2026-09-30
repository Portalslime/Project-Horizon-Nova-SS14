using System.Linq;
using Content.Server._Horizon.Plumbing.Nodes;
using Content.Server._Horizon.Plumbing.Systems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Server._Horizon.Plumbing.NodeGroups;

/// <summary>
/// A connected set of <see cref="PlumbingNode"/>s sharing one body of liquid. The liquid is spread evenly over the
/// capacity of the nodes, so every node effectively holds the same fill percentage.
/// </summary>
/// <remarks>
/// The liquid lives in the solution of an entity of its own (<see cref="FluidEntity"/>), so it reacts like the contents
/// of any container. Liquid is never created or destroyed by re-shaping the network: it is split between the new
/// networks by capacity, and whatever belongs to a pipe that is deleted or detached is spilled where the pipe was.
/// </remarks>
[NodeGroup(NodeGroupID.Plumbing)]
public sealed class PlumbingNet : BaseNodeGroup
{
    /// <summary>
    /// The entity holding the liquid of the network. Always add liquid through <see cref="PlumbingSystem.Deposit"/>,
    /// which makes it react.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent> FluidEntity { get; private set; }

    /// <summary>
    /// The liquid in the network. Its MaxVolume is the network capacity.
    /// </summary>
    public Solution Fluid => FluidEntity.Comp.Solution;

    [ViewVariables]
    public FixedPoint2 Capacity => Fluid.MaxVolume;

    [ViewVariables]
    public FixedPoint2 FreeSpace => FixedPoint2.Max(FixedPoint2.Zero, Fluid.MaxVolume - Fluid.Volume);

    /// <summary>
    /// How full the network is, from 0 to 1.
    /// </summary>
    [ViewVariables]
    public float FillRatio => Capacity > 0 ? Math.Clamp(Fluid.Volume.Float() / Capacity.Float(), 0f, 1f) : 0f;

    /// <summary>
    /// Liquid that pumps tried to push into this network during the current step but that did not fit.
    /// Overflow devices spill that much out of the network. Handled by PlumbingOverflowSystem.
    /// </summary>
    [ViewVariables]
    public FixedPoint2 Rejected;

    private PlumbingSystem _plumbing = default!;

    public override void Initialize(Node sourceNode, IEntityManager entMan)
    {
        base.Initialize(sourceNode, entMan);

        _plumbing = entMan.System<PlumbingSystem>();
        FluidEntity = _plumbing.SpawnNetSolution(sourceNode.Owner);
    }

    public override void LoadNodes(List<Node> groupNodes)
    {
        base.LoadNodes(groupNodes);

        foreach (var node in groupNodes)
        {
            if (node is PlumbingNode plumbing)
                Fluid.MaxVolume += plumbing.Volume;
        }
    }

    public override void RemoveNode(Node node)
    {
        base.RemoveNode(node);

        // Nodes that only get detached are handled in AfterRemake. A deleted node has to hand back its share here.
        if (!node.Deleting || node is not PlumbingNode plumbing)
            return;

        var share = ShareOf(Fluid.Volume, plumbing.Volume, Capacity);
        if (share > 0)
            _plumbing.Spill(node.Owner, Fluid.SplitSolution(share));

        Fluid.MaxVolume -= plumbing.Volume;
    }

    public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
    {
        HandOver(newGroups);

        // This network is gone, its liquid is now in the new ones.
        _plumbing.DeleteNetSolution(FluidEntity);
    }

    private void HandOver(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
    {
        var total = Fluid.Volume;
        var capacity = Capacity;
        if (total <= 0 || capacity <= 0)
            return;

        PlumbingNet? firstNet = null;
        Node? anyNode = null;

        foreach (var grouping in newGroups)
        {
            // Deleted nodes stay in the old group, they already gave back their share in RemoveNode.
            if (ReferenceEquals(grouping.Key, this))
                continue;

            if (grouping.Key is PlumbingNet net)
            {
                var volume = FixedPoint2.Zero;
                foreach (var node in grouping)
                {
                    if (node is PlumbingNode plumbing)
                        volume += plumbing.Volume;
                }

                var share = ShareOf(total, volume, capacity);
                if (share > 0)
                    _plumbing.Deposit(net, Fluid.SplitSolution(share));

                firstNet ??= net;
                continue;
            }

            // Nodes without a group (unanchored pipes and devices): their liquid ends up on the floor.
            foreach (var node in grouping)
            {
                anyNode ??= node;
                if (node is not PlumbingNode plumbing)
                    continue;

                var share = ShareOf(total, plumbing.Volume, capacity);
                if (share > 0)
                    _plumbing.Spill(node.Owner, Fluid.SplitSolution(share));
            }
        }

        // Rounding leftovers must not vanish either.
        if (Fluid.Volume <= 0)
            return;

        if (firstNet != null)
            _plumbing.Deposit(firstNet, Fluid.SplitSolution(Fluid.Volume));
        else if (anyNode != null)
            _plumbing.Spill(anyNode.Owner, Fluid.SplitSolution(Fluid.Volume));
    }

    private static FixedPoint2 ShareOf(FixedPoint2 total, FixedPoint2 part, FixedPoint2 whole)
    {
        if (whole <= 0 || total <= 0)
            return FixedPoint2.Zero;

        return FixedPoint2.Min(total, FixedPoint2.New(total.Float() * part.Float() / whole.Float()));
    }

    public override string GetDebugData()
    {
        return $"Fluid: {Fluid.Volume}/{Capacity} ({FillRatio:P0})";
    }
}
