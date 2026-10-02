/*
 * Horizon Nova — AGPLv3
 * Copyright (c) 2026 Horizon Nova Contributors
 *
 * Раса «Резоми». Общая часть системы спринта: модификатор скорости
 * считается и на сервере, и на клиенте, поэтому обработчик живёт в Shared.
 */

using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;

namespace Content.Shared._HorizonNova.Resomi.Sprint;

public abstract partial class SharedResomiSprintSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResomiSprintComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
    }

    private void OnRefreshMoveSpeed(Entity<ResomiSprintComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!ent.Comp.Active)
            return;

        args.ModifySpeed(ent.Comp.SpeedModifier, ent.Comp.SpeedModifier);
    }
}
