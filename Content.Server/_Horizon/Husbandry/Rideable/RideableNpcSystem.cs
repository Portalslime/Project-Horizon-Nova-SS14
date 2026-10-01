using Content.Server.NPC.HTN;
using Content.Shared._Horizon.Husbandry.Rideable;

namespace Content.Server._Horizon.Husbandry.Rideable;

/// <summary>
/// An animal that is being ridden does what its rider says, not what its own brain wants.
/// </summary>
public sealed class RideableNpcSystem : EntitySystem
{
    [Dependency] private readonly HTNSystem _htn = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RideableComponent, RiderMountedEvent>(OnMounted);
        SubscribeLocalEvent<RideableComponent, RiderDismountedEvent>(OnDismounted);
    }

    private void OnMounted(Entity<RideableComponent> ent, ref RiderMountedEvent args)
    {
        SetBrain(ent, false);
    }

    private void OnDismounted(Entity<RideableComponent> ent, ref RiderDismountedEvent args)
    {
        SetBrain(ent, true);
    }

    private void SetBrain(EntityUid uid, bool enabled)
    {
        if (TryComp<HTNComponent>(uid, out var htn))
            _htn.SetHTNEnabled((uid, htn), enabled);
    }
}
