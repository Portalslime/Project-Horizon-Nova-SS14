using System;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Humanoid;

/// <summary>
/// Horizon: применяет массу персонажа из множителей роста/ширины.
/// Визуальный масштаб делается отдельно через ScaleVisuals (SharedHumanoidAppearanceSystem),
/// здесь меняется только плотность фикстур (физическая масса). Хитбокс не масштабируется.
/// Порт механики из Lust/Sunrise.
/// </summary>
public sealed class HumanoidSizeSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public void ApplyPhysicalStats(EntityUid uid, ProtoId<SpeciesPrototype> speciesId, float width, float height)
    {
        if (!_proto.TryIndex(speciesId, out var species))
            return;

        if (width <= 0f)
            width = 1f;
        if (height <= 0f)
            height = 1f;

        if (!TryComp<FixturesComponent>(uid, out var fixtures))
            return;

        // Множитель массы относительно стандартных роста/ширины вида.
        var defaultWeight = MathF.Max(0.0001f, species.GetProfileWeight(species.DefaultWidth, species.DefaultHeight));
        var weight = species.GetProfileWeight(width, height);
        var multiplier = MathF.Max(0.01f, weight / defaultWeight);

        var stats = EnsureComp<HumanoidPhysicalStatsComponent>(uid);
        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            if (!stats.BaseDensities.TryGetValue(id, out var baseDensity))
            {
                baseDensity = fixture.Density;
                stats.BaseDensities[id] = baseDensity;
            }

            _physics.SetDensity(uid, id, fixture, baseDensity * multiplier);
        }
    }
}
