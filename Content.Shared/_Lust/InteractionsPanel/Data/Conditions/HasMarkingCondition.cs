using Content.Shared.Humanoid;
using Robust.Shared.Serialization;

// Порт с Lust Station (_Sunrise/InteractionsPanel), адаптировано под Lua-API: MarkingSet вместо SharedVisualBodySystem.
namespace Content.Shared._Lust.InteractionsPanel.Data.Conditions;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class HasMarkingCondition : IAppearCondition
{
    [DataField]
    public bool CheckInitiator { get; private set; }

    [DataField]
    public bool CheckTarget { get; private set; } = true;

    [DataField(required: true)]
    public List<string> MarkingWhitelist { get; private set; } = new();

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entMan)
    {
        if (CheckInitiator && !HasAnyMarking(initiator, entMan))
            return false;

        if (CheckTarget && !HasAnyMarking(target, entMan))
            return false;

        return true;
    }

    //HN: Lua-совместимая проверка — поиск маркировки по всем категориям.
    private bool HasAnyMarking(EntityUid uid, EntityManager entMan)
    {
        if (!entMan.TryGetComponent<HumanoidAppearanceComponent>(uid, out var appearance))
            return false;

        foreach (var marking in appearance.MarkingSet.GetForwardEnumerator())
        {
            if (MarkingWhitelist.Contains(marking.MarkingId))
                return true;
        }

        return false;
    }
}
