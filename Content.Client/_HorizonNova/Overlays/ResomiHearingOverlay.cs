/*
 * Horizon Nova — AGPLv3
 * Copyright (c) 2026 Horizon Nova Contributors
 *
 * Раса «Резоми». Оверлей «Четырёхканального слуха»: рисует цветные маркеры
 * источников звука вокруг игрока. Цвет зависит от канала звука.
 */

using System.Numerics;
using Content.Shared._HorizonNova.Resomi.Hearing;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;

namespace Content.Client._HorizonNova.Overlays;

public sealed partial class ResomiHearingOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly SharedTransformSystem _transform;

    /// <summary>
    /// Радиус слуха локального игрока (тайлы).
    /// </summary>
    public float Radius = 10f;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public ResomiHearingOverlay()
    {
        IoCManager.InjectDependencies(this);
        _transform = _entMan.System<SharedTransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity is not { } player
            || !_entMan.TryGetComponent<TransformComponent>(player, out var playerXform))
            return;

        var playerPos = _transform.GetWorldPosition(playerXform);
        var radiusSquared = Radius * Radius;
        var handle = args.WorldHandle;

        var query = _entMan.EntityQueryEnumerator<ResomiSoundMarkerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var marker, out var xform))
        {
            if (uid == player || xform.MapID != args.MapId)
                continue;

            var position = _transform.GetWorldPosition(xform);
            if ((position - playerPos).LengthSquared() > radiusSquared)
                continue;

            var color = GetChannelColor(marker.Channel);
            handle.DrawCircle(position, 0.32f, color.WithAlpha(0.35f));
            handle.DrawCircle(position, 0.16f, color.WithAlpha(0.9f));
        }
    }

    /// <summary>
    /// Цвет маркера по каналу звука.
    /// </summary>
    private static Color GetChannelColor(ResomiSoundChannel channel)
    {
        return channel switch
        {
            ResomiSoundChannel.Combat => Color.Red,
            ResomiSoundChannel.Fall => Color.Yellow,
            ResomiSoundChannel.Voice => Color.LimeGreen,
            ResomiSoundChannel.Footstep => Color.Gray,
            _ => Color.White,
        };
    }
}
