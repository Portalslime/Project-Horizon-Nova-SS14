/*
 * Horizon Nova — AGPLv3
 * Copyright (c) 2026 Horizon Nova Contributors
 *
 * Раса «Резоми». Серверная часть «Четырёхканального слуха»: слушает события мира
 * (выстрелы, урон, взрывы, падения, речь, бег) и вешает на источник временный
 * маркер <see cref="ResomiSoundMarkerComponent"/>. Клиент рисует маркеры цветом канала.
 */

using Content.Server.Chat.Systems;
using Content.Shared._HorizonNova.Resomi.Hearing;
using Content.Shared._HorizonNova.Resomi.Sprint;
using Content.Shared.Actions;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Emoting;
using Content.Shared.Explosion;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Timing;

namespace Content.Server._HorizonNova.Resomi.Hearing;

public sealed partial class ResomiHearingSystem : EntitySystem
{
    /// <summary>Максимальный радиус слуха (для EntityLookup, у каждого слушателя свой).</summary>
    private const float MaxHearingRadius = 10f;

    /// <summary>Как часто сканируются бегущие рядом сущности.</summary>
    private static readonly TimeSpan FootstepScanInterval = TimeSpan.FromSeconds(0.5);

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private readonly HashSet<EntityUid> _found = new();
    private TimeSpan _nextFootstepScan;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResomiHearingSkillComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ResomiHearingSkillComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ResomiHearingSkillComponent, ResomiHearingToggleEvent>(OnToggle);
        SubscribeLocalEvent<ResomiHearingSkillComponent, MobStateChangedEvent>(OnMobStateChanged);

        // Каналы слуха.
        SubscribeLocalEvent<GunComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<DamageableComponent, BeforeExplodeEvent>(OnExplosion);
        SubscribeLocalEvent<ThrownItemComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<KnockedDownComponent, ComponentStartup>(OnKnockedDown);
        SubscribeLocalEvent<EntitySpokeEvent>(OnSpoke);
        SubscribeLocalEvent<EmotingComponent, EmoteEvent>(OnEmote);
    }

    private void OnMapInit(Entity<ResomiHearingSkillComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.HearingActionEntity, ent.Comp.HearingAction);
    }

    private void OnShutdown(Entity<ResomiHearingSkillComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.HearingActionEntity);
        RemComp<ResomiHearingComponent>(ent.Owner);
    }

    private void OnMobStateChanged(Entity<ResomiHearingSkillComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
            SetActive(ent.Owner, ent.Comp, false, popup: false);
    }

    private void OnToggle(Entity<ResomiHearingSkillComponent> ent, ref ResomiHearingToggleEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        SetActive(args.Performer, ent.Comp, !HasComp<ResomiHearingComponent>(args.Performer), popup: true);
    }

    private void SetActive(EntityUid uid, ResomiHearingSkillComponent skill, bool active, bool popup)
    {
        if (active)
        {
            EnsureComp<ResomiHearingComponent>(uid);
            if (popup)
                _popup.PopupEntity(Loc.GetString("resomi-hearing-activated"), uid, uid);
        }
        else
        {
            if (!HasComp<ResomiHearingComponent>(uid))
                return;

            RemComp<ResomiHearingComponent>(uid);
            if (popup)
                _popup.PopupEntity(Loc.GetString("resomi-hearing-deactivated"), uid, uid);
        }

        _actions.SetToggled(skill.HearingActionEntity, active);
    }

    #region Каналы слуха

    private void OnGunShot(Entity<GunComponent> ent, ref GunShotEvent args)
    {
        if (!args.User.IsValid())
            return;

        TryAddMarker(args.User, ResomiSoundChannel.Combat);
    }

    private void OnDamageChanged(Entity<DamageableComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        TryAddMarker(ent.Owner, ResomiSoundChannel.Combat);
    }

    private void OnExplosion(Entity<DamageableComponent> ent, ref BeforeExplodeEvent args)
    {
        TryAddMarker(ent.Owner, ResomiSoundChannel.Combat);
    }

    private void OnLand(Entity<ThrownItemComponent> ent, ref LandEvent args)
    {
        TryAddMarker(ent.Owner, ResomiSoundChannel.Fall);
    }

    private void OnKnockedDown(Entity<KnockedDownComponent> ent, ref ComponentStartup args)
    {
        TryAddMarker(ent.Owner, ResomiSoundChannel.Fall);
    }

    private void OnSpoke(EntitySpokeEvent args)
    {
        TryAddMarker(args.Source, ResomiSoundChannel.Voice);
    }

    private void OnEmote(Entity<EmotingComponent> ent, ref EmoteEvent args)
    {
        if (args.Emote.Category != EmoteCategory.Vocal)
            return;

        TryAddMarker(ent.Owner, ResomiSoundChannel.Voice);
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        // Маркеры живут недолго, чистятся пачкой.
        var markers = EntityQueryEnumerator<ResomiSoundMarkerComponent>();
        while (markers.MoveNext(out var markerUid, out var marker))
        {
            if (curTime >= marker.ExpireAt)
                RemCompDeferred(markerUid, marker);
        }

        // Шаги от бега: раз в полсекунды помечаем бегущих рядом.
        if (curTime < _nextFootstepScan || Count<ResomiHearingComponent>() == 0)
            return;

        _nextFootstepScan = curTime + FootstepScanInterval;

        var listeners = EntityQueryEnumerator<ResomiHearingComponent, TransformComponent>();
        while (listeners.MoveNext(out var listener, out var hearing, out var listenerXform))
        {
            if (!_mobState.IsAlive(listener))
                continue;

            _found.Clear();
            _lookup.GetEntitiesInRange(listenerXform.Coordinates, hearing.Radius, _found);

            foreach (var uid in _found)
            {
                if (uid == listener)
                    continue;

                if (HasComp<ResomiSoundMarkerComponent>(uid))
                    continue;

                if (!TryComp<InputMoverComponent>(uid, out var mover))
                    continue;

                var running = mover.IsSprinting && mover.HasDirectionalMovement
                              || TryComp<ResomiSprintComponent>(uid, out var sprint) && sprint.Active;

                if (!running)
                    continue;

                if (TryComp<MobStateComponent>(uid, out var mobState) && !_mobState.IsAlive(uid, mobState))
                    continue;

                AddMarker(uid, ResomiSoundChannel.Footstep);
            }
        }
    }

    #region Маркеры

    /// <summary>
    /// Пытается создать/обновить маркер, если рядом есть хотя бы один слушатель.
    /// </summary>
    private bool TryAddMarker(EntityUid source, ResomiSoundChannel channel)
    {
        if (Count<ResomiHearingComponent>() == 0)
            return false;

        var sourceXform = Transform(source);
        if (sourceXform.MapUid == null)
            return false;

        if (!HasListenerNear(sourceXform))
            return false;

        AddMarker(source, channel);
        return true;
    }

    private void AddMarker(EntityUid source, ResomiSoundChannel channel)
    {
        var marker = EnsureComp<ResomiSoundMarkerComponent>(source);
        marker.Channel = channel;
        marker.ExpireAt = _timing.CurTime + TimeSpan.FromSeconds(marker.Lifetime);
        Dirty(source, marker);
    }

    private bool HasListenerNear(TransformComponent sourceXform)
    {
        _found.Clear();
        _lookup.GetEntitiesInRange(sourceXform.Coordinates, MaxHearingRadius, _found);

        var sourcePos = _transform.GetWorldPosition(sourceXform);

        foreach (var uid in _found)
        {
            if (!TryComp<ResomiHearingComponent>(uid, out var hearing))
                continue;

            var listenerXform = Transform(uid);
            if ((_transform.GetWorldPosition(listenerXform) - sourcePos).LengthSquared() <= hearing.Radius * hearing.Radius)
                return true;
        }

        return false;
    }

    #endregion
}
