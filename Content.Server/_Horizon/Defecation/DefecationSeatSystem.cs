using Content.Server.Fluids.EntitySystems;
using Content.Shared._Horizon.Defecation;
using Content.Shared.Buckle.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._Horizon.Defecation;

/// <summary>
/// Waste of someone buckled to a <see cref="DefecationSeatComponent"/> (a toilet) goes into the seat's buffer instead
/// of onto the floor. What does not fit is spilled.
/// </summary>
public sealed class DefecationSeatSystem : EntitySystem
{
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BuckleComponent, DefecateEvent>(OnDefecate);
    }

    private void OnDefecate(Entity<BuckleComponent> ent, ref DefecateEvent args)
    {
        if (args.Handled ||
            ent.Comp.BuckledTo is not { } seat ||
            !TryComp<DefecationSeatComponent>(seat, out var receiver))
            return;

        args.Handled = true;

        if (TryComp<DefecationComponent>(ent, out var defecation))
            _audio.PlayPvs(defecation.SeatSound, ent);

        var waste = new Solution(receiver.Reagent, receiver.Amount);
        if (_solutions.TryGetSolution(seat, receiver.Solution, out var soln, out var solution))
        {
            var fits = FixedPoint2.Min(waste.Volume, solution.AvailableVolume);
            if (fits > 0)
                _solutions.TryAddSolution(soln.Value, waste.SplitSolution(fits));
        }

        _popup.PopupEntity(Loc.GetString("defecation-self"), ent, ent);
        _popup.PopupEntity(Loc.GetString("defecation-others", ("entity", ent)), ent, Filter.PvsExcept(ent), true);

        // The buffer was full: the rest goes on the floor.
        if (waste.Volume > 0)
        {
            _puddle.TrySpillAt(seat, waste, out _);
            _popup.PopupEntity(Loc.GetString("defecation-seat-overflow", ("seat", seat)), seat, ent,
                PopupType.MediumCaution);
        }
    }
}
