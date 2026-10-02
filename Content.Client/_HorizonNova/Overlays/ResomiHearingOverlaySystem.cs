/*
 * Horizon Nova — AGPLv3
 * Copyright (c) 2026 Horizon Nova Contributors
 *
 * Раса «Резоми». Включает оверлей «Четырёхканального слуха», пока он висит на
 * локальном игроке.
 */

using Content.Shared._HorizonNova.Resomi.Hearing;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Client._HorizonNova.Overlays;

public sealed partial class ResomiHearingOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private ResomiHearingOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new ResomiHearingOverlay();

        SubscribeLocalEvent<ResomiHearingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ResomiHearingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ResomiHearingComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<ResomiHearingComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ResomiHearingComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnStartup(Entity<ResomiHearingComponent> ent, ref ComponentStartup args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        _overlay.Radius = ent.Comp.Radius;
        AddOverlay();
    }

    private void OnShutdown(Entity<ResomiHearingComponent> ent, ref ComponentShutdown args)
    {
        RemoveOverlay();
    }

    private void OnHandleState(Entity<ResomiHearingComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        _overlay.Radius = ent.Comp.Radius;
    }

    private void OnPlayerAttached(Entity<ResomiHearingComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlay.Radius = ent.Comp.Radius;
        AddOverlay();
    }

    private void OnPlayerDetached(Entity<ResomiHearingComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveOverlay();
    }

    private void AddOverlay()
    {
        if (!_overlayMan.HasOverlay<ResomiHearingOverlay>())
            _overlayMan.AddOverlay(_overlay);
    }

    private void RemoveOverlay()
    {
        _overlayMan.RemoveOverlay(_overlay);
    }
}
