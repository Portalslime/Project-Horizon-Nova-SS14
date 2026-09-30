using Content.Shared._Horizon.Defecation;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._Horizon.Defecation;

/// <summary>
/// The default when nobody else took care of it: the entity leaves its product on the floor. An accident on the
/// floor is announced further, see <see cref="DefecationAccidentEvent"/>.
/// </summary>
public sealed class DefecationDropSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DefecationComponent, DefecateEvent>(OnDefecate, after: [typeof(DefecationSeatSystem)]);
    }

    private void OnDefecate(Entity<DefecationComponent> ent, ref DefecateEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        Spawn(ent.Comp.Product, Transform(ent).Coordinates);
        _audio.PlayPvs(ent.Comp.Sound, ent);

        if (args.Accident)
        {
            var accident = new DefecationAccidentEvent();
            RaiseLocalEvent(ent, ref accident);
            return;
        }

        _popup.PopupEntity(Loc.GetString("defecation-self"), ent, ent);
        _popup.PopupEntity(Loc.GetString("defecation-others", ("entity", ent)), ent, Filter.PvsExcept(ent), true);
    }
}
