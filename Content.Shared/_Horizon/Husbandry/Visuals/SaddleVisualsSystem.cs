using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;

namespace Content.Shared._Horizon.Husbandry.Visuals;

/// <summary>
/// Keeps <see cref="AnimalVisuals.Saddled"/> in step with the saddle slot.
/// </summary>
public sealed class SaddleVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SaddleVisualsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SaddleVisualsComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<SaddleVisualsComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnStartup(Entity<SaddleVisualsComponent> ent, ref ComponentStartup args)
    {
        UpdateVisuals(ent);
    }

    private void OnInserted(Entity<SaddleVisualsComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.Slot)
            UpdateVisuals(ent);
    }

    private void OnRemoved(Entity<SaddleVisualsComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.Slot)
            UpdateVisuals(ent);
    }

    private void UpdateVisuals(Entity<SaddleVisualsComponent> ent)
    {
        _appearance.SetData(ent, AnimalVisuals.Saddled, _itemSlots.GetItemOrNull(ent, ent.Comp.Slot) != null);
    }
}
