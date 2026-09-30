using Content.Server._Horizon.Atmos;
using Content.Shared._Horizon.Defecation;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server._Horizon.Defecation;

/// <summary>
/// The consequences of an accident on the floor: everyone notices, and the entity smells for a while.
/// </summary>
public sealed class DefecationAccidentSystem : EntitySystem
{
    [Dependency] private readonly GasEmitterSystem _gasEmitter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DefecationComponent, DefecationAccidentEvent>(OnAccident);
    }

    private void OnAccident(Entity<DefecationComponent> ent, ref DefecationAccidentEvent args)
    {
        _popup.PopupEntity(Loc.GetString("defecation-accident-self"), ent, ent, PopupType.MediumCaution);
        _popup.PopupEntity(Loc.GetString("defecation-accident-others", ("entity", ent)), ent,
            Filter.PvsExcept(ent), true, PopupType.Medium);

        _gasEmitter.Emit(ent, ent.Comp.AccidentGases, ent.Comp.AccidentDuration, ent.Comp.AccidentEmitInterval);
    }
}
