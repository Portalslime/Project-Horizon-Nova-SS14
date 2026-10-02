/*
 * Horizon Nova — AGPLv3
 * Copyright (c) 2026 Horizon Nova Contributors
 *
 * Раса «Резоми». Серверная часть спринта: переключение экшена, расход и
 * восстановление стамины (LuaSprint). В YAML резоми у LuaSprint обнулены
 * drainPerSecond/regenPerSecond — этим ресурсом владеет только эта система.
 */

using Content.Shared._HorizonNova.Resomi.Hearing;
using Content.Shared._HorizonNova.Resomi.Sprint;
using Content.Shared._Lua.Sprint;
using Content.Shared.Actions;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;

namespace Content.Server._HorizonNova.Resomi.Sprint;

public sealed partial class ResomiSprintSystem : SharedResomiSprintSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _moveSpeed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResomiSprintComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ResomiSprintComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ResomiSprintComponent, ResomiSprintToggleEvent>(OnToggle);
        SubscribeLocalEvent<ResomiSprintComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ResomiSprintComponent, KnockedDownEvent>(OnKnockedDown);
    }

    private void OnMapInit(Entity<ResomiSprintComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.SprintActionEntity, ent.Comp.SprintAction);
    }

    private void OnShutdown(Entity<ResomiSprintComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.SprintActionEntity);
    }

    private void OnMobStateChanged(Entity<ResomiSprintComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive && ent.Comp.Active)
            SetActive(ent, false, null, exhausted: false);
    }

    private void OnKnockedDown(Entity<ResomiSprintComponent> ent, ref KnockedDownEvent args)
    {
        if (ent.Comp.Active)
            SetActive(ent, false, null, exhausted: false);
    }

    private void OnToggle(Entity<ResomiSprintComponent> ent, ref ResomiSprintToggleEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var performer = args.Performer;

        if (ent.Comp.Active)
        {
            SetActive(ent, false, performer, exhausted: false);
            return;
        }

        if (TryComp<LuaSprintComponent>(ent.Owner, out var stamina)
            && stamina.CurrentSprint <= ent.Comp.AutoDeactivateThreshold)
        {
            _popup.PopupEntity(Loc.GetString("resomi-sprint-no-stamina"), performer, performer);
            return;
        }

        SetActive(ent, true, performer, exhausted: false);
    }

    private void SetActive(Entity<ResomiSprintComponent> ent, bool active, EntityUid? performer, bool exhausted)
    {
        ent.Comp.Active = active;

        if (TryComp<LuaSprintComponent>(ent.Owner, out var stamina))
        {
            // Отключение вручную, пока стамины больше порога — восстановление ускоряется.
            // Истощение до нуля — восстановление замедляется, пока стамина не вернётся к порогу.
            ent.Comp.FastRecovery = !active && !exhausted && stamina.CurrentSprint >= ent.Comp.FastRecoveryThreshold;
            ent.Comp.SlowRecovery = !active && exhausted;

            if (!active)
                stamina.LastSprintTime = _timing.CurTime;
        }

        _actions.SetToggled(ent.Comp.SprintActionEntity, active);
        _moveSpeed.RefreshMovementSpeedModifiers(ent);
        Dirty(ent);

        if (performer is { Valid: true } user)
        {
            var key = active ? "resomi-sprint-activated" : "resomi-sprint-deactivated";
            _popup.PopupEntity(Loc.GetString(key), user, user);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ResomiSprintComponent, LuaSprintComponent>();

        while (query.MoveNext(out var uid, out var sprint, out var stamina))
        {
            var changed = false;

            if (sprint.Active)
            {
                if (stamina.CurrentSprint <= sprint.AutoDeactivateThreshold)
                {
                    // Стамина опустошена — спринт гаснет сам.
                    SetActive((uid, sprint), false, uid, exhausted: true);
                    continue;
                }

                var next = MathF.Max(sprint.AutoDeactivateThreshold, stamina.CurrentSprint - sprint.StaminaDrainPerSecond * frameTime);
                if (!MathHelper.CloseTo(next, stamina.CurrentSprint))
                {
                    stamina.CurrentSprint = next;
                    changed = true;
                }

                stamina.LastSprintTime = curTime;

                if (stamina.CurrentSprint <= sprint.AutoDeactivateThreshold)
                {
                    SetActive((uid, sprint), false, uid, exhausted: true);
                    continue;
                }
            }
            else
            {
                // «Четырёхканальный слух» блокирует восстановление стамины.
                var regenBlocked = HasComp<ResomiHearingComponent>(uid);

                if (!regenBlocked
                    && curTime >= stamina.LastSprintTime + TimeSpan.FromSeconds(stamina.RegenDelay)
                    && stamina.CurrentSprint < stamina.MaxSprint)
                {
                    var rate = sprint.StaminaRegenPerSecond;
                    if (sprint.FastRecovery)
                        rate *= sprint.FastRecoveryMultiplier;
                    if (sprint.SlowRecovery)
                        rate *= sprint.SlowRecoveryMultiplier;

                    stamina.CurrentSprint = MathF.Min(stamina.MaxSprint, stamina.CurrentSprint + rate * frameTime);
                    changed = true;
                }

                if (sprint.FastRecovery && stamina.CurrentSprint >= stamina.MaxSprint)
                {
                    sprint.FastRecovery = false;
                    changed = true;
                }

                if (sprint.SlowRecovery && stamina.CurrentSprint >= sprint.FastRecoveryThreshold)
                {
                    sprint.SlowRecovery = false;
                    changed = true;
                }
            }

            if (changed)
                Dirty(uid, stamina);
        }
    }
}
