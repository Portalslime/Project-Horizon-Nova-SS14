using Content.Shared.Inventory;
using Robust.Shared.Serialization;

namespace Content.Shared._Arcane.ErpPanel.Requirements;

/// <summary>
/// HN: в нашей сборке нет BodyPartType.Chest/Groin, поэтому часть тела задаётся
/// собственным перечислением и проверяется по слотам одежды.
/// </summary>
[Serializable, NetSerializable]
public enum ErpExposedPart : byte
{
    Chest,
    Groin,
}

[Serializable, NetSerializable]
public sealed partial class ExposedBodyPartRequirement : InvertableErpRequirement
{
    [DataField(required: true)]
    public ErpExposedPart Part;

    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        var coveringSlots = Part switch
        {
            ErpExposedPart.Chest => SlotFlags.INNERCLOTHING | SlotFlags.OUTERCLOTHING | SlotFlags.UNDERWEART,
            ErpExposedPart.Groin => SlotFlags.INNERCLOTHING | SlotFlags.OUTERCLOTHING | SlotFlags.UNDERWEARB,
            _ => SlotFlags.NONE,
        };

        if (coveringSlots == SlotFlags.NONE)
            return false;

        var inventory = entityManager.System<InventorySystem>();
        if (!inventory.TryGetContainerSlotEnumerator(uid, out var slots, coveringSlots))
            return false;

        var isExposed = true;

        while (slots.MoveNext(out var containerSlot))
        {
            if (containerSlot?.ContainedEntity is null)
                continue;

            isExposed = false;
            break;
        }

        return Inverted ? !isExposed : isExposed;
    }
}
