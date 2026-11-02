using Content.Shared._Arcane.ErpPanel.Requirements;
using Content.Shared.Inventory;
using Robust.Shared.Serialization;

namespace Content.Shared._HorizonNova.Lactation;

/// <summary>
/// Для сцеживания молока грудь должна быть открыта: запрещены комбинезон,
/// ливчик/бюстгальтер и верхняя одежда. Зеркалит логику ExposedBodyPartRequirement
/// (Chest), но проверяет конкретные слоты и живёт в проекте Horizon Nova.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class LactationClothingRequirement : InvertableErpRequirement
{
    /// <summary>
    /// Слоты, которые должны быть пустыми для сцеживания.
    /// </summary>
    [DataField]
    public List<string> Slots = new()
    {
        "underweart",
        "jumpsuit",
        "outerClothing",
    };

    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        var inventory = entityManager.System<InventorySystem>();

        var hasClothes = false;
        foreach (var slot in Slots)
        {
            if (!inventory.TryGetSlotEntity(uid, slot, out _))
                continue;

            hasClothes = true;
            break;
        }

        // Обычно требование «одежда не должна быть надета»; inverted — наоборот.
        return Inverted ? hasClothes : !hasClothes;
    }
}
