using Content.Server.Atmos.EntitySystems;
using Content.Shared._Horizon.Atmos;
using Content.Shared.Atmos;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.Atmos;

public sealed class GasEmitterSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasEmitterComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<GasEmitterComponent> ent, ref MapInitEvent args)
    {
        // Jitter the first emission so emitters spawned together don't all wake the atmos on the same tick.
        ent.Comp.NextUpdateTime = _timing.CurTime + ent.Comp.UpdateRate * _random.NextFloat(0.5f, 1.5f);

        if (ent.Comp.Lifetime is { } lifetime)
            ent.Comp.ExpireTime = _timing.CurTime + lifetime;
    }

    /// <summary>
    /// Starts or refreshes a temporary emitter on the entity.
    /// </summary>
    public void Emit(EntityUid uid, Dictionary<Gas, float> gases, TimeSpan duration, TimeSpan interval)
    {
        var emitter = EnsureComp<GasEmitterComponent>(uid);
        emitter.Gases = new Dictionary<Gas, float>(gases);
        emitter.UpdateRate = interval;
        emitter.Lifetime = duration;
        emitter.ExpireTime = _timing.CurTime + duration;
        emitter.NextUpdateTime = _timing.CurTime + emitter.UpdateRate;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<GasEmitterComponent>();
        while (query.MoveNext(out var uid, out var emitter))
        {
            // Only a timestamp comparison per emitter on most ticks.
            if (curTime < emitter.NextUpdateTime)
                continue;

            // Don't catch up on missed updates (e.g. after a lag spike), that would burst gas and atmos work.
            emitter.NextUpdateTime = curTime + emitter.UpdateRate;

            if (emitter.ExpireTime is { } expire && curTime >= expire)
            {
                RemCompDeferred<GasEmitterComponent>(uid);
                continue;
            }

            var mix = _atmosphere.GetTileMixture(uid, excite: true);
            if (mix == null)
                continue;

            var seconds = (float) emitter.UpdateRate.TotalSeconds;
            foreach (var (gas, moles) in emitter.Gases)
            {
                mix.AdjustMoles(gas, moles * seconds);
            }
        }
    }
}
