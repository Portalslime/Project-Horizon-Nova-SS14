using Content.Server.Actions;
using Content.Shared._Horizon.Urination;
using Content.Shared.Alert;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.Urination;

/// <summary>
/// Horizon: потребность «сходить по-маленькому» — аналог дефекации, но копится быстрее и терпится дольше.
/// </summary>
public sealed class UrinationSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UrinationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<UrinationComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<UrinationComponent, UrinateActionEvent>(OnUrinateAction);
        SubscribeLocalEvent<UrinationComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<UrinationComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void OnMapInit(Entity<UrinationComponent> ent, ref MapInitEvent args)
    {
        var comp = ent.Comp;
        if (comp.Value < 0)
            comp.Value = _random.NextFloat(comp.StartingRange.X, comp.StartingRange.Y);

        comp.NextUpdateTime = _timing.CurTime;
        comp.CurrentThreshold = GetThreshold(comp);
        _actions.AddAction(ent, ref comp.ActionEntity, comp.Action);
        UpdateAlert(ent);
    }

    private void OnShutdown(Entity<UrinationComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlertCategory(ent, ent.Comp.AlertCategory);
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnRejuvenate(Entity<UrinationComponent> ent, ref RejuvenateEvent args)
    {
        SetValue(ent, 0f);
    }

    private void OnUrinateAction(Entity<UrinationComponent> ent, ref UrinateActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Value < ent.Comp.Thresholds[UrinationThreshold.Urge])
        {
            _popup.PopupEntity(Loc.GetString("urination-no-need"), ent, ent);
            return;
        }

        args.Handled = true;
        Urinate(ent, accident: false);
    }

    /// <summary>
    /// Пока персонаж хочет писать — он немного замедляется.
    /// </summary>
    private void OnRefreshSpeed(Entity<UrinationComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.CurrentThreshold >= UrinationThreshold.Urge)
            args.ModifySpeed(ent.Comp.SlowdownModifier);
    }

    private UrinationThreshold GetThreshold(UrinationComponent comp)
    {
        var result = UrinationThreshold.Normal;
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

    private void SetValue(Entity<UrinationComponent> ent, float value)
    {
        ent.Comp.Value = Math.Clamp(value, 0f, ent.Comp.Thresholds[UrinationThreshold.Accident]);

        if (GetThreshold(ent.Comp) != ent.Comp.CurrentThreshold)
            UpdateAlert(ent);
    }

    private void UpdateAlert(Entity<UrinationComponent> ent)
    {
        var comp = ent.Comp;
        var previous = comp.CurrentThreshold;
        comp.CurrentThreshold = GetThreshold(comp);

        if (previous != comp.CurrentThreshold)
            _movement.RefreshMovementSpeedModifiers(ent.Owner);

        if (comp.Alerts.TryGetValue(comp.CurrentThreshold, out var alert))
            _alerts.ShowAlert(ent, alert);
        else
            _alerts.ClearAlertCategory(ent, comp.AlertCategory);
    }

    /// <summary>
    /// Сбрасывает потребность и передаёт результат дальше, см. <see cref="UrinateEvent"/>.
    /// </summary>
    public void Urinate(Entity<UrinationComponent> ent, bool accident)
    {
        SetValue(ent, 0f);

        var ev = new UrinateEvent(accident);
        RaiseLocalEvent(ent, ref ev);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<UrinationComponent>();
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

            if (comp.CurrentThreshold == UrinationThreshold.Accident)
            {
                Urinate(ent, accident: true);
                continue;
            }

            if (comp.CurrentThreshold != previous && comp.CurrentThreshold != UrinationThreshold.Normal)
            {
                _popup.PopupEntity(Loc.GetString($"urination-threshold-{comp.CurrentThreshold.ToString().ToLowerInvariant()}"),
                    uid, uid, PopupType.SmallCaution);
            }
        }
    }
}
