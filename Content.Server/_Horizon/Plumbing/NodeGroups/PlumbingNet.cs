using System.Linq;
using Content.Server._Horizon.Plumbing.Nodes;
using Content.Server._Horizon.Plumbing.Systems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Robust.Shared.Prototypes;

namespace Content.Server._Horizon.Plumbing.NodeGroups;

/// <summary>
/// A connected set of <see cref="PlumbingNode"/>s sharing one body of liquid. The liquid is spread evenly over the
/// capacity of the nodes, so every node effectively holds the same fill percentage.
/// </summary>
/// <remarks>
/// Liquid is never created or destroyed by re-shaping the network: it is split between the new networks by capacity,
/// and whatever belongs to a pipe that is deleted or detached is spilled where the pipe was.
/// </remarks>
[NodeGroup(NodeGroupID.Plumbing)]
public sealed class PlumbingNet : BaseNodeGroup
{
    /// <summary>
    /// The liquid in the network. Its MaxVolume is the network capacity.
    /// </summary>
    [ViewVariables]
    public Solution Fluid { get; } = new();

    [ViewVariables]
    public FixedPoint2 Capacity => Fluid.MaxVolume;

    [ViewVariables]
    public FixedPoint2 FreeSpace => FixedPoint2.Max(FixedPoint2.Zero, Fluid.MaxVolume - Fluid.Volume);

    /// <summary>
    /// How full the network is, from 0 to 1.
    /// </summary>
    [ViewVariables]
    public float FillRatio => Capacity > 0 ? Math.Clamp(Fluid.Volume.Float() / Capacity.Float(), 0f, 1f) : 0f;

    private PlumbingSystem _plumbing = default!;
    private IPrototypeManager _prototype = default!;

    public override void Initialize(Node sourceNode, IEntityManager entMan)
    {
        base.Initialize(sourceNode, entMan);

        _plumbing = entMan.System<PlumbingSystem>();
        _prototype = IoCManager.Resolve<IPrototypeManager>();
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
                    net.Fluid.AddSolution(Fluid.SplitSolution(share), _prototype);

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
            firstNet.Fluid.AddSolution(Fluid.SplitSolution(Fluid.Volume), _prototype);
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
