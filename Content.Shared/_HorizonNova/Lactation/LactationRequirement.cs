using Content.Shared._Arcane.ERP;
using Content.Shared._Arcane.ErpPanel.Requirements;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._HorizonNova.Lactation;

/// <summary>
/// Требование лактации: раса из списка, ненулевой размер груди, в груди есть молоко,
/// а возбуждение не ниже порога. Порог зависит от размера груди: чем больше грудь,
/// тем меньше возбуждения нужно для сцеживания.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class LactationRequirement : InvertableErpRequirement
{
    /// <summary>
    /// Расы, для которых действует требование. Пусто — для всех.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>> Species = new();

    /// <summary>
    /// Пороги возбуждения по размерам груди 1..5.
    /// </summary>
    [DataField]
    public List<float> ArousalThresholds = new() { 90f, 75f, 60f, 45f, 30f };

    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        var hasMilk = false;

        if (entityManager.TryGetComponent<HumanoidAppearanceComponent>(uid, out var humanoid)
            && (Species.Count == 0 || Species.Contains(humanoid.Species)))
        {
            var size = LactationHelpers.GetBreastSize(humanoid);
            if (size > 0
                && entityManager.TryGetComponent<LactationComponent>(uid, out var lactation)
                && lactation.MilkVolume > FixedPoint2.Zero
                && entityManager.TryGetComponent<ArousalComponent>(uid, out var arousal))
            {
                var threshold = LactationHelpers.GetArousalThreshold(size, ArousalThresholds);
                var arousalSystem = entityManager.System<ArousalSystem>();
                hasMilk = arousalSystem.GetArousal(arousal) >= threshold;
            }
        }

        return Inverted ? !hasMilk : hasMilk;
    }
}
