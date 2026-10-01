using Content.Shared.Access.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Doors.Systems;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Containers;

namespace Content.Shared._Horizon.Husbandry.Rideable;

/// <summary>
/// Lets somebody ride an animal with a saddle: holds the reins, sends the rider's movement to the animal and keeps
/// the rider in place.
/// </summary>
public sealed class RideableSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RideableComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RideableComponent, EntInsertedIntoContainerMessage>(OnSaddleInserted);
        SubscribeLocalEvent<RideableComponent, EntRemovedFromContainerMessage>(OnSaddleRemoved);
        SubscribeLocalEvent<RideableComponent, ItemSlotEjectAttemptEvent>(OnSaddleEjectAttempt);
        SubscribeLocalEvent<RideableComponent, StrapAttemptEvent>(OnStrapAttempt);
        SubscribeLocalEvent<RideableComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<RideableComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<RideableComponent, VirtualItemDeletedEvent>(OnReinsDropped);
        SubscribeLocalEvent<RideableComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RideableComponent, GetAdditionalAccessEvent>(OnGetAdditionalAccess);
        SubscribeLocalEvent<RiderComponent, PullAttemptEvent>(OnRiderPullAttempt);
        SubscribeLocalEvent<RiderComponent, AttemptMobTargetCollideEvent>(OnRiderTargetCollide);
    }

    /// <summary>
    /// Mobs push each other apart. The rider sits exactly where the animal is, so without this the animal would shove
    /// and slow itself down on the server, which the client that predicts the ride knows nothing about.
    /// </summary>
    private void OnRiderTargetCollide(Entity<RiderComponent> ent, ref AttemptMobTargetCollideEvent args)
    {
        args.Cancelled = true;
    }

    private void OnStartup(Entity<RideableComponent> ent, ref ComponentStartup args)
    {
        UpdateStrap(ent);
    }

    private void OnSaddleInserted(Entity<RideableComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.SaddleSlot)
            UpdateStrap(ent);
    }

    private void OnSaddleRemoved(Entity<RideableComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SaddleSlot)
            return;

        // Whoever sits on the animal gets off, however the saddle was taken away.
        if (ent.Comp.Rider is { } rider)
            _buckle.Unbuckle(rider, null);

        UpdateStrap(ent);
    }

    /// <summary>
    /// The saddle stays on while somebody rides. The slot is locked too, this covers every way around that.
    /// </summary>
    private void OnSaddleEjectAttempt(Entity<RideableComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (ent.Comp.Rider != null && args.Slot.ContainerSlot?.ID == ent.Comp.SaddleSlot)
            args.Cancelled = true;
    }

    /// <summary>
    /// The animal can only be climbed on while it has a saddle.
    /// </summary>
    private void UpdateStrap(Entity<RideableComponent> ent)
    {
        _buckle.StrapSetEnabled(ent, HasSaddle(ent));
    }

    private bool HasSaddle(Entity<RideableComponent> ent)
    {
        return _itemSlots.GetItemOrNull(ent, ent.Comp.SaddleSlot) != null;
    }

    private void OnStrapAttempt(Entity<RideableComponent> ent, ref StrapAttemptEvent args)
    {
        var rider = args.Buckle.Owner;

        string? reason = null;
        if (!HasSaddle(ent))
            reason = "rideable-no-saddle";
        else if (!_mobState.IsAlive(ent))
            reason = "rideable-not-alive";
        else if (ent.Comp.Rider != null)
            reason = "rideable-occupied";
        else if (CountFreeHands(rider) < ent.Comp.RequiredHands)
            reason = "rideable-no-free-hands";
        else if (TryComp<PullerComponent>(rider, out var puller) && puller.Pulling != null)
            reason = "rideable-cannot-pull";

        if (reason == null)
            return;

        args.Cancelled = true;
        if (args.Popup)
            _popup.PopupClient(Loc.GetString(reason, ("animal", ent.Owner)), ent, args.User ?? rider);
    }

    /// <summary>
    /// The reins go into a free hand that is not the active one, so the rider can still use the active hand
    /// for doors and the like. The active hand is taken only when there is nothing else.
    /// </summary>
    private string? FindReinsHand(EntityUid rider)
    {
        if (!TryComp<HandsComponent>(rider, out var hands))
            return null;

        string? fallback = null;
        foreach (var handId in hands.Hands.Keys)
        {
            if (!_hands.HandIsEmpty((rider, hands), handId))
                continue;

            if (handId != hands.ActiveHandId)
                return handId;

            fallback = handId;
        }

        return fallback;
    }

    private int CountFreeHands(EntityUid rider)
    {
        if (!TryComp<HandsComponent>(rider, out var hands))
            return 0;

        var free = 0;
        foreach (var handId in hands.Hands.Keys)
        {
            if (_hands.HandIsEmpty((rider, hands), handId))
                free++;
        }

        return free;
    }

    private void OnStrapped(Entity<RideableComponent> ent, ref StrappedEvent args)
    {
        var rider = args.Buckle.Owner;

        for (var i = 0; i < ent.Comp.RequiredHands; i++)
        {
            if (_virtualItem.TrySpawnVirtualItemInHand(ent, rider, out _, empty: FindReinsHand(rider)))
                continue;

            // The hands were checked before, but better safe than riding without holding the reins.
            _virtualItem.DeleteInHandsMatching(rider, ent);
            _buckle.Unbuckle(rider, null);
            return;
        }

        ent.Comp.Rider = rider;
        Dirty(ent);

        var riderComp = EnsureComp<RiderComponent>(rider);
        riderComp.Mount = ent;
        Dirty(rider, riderComp);

        _itemSlots.SetLock(ent, ent.Comp.SaddleSlot, true);
        _mover.SetRelay(rider, ent);

        // Doors open for whoever bumps into them with this tag. The animal alone does not have it.
        if (ent.Comp.RiderOpensDoors && _tag.AddTag(ent, SharedDoorSystem.DoorBumpTag))
            ent.Comp.AddedBumpTag = true;

        var ev = new RiderMountedEvent(rider);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnUnstrapped(Entity<RideableComponent> ent, ref UnstrappedEvent args)
    {
        var rider = args.Buckle.Owner;
        var isRider = ent.Comp.Rider == rider ||
                      TryComp<RiderComponent>(rider, out var riderComp) && riderComp.Mount == ent.Owner;
        if (!isRider)
            return;

        // Forget the rider first, deleting the reins tells us about it.
        ent.Comp.Rider = null;
        Dirty(ent);

        RemComp<RelayInputMoverComponent>(rider);
        _actionBlocker.UpdateCanMove(rider);
        _virtualItem.DeleteInHandsMatching(rider, ent);
        RemComp<RiderComponent>(rider);

        _itemSlots.SetLock(ent, ent.Comp.SaddleSlot, false);

        if (ent.Comp.AddedBumpTag)
        {
            _tag.RemoveTag(ent, SharedDoorSystem.DoorBumpTag);
            ent.Comp.AddedBumpTag = false;
        }

        var ev = new RiderDismountedEvent(rider);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnReinsDropped(EntityUid uid, RideableComponent comp, VirtualItemDeletedEvent args)
    {
        if (comp.Rider == args.User)
            _buckle.Unbuckle(args.User, null);
    }

    /// <summary>
    /// What the rider carries counts as the animal's own when it opens a door.
    /// </summary>
    private void OnGetAdditionalAccess(Entity<RideableComponent> ent, ref GetAdditionalAccessEvent args)
    {
        if (ent.Comp.RiderOpensDoors && ent.Comp.Rider is { } rider)
            args.Entities.Add(rider);
    }

    private void OnMobStateChanged(Entity<RideableComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive && ent.Comp.Rider is { } rider)
            _buckle.Unbuckle(rider, null);
    }

    private void OnRiderPullAttempt(Entity<RiderComponent> ent, ref PullAttemptEvent args)
    {
        if (args.PullerUid == ent.Owner)
            args.Cancelled = true;
    }
}
