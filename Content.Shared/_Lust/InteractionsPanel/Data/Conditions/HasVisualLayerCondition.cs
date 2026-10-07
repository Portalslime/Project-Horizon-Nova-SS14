using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;

// Порт с Lust Station (_Sunrise/InteractionsPanel), адаптировано под Lua-API: MarkingSet вместо SharedVisualBodySystem.
namespace Content.Shared._Lust.InteractionsPanel.Data.Conditions;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class HasVisualLayerCondition : IAppearCondition
{
    [DataField]
    public bool CheckInitiator { get; private set; }

    [DataField]
    public bool CheckTarget { get; private set; } = true;

    [DataField(required: true)]
    public HumanoidVisualLayers Layer { get; private set; }

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entMan)
    {
        if (CheckInitiator && !CheckLayer(initiator, entMan))
            return false;

        if (CheckTarget && !CheckLayer(target, entMan))
            return false;

        return true;
    }

    //HN: Lua-совместимая проверка — наличие маркировки в категории, соответствующей слою.
    private bool CheckLayer(EntityUid uid, EntityManager entMan)
    {
        if (!entMan.TryGetComponent<HumanoidAppearanceComponent>(uid, out var appearance))
            return false;

        var category = MarkingCategoriesConversion.FromHumanoidVisualLayers(Layer);
        return appearance.MarkingSet.TryGetCategory(category, out var markings) && markings.Count > 0;
    }
}
