using Content.Server._Horizon.Plumbing.NodeGroups;
using Content.Shared._Horizon.Plumbing;
using Content.Shared.FixedPoint;
using Content.Shared.NodeContainer;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// Lets the <see cref="PlumbingOverflowComponent"/> devices of a network that was pushed into while full let its
/// liquid out onto the floor, so the pump has somewhere to put what it moves.
/// </summary>
public sealed class PlumbingOverflowSystem : EntitySystem
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PlumbingSystem _plumbing = default!;

    private TimeSpan _nextStep;
    private readonly Dictionary<PlumbingNet, List<Entity<PlumbingOverflowComponent>>> _devices = new();

    public override void Initialize()
    {
        base.Initialize();

        // The pumps note which networks were pushed into while full, and this looks at them afterwards.
        UpdatesAfter.Add(typeof(PlumbingPumpSystem));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        if (curTime < _nextStep)
            return;

        _nextStep = curTime + Interval;

        if (_plumbing.BlockedNets.Count == 0)
            return;

        // Sort the overflow devices by the blocked network they are on.
        _devices.Clear();
        var query = EntityQueryEnumerator<PlumbingOverflowComponent, NodeContainerComponent>();
        while (query.MoveNext(out var uid, out var overflow, out var container))
        {
            if (!_plumbing.TryGetNet(uid, overflow.NodeName, out var net, container) ||
                !_plumbing.BlockedNets.Contains(net))
                continue;

            if (!_devices.TryGetValue(net, out var list))
                _devices[net] = list = new();

            list.Add((uid, overflow));
        }

        foreach (var net in _plumbing.BlockedNets)
        {
            if (_devices.TryGetValue(net, out var list))
                Release(net, list);

            // Unhandled or handled, the note is only good for this step.
            net.Rejected = FixedPoint2.Zero;
        }

        _plumbing.BlockedNets.Clear();
        _devices.Clear();
    }

    private void Release(PlumbingNet net, List<Entity<PlumbingOverflowComponent>> devices)
    {
        var seconds = (float) Interval.TotalSeconds;
        var remaining = FixedPoint2.Min(net.Rejected, net.Fluid.Volume);

        foreach (var device in devices)
        {
            if (remaining <= 0)
                return;

            var amount = FixedPoint2.Min(device.Comp.Rate * seconds, remaining);
            _plumbing.Spill(device, net.Fluid.SplitSolution(amount));
            remaining -= amount;
        }
    }
}
