using System.Diagnostics.CodeAnalysis;
using Content.Server._Horizon.Plumbing.NodeGroups;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.NodeContainer;
using Robust.Shared.Prototypes;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// The shared toolbox of the plumbing systems: finding the network of a node, putting liquid into it and moving
/// liquid between networks. The behaviours themselves (inlets, pumps, filters...) live in their own systems.
/// </summary>
public sealed class PlumbingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    /// <summary>
    /// Networks that were pushed into while full since the overflow devices last looked, see
    /// <see cref="PlumbingNet.Rejected"/>.
    /// </summary>
    public readonly HashSet<PlumbingNet> BlockedNets = new();

    /// <summary>
    /// Finds the plumbing network connected to the named node of an entity.
    /// </summary>
    public bool TryGetNet(EntityUid uid, string nodeName, [NotNullWhen(true)] out PlumbingNet? net,
        NodeContainerComponent? container = null)
    {
        net = null;
        if (!Resolve(uid, ref container, false))
            return false;

        if (!container.Nodes.TryGetValue(nodeName, out var node))
            return false;

        net = node.NodeGroup as PlumbingNet;
        return net != null;
    }

    /// <summary>
    /// Puts liquid on the floor where the entity is.
    /// </summary>
    public void Spill(EntityUid at, Solution solution)
    {
        if (solution.Volume <= 0)
            return;

        _puddle.TrySpillAt(at, solution, out _);
    }

    /// <summary>
    /// Adds liquid to a network. The caller is responsible for checking the free space first.
    /// </summary>
    public void Deposit(PlumbingNet net, Solution solution)
    {
        net.Fluid.AddSolution(solution, _prototype);
    }

    /// <summary>
    /// Moves up to <paramref name="amount"/> from one network to another, limited by what the source holds and
    /// the free space at the destination. What did not fit is noted on the destination, so that its overflow devices
    /// can make room. Returns how much was actually moved.
    /// </summary>
    public FixedPoint2 Transfer(PlumbingNet from, PlumbingNet to, FixedPoint2 amount)
    {
        if (ReferenceEquals(from, to))
            return FixedPoint2.Zero;

        var wanted = FixedPoint2.Min(amount, from.Fluid.Volume);
        var moved = FixedPoint2.Min(wanted, to.FreeSpace);

        if (wanted > moved)
        {
            to.Rejected += wanted - moved;
            BlockedNets.Add(to);
        }

        if (moved <= 0)
            return FixedPoint2.Zero;

        Deposit(to, from.Fluid.SplitSolution(moved));
        return moved;
    }
}
