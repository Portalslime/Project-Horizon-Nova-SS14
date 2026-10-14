using Content.Shared.DirtVisuals;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.DirtVisuals;

/// <summary>
/// HN: периодически пачкает бельё/комбинезон кровью у женских персонажей.
/// </summary>
public sealed partial class MenstruationSystem : EntitySystem
{
    [Dependency] private SharedDirtSystem _dirt = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MenstruationComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<MenstruationComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid) || humanoid.Sex != Sex.Female)
        {
            ent.Comp.Enabled = false;
            return;
        }

        ent.Comp.NextEvent = _timing.CurTime +
            TimeSpan.FromSeconds(_random.NextFloat(ent.Comp.StartingRange.X, ent.Comp.StartingRange.Y));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MenstruationComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Enabled)
                continue;

            if (_timing.CurTime < comp.NextEvent)
                continue;

            comp.NextEvent = _timing.CurTime + comp.Interval;

            _dirt.AddDirtToSlotsFromReagent(uid, comp.Amount, comp.Reagent.Id, comp.Slots);
            _popup.PopupEntity(Loc.GetString("menstruation-event-self"), uid, uid, PopupType.SmallCaution);
        }
    }
}
