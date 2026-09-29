using Content.Server._Horizon.Atmos;
using Content.Server.Actions;
using Content.Shared._Horizon.Defecation;
using Content.Shared.Alert;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.Defecation;

public sealed class DefecationSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly GasEmitterSystem _gasEmitter = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DefecationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DefecationComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DefecationComponent, DefecateActionEvent>(OnDefecateAction);
        SubscribeLocalEvent<DefecationComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnMapInit(Entity<DefecationComponent> ent, ref MapInitEvent args)
    {
        var comp = ent.Comp;
        if (comp.Value < 0)
            comp.Value = _random.NextFloat(comp.StartingRange.X, comp.StartingRange.Y);

        comp.NextUpdateTime = _timing.CurTime;
        comp.CurrentThreshold = GetThreshold(comp);
        _actions.AddAction(ent, ref comp.ActionEntity, comp.Action);
        UpdateAlert(ent);
    }

    private void OnShutdown(Entity<DefecationComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlertCategory(ent, ent.Comp.AlertCategory);
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnRejuvenate(Entity<DefecationComponent> ent, ref RejuvenateEvent args)
    {
        SetValue(ent, 0f);
    }

    private void OnDefecateAction(Entity<DefecationComponent> ent, ref DefecateActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Value < ent.Comp.Thresholds[DefecationThreshold.Urge])
        {
            _popup.PopupEntity(Loc.GetString("defecation-no-need"), ent, ent);
            return;
        }

        args.Handled = true;
        Defecate(ent, accident: false);
    }

    private DefecationThreshold GetThreshold(DefecationComponent comp)
    {
        var result = DefecationThreshold.Normal;
        var best = float.MinValue;
        foreach (var (threshold, start) in comp.Thresholds)
        {
            if (comp.Value >= start && start > best)
            {
                result = threshold;
                best = start;
            }
        }

        return result;
    }

    private void SetValue(Entity<DefecationComponent> ent, float value)
    {
        ent.Comp.Value = Math.Clamp(value, 0f, ent.Comp.Thresholds[DefecationThreshold.Accident]);

        // Alerts only change when the stage changes, no need to touch them every tick.
        if (GetThreshold(ent.Comp) != ent.Comp.CurrentThreshold)
            UpdateAlert(ent);
    }

    private void UpdateAlert(Entity<DefecationComponent> ent)
    {
        var comp = ent.Comp;
        comp.CurrentThreshold = GetThreshold(comp);

        if (comp.Alerts.TryGetValue(comp.CurrentThreshold, out var alert))
            _alerts.ShowAlert(ent, alert);
        else
            _alerts.ClearAlertCategory(ent, comp.AlertCategory);
    }

    /// <summary>
    /// Spawns the product under the entity and resets the need.
    /// An accident also makes the entity emit gases for a while.
    /// </summary>
    public void Defecate(Entity<DefecationComponent> ent, bool accident)
    {
        var comp = ent.Comp;
        Spawn(comp.Product, Transform(ent).Coordinates);
        SetValue(ent, 0f);

        if (accident)
        {
            _popup.PopupEntity(Loc.GetString("defecation-accident-self"), ent, ent, PopupType.MediumCaution);
            _popup.PopupEntity(Loc.GetString("defecation-accident-others", ("entity", ent)), ent,
                Filter.PvsExcept(ent), true, PopupType.Medium);
            _gasEmitter.Emit(ent, comp.AccidentGases, comp.AccidentDuration, comp.AccidentEmitInterval);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("defecation-self"), ent, ent);
            _popup.PopupEntity(Loc.GetString("defecation-others", ("entity", ent)), ent,
                Filter.PvsExcept(ent), true);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DefecationComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextUpdateTime)
                continue;

            comp.NextUpdateTime += comp.UpdateRate;

            if (_mobState.IsDead(uid))
                continue;

            var ent = (uid, comp);
            var seconds = (float) comp.UpdateRate.TotalSeconds;
            var delta = comp.BaseFillRate * seconds;

            // Eating and drinking raise hunger/thirst above the last observed value, which speeds things up.
            if (TryComp<HungerComponent>(uid, out var hunger))
            {
                var current = _hunger.GetHunger(hunger);
                if (comp.LastHunger >= 0 && current > comp.LastHunger)
                    delta += (current - comp.LastHunger) * comp.HungerFactor;
                comp.LastHunger = current;
            }

            if (TryComp<ThirstComponent>(uid, out var thirst))
            {
                if (comp.LastThirst >= 0 && thirst.CurrentThirst > comp.LastThirst)
                    delta += (thirst.CurrentThirst - comp.LastThirst) * comp.ThirstFactor;
                comp.LastThirst = thirst.CurrentThirst;
            }

            var previous = comp.CurrentThreshold;
            SetValue(ent, comp.Value + delta);

            if (comp.CurrentThreshold == DefecationThreshold.Accident)
            {
                Defecate(ent, accident: true);
                continue;
            }

            if (comp.CurrentThreshold != previous && comp.CurrentThreshold != DefecationThreshold.Normal)
            {
                _popup.PopupEntity(Loc.GetString($"defecation-threshold-{comp.CurrentThreshold.ToString().ToLowerInvariant()}"),
                    uid, uid, PopupType.SmallCaution);
            }
        }
    }
}
