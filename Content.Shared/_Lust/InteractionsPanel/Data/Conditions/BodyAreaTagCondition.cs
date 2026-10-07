using Robust.Shared.Containers;
using Robust.Shared.Serialization;

// Порт с Lust Station (_Sunrise/InteractionsPanel).
//HN: оригинал определял покрытие тела по тегам на одежде (NudeBottom, FullCovered, ...).
// В этой сборке таких тегов нет, поэтому покрытие определяется по занятости слотов инвентаря,
// что соответствует существующей логике одежды/сокрытия слоёв.
namespace Content.Shared._Lust.InteractionsPanel.Data.Conditions;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class BodyAreaTagCondition : IAppearCondition
{
    [DataField]
    public bool CheckInitiator { get; private set; }

    [DataField]
    public bool CheckTarget { get; private set; } = true;

    [DataField]
    public bool RequireExposed { get; private set; } = true;

    [DataField(required: true)]
    public HashSet<string> Categories { get; private set; } = new();

    /// <summary>
    /// Какие слоты инвентаря закрывают область тела.
    /// Слоты этой сборки: jumpsuit, outerClothing, underwearb (низ), underweart (верх),
    /// head, mask, neck, gloves, socks, shoes.
    /// </summary>
    private static readonly Dictionary<string, string[]> AreaSlots = new()
    {
        ["пах"] = new[] { "jumpsuit", "outerClothing", "underwearb" },
        ["яйца"] = new[] { "jumpsuit", "outerClothing", "underwearb" },
        ["член"] = new[] { "jumpsuit", "outerClothing", "underwearb" },
        ["вагина"] = new[] { "jumpsuit", "outerClothing", "underwearb" },
        ["анал"] = new[] { "jumpsuit", "outerClothing", "underwearb" },
        ["грудь"] = new[] { "jumpsuit", "outerClothing", "underweart" },
        ["ляжки"] = new[] { "jumpsuit", "outerClothing" },
        ["попа"] = new[] { "jumpsuit", "outerClothing" },
        ["лицо"] = new[] { "head", "mask" },
        ["губы"] = new[] { "head", "mask" },
        ["щёки"] = new[] { "head", "mask" },
        ["рот"] = new[] { "mask" },
        ["волосы"] = new[] { "head" },
        ["уши"] = new[] { "head" },
        ["шея"] = new[] { "neck" },
        ["ладони"] = new[] { "gloves" },
        ["гладкие перчатки"] = new[] { "gloves" },
        ["ступни"] = new[] { "shoes", "socks" },
        ["носки"] = new[] { "socks" },
        // "хвост" и "клетка" одеждой в этой сборке не закрываются.
    };

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entityManager)
    {
        if (CheckInitiator && !CheckEntity(initiator, entityManager))
            return false;

        if (CheckTarget && !CheckEntity(target, entityManager))
            return false;

        return true;
    }

    private bool CheckEntity(EntityUid entity, EntityManager entMan)
    {
        if (!entMan.TryGetComponent<ContainerManagerComponent>(entity, out var inventory))
            return RequireExposed;

        foreach (var category in Categories)
        {
            var isCovered = IsAreaCovered(inventory, category);

            if (RequireExposed && isCovered)
                return false;

            if (!RequireExposed && !isCovered)
                return false;
        }

        return true;
    }

    private static bool IsAreaCovered(ContainerManagerComponent inventory, string category)
    {
        if (!AreaSlots.TryGetValue(category, out var slots))
            return false;

        foreach (var slot in slots)
        {
            if (inventory.Containers.TryGetValue(slot, out var container) && container.ContainedEntities.Count > 0)
                return true;
        }

        return false;
    }
}
