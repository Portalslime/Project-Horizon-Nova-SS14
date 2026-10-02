using Content.Server.Fluids.EntitySystems;
using Content.Shared._Horizon.Defecation;
using Content.Shared._Horizon.Urination;
using Content.Shared.Buckle.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._Horizon.Urination;

/// <summary>
/// Малая нужда того, кто пристёгнут к сиденью (<see cref="DefecationSeatComponent"/>, туалет),
/// идёт в буфер сиденья, а не на пол. Жидкость там та же, что и от дефекации (единый сток).
/// </summary>
public sealed class UrinationSeatSystem : EntitySystem
{
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BuckleComponent, UrinateEvent>(OnUrinate);
    }

    private void OnUrinate(Entity<BuckleComponent> ent, ref UrinateEvent args)
    {
        if (args.Handled ||
            ent.Comp.BuckledTo is not { } seat ||
            !TryComp<DefecationSeatComponent>(seat, out var receiver))
            return;

        args.Handled = true;

        if (TryComp<UrinationComponent>(ent, out var urination))
            _audio.PlayPvs(urination.SeatSound, ent);

        var waste = new Solution(receiver.Reagent, receiver.Amount);
        if (_solutions.TryGetSolution(seat, receiver.Solution, out var soln, out var solution))
        {
            var fits = FixedPoint2.Min(waste.Volume, solution.AvailableVolume);
            if (fits > 0)
                _solutions.TryAddSolution(soln.Value, waste.SplitSolution(fits));
        }

        _popup.PopupEntity(Loc.GetString("urination-self"), ent, ent);
        _popup.PopupEntity(Loc.GetString("urination-others", ("entity", ent)), ent, Filter.PvsExcept(ent), true);

        if (waste.Volume > 0)
        {
            _puddle.TrySpillAt(seat, waste, out _);
            _popup.PopupEntity(Loc.GetString("urination-seat-overflow", ("seat", seat)), seat, ent,
                PopupType.MediumCaution);
        }
    }
}
