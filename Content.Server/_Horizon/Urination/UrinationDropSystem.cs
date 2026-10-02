using Content.Server.Fluids.EntitySystems;
using Content.Shared._Horizon.Urination;
using Content.Shared.Chemistry.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._Horizon.Urination;

/// <summary>
/// По умолчанию, если никто не обработал: под персонажем образуется лужа мочи.
/// </summary>
public sealed class UrinationDropSystem : EntitySystem
{
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UrinationComponent, UrinateEvent>(OnUrinate, after: [typeof(UrinationSeatSystem)]);
    }

    private void OnUrinate(Entity<UrinationComponent> ent, ref UrinateEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var waste = new Solution(ent.Comp.AccidentReagent, ent.Comp.AccidentAmount);
        _puddle.TrySpillAt(ent, waste, out _);
        _audio.PlayPvs(ent.Comp.Sound, ent);

        if (args.Accident)
        {
            var accident = new UrinationAccidentEvent();
            RaiseLocalEvent(ent, ref accident);
            return;
        }

        _popup.PopupEntity(Loc.GetString("urination-self"), ent, ent);
        _popup.PopupEntity(Loc.GetString("urination-others", ("entity", ent)), ent, Filter.PvsExcept(ent), true);
    }
}
