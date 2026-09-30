using Content.Shared._Horizon.Plumbing;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// Lets a <see cref="FlushableComponent"/> owner (a toilet) be flushed into the plumbing network.
/// </summary>
public sealed class FlushableSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PlumbingSystem _plumbing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlushableComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<FlushableComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("flushable-verb-flush"),
            Priority = 2,
            Act = () => Flush(ent, user),
        });
    }

    /// <summary>
    /// Sends the buffer into the network. Returns false if it was on cooldown.
    /// </summary>
    public bool Flush(Entity<FlushableComponent> ent, EntityUid? user = null)
    {
        if (_timing.CurTime < ent.Comp.NextFlush)
            return false;

        ent.Comp.NextFlush = _timing.CurTime + ent.Comp.Cooldown;
        _audio.PlayPvs(ent.Comp.Sound, ent);

        if (!_solutions.TryGetSolution(ent.Owner, ent.Comp.Solution, out var soln, out var solution) ||
            solution.Volume <= 0)
            return true;

        var total = solution.Volume;
        var accepted = FixedPoint2.Zero;

        if (_plumbing.TryGetNet(ent, ent.Comp.NodeName, out var net))
        {
            accepted = FixedPoint2.Min(total, net.FreeSpace);
            if (accepted > 0)
                _plumbing.Deposit(net, _solutions.SplitSolution(soln.Value, accepted));
        }

        // What the pipes cannot take ends up on the floor.
        var overflow = total - accepted;
        if (overflow > 0)
        {
            _plumbing.Spill(ent, _solutions.SplitSolution(soln.Value, overflow));
            if (user != null)
                _popup.PopupEntity(Loc.GetString("flushable-overflow", ("entity", ent)), ent, user.Value, PopupType.MediumCaution);
        }

        return true;
    }
}
