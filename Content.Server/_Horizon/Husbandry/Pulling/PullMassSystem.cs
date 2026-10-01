using Content.Shared._Horizon.Husbandry.Pulling;
using Content.Shared.Movement.Pulling.Events;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Server._Horizon.Husbandry.Pulling;

/// <summary>
/// Lightens an animal while somebody drags it, see <see cref="PullMassComponent"/>. Server only: the fixtures are
/// networked, so the client gets the lighter body with the next state and does not have to predict the change.
/// </summary>
public sealed class PullMassSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PullMassComponent, PullStartedMessage>(OnPullStarted);
        SubscribeLocalEvent<PullMassComponent, PullStoppedMessage>(OnPullStopped);
        SubscribeLocalEvent<PullMassComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnPullStarted(EntityUid uid, PullMassComponent comp, PullStartedMessage args)
    {
        // The message goes to the puller and the pulled one, only the pulled one is lightened.
        if (args.PulledUid != uid || comp.OriginalDensities.Count > 0)
            return;

        if (!TryComp<FixturesComponent>(uid, out var fixtures))
            return;

        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            if (fixture.Density <= comp.Density)
                continue;

            comp.OriginalDensities[id] = fixture.Density;
            _physics.SetDensity(uid, id, fixture, comp.Density, manager: fixtures);
        }
    }

    private void OnPullStopped(EntityUid uid, PullMassComponent comp, PullStoppedMessage args)
    {
        if (args.PulledUid == uid)
            Restore(uid, comp);
    }

    private void OnShutdown(EntityUid uid, PullMassComponent comp, ComponentShutdown args)
    {
        Restore(uid, comp);
    }

    private void Restore(EntityUid uid, PullMassComponent comp)
    {
        if (comp.OriginalDensities.Count == 0)
            return;

        if (TryComp<FixturesComponent>(uid, out var fixtures))
        {
            foreach (var (id, density) in comp.OriginalDensities)
            {
                if (fixtures.Fixtures.TryGetValue(id, out var fixture))
                    _physics.SetDensity(uid, id, fixture, density, manager: fixtures);
            }
        }

        comp.OriginalDensities.Clear();
    }
}
