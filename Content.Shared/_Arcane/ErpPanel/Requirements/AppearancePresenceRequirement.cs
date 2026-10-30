using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Arcane.ErpPanel.Requirements;

/// <summary>
/// Проверяет присутствие «черты внешности»: marking'и указанных категорий/по id
/// и/или части тела (Shitmed). Если задан <see cref="Species"/> — проверка обязательна
/// только для перечисленных рас; у остальных рас требование считается выполненным,
/// чтобы не менять их поведение.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class AppearancePresenceRequirement : InvertableErpRequirement
{
    /// <summary>Категории marking'ов: любая непустая категория = присутствие.</summary>
    [DataField]
    public List<MarkingCategories> Categories = new();

    /// <summary>Конкретные id marking'ов: любой найденный = присутствие.</summary>
    [DataField]
    public List<ProtoId<MarkingPrototype>> Markings = new();

    /// <summary>Типы частей тела (Shitmed): любая найденная часть = присутствие.</summary>
    [DataField]
    public List<BodyPartType> BodyParts = new();

    /// <summary>Если список не пуст — проверка обязательна только для этих рас.</summary>
    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>> Species = new();

    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        if (entityManager.TryGetComponent<HumanoidAppearanceComponent>(uid, out var humanoid))
        {
            if (Species.Count > 0 && !Species.Contains(humanoid.Species))
                return true;

            var humanoidPresent = HasMarking(humanoid) || HasBodyPart(uid, entityManager);
            return Inverted ? !humanoidPresent : humanoidPresent;
        }

        // Не-гуманоид: marking'и проверить нельзя, но части тела (борги, животные) — можно.
        if (Species.Count > 0)
            return true;

        var bodyPresent = HasBodyPart(uid, entityManager);
        return Inverted ? !bodyPresent : bodyPresent;
    }

    private bool HasMarking(HumanoidAppearanceComponent humanoid)
    {
        if (Categories.Count == 0 && Markings.Count == 0)
            return false;

        foreach (var category in Categories)
        {
            if (humanoid.MarkingSet.Markings.TryGetValue(category, out var markings) && markings.Count > 0)
                return true;
        }

        if (Markings.Count == 0)
            return false;

        foreach (var marking in humanoid.MarkingSet.GetForwardEnumerator())
        {
            foreach (var configured in Markings)
            {
                if (configured.Id == marking.MarkingId)
                    return true;
            }
        }

        return false;
    }

    private bool HasBodyPart(EntityUid uid, IEntityManager entityManager)
    {
        if (BodyParts.Count == 0 || !entityManager.TryGetComponent<BodyComponent>(uid, out var body))
            return false;

        var bodySystem = entityManager.System<SharedBodySystem>();
        foreach (var (_, part) in bodySystem.GetBodyChildren(uid, body))
        {
            if (BodyParts.Contains(part.PartType))
                return true;
        }

        return false;
    }
}
