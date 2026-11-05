using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._HorizonNova.Lactation;

/// <summary>
/// Общие правила лактации: список рас, размер груди по marking'ам и формулы
/// объёмов. Используется серверной системой.
/// </summary>
public static class LactationHelpers
{
    /// <summary>
    /// Расы, которым доступна лактация.
    /// </summary>
    public static readonly HashSet<ProtoId<SpeciesPrototype>> LactatingSpecies = new()
    {
        "Human",
        "Resomi",
        "Demon",
        "Dwarf",
    };

    /// <summary>
    /// Размер груди по id грудного marking'а.
    /// </summary>
    private static readonly Dictionary<string, int> BreastMarkingSizes = new()
    {
        ["HumanBreastSmall"] = 1,
        ["HumanBreastDefault"] = 2,
        ["HumanBreastMedium"] = 3,
        ["HumanBreastBig"] = 4,
        ["HumanBreastSuperBig"] = 5,
        ["DemonBreastSmall"] = 1,
        ["DemonBreastDefault"] = 2,
        ["DemonBreastMedium"] = 3,
        ["DemonBreastBig"] = 4,
        ["DemonBreastSuperBig"] = 5,
    };

    /// <summary>
    /// Размер груди персонажа: 0 — лактации нет, 1..5 — сила лактации.
    /// У Human/Demon определяется выбранным marking'ом груди; у Resomi/Dwarf
    /// своих грудных marking'ов нет, поэтому у женщин фиксированный минимальный размер.
    /// Лактация доступна только женщинам.
    /// <paramref name="overrideSize"/>: 0 — не задан, -1 — грудь убрана админом, 1..5 — принудительный размер.
    /// </summary>
    public static int GetBreastSize(HumanoidAppearanceComponent humanoid, int overrideSize = 0)
    {
        if (!LactatingSpecies.Contains(humanoid.Species))
            return 0;

        if (humanoid.Sex != Sex.Female)
            return 0;

        if (overrideSize < 0)
            return 0;

        if (overrideSize > 0)
            return Math.Clamp(overrideSize, 1, 5);

        var size = 0;
        foreach (var marking in humanoid.MarkingSet.GetForwardEnumerator())
        {
            if (BreastMarkingSizes.TryGetValue(marking.MarkingId, out var markingSize) && markingSize > size)
                size = markingSize;
        }

        if (size > 0)
            return size;

        // Resomi/Dwarf: без грудных marking'ов — минимальный размер.
        if (humanoid.Species == "Resomi" || humanoid.Species == "Dwarf")
            return 1;

        return 0;
    }

    /// <summary>
    /// Можно ли сцеживать молоко у этого персонажа: женщина, одна из четырёх рас
    /// и у неё есть грудь (размер больше нуля).
    /// </summary>
    public static bool CanMilk(HumanoidAppearanceComponent humanoid, int overrideSize = 0)
    {
        return humanoid.Sex == Sex.Female
               && LactatingSpecies.Contains(humanoid.Species)
               && GetBreastSize(humanoid, overrideSize) > 0;
    }

    /// <summary>
    /// Грудные marking'и расы (1..5) или null, если у расы их нет.
    /// </summary>
    public static readonly string[] HumanBreastMarkings =
    {
        "HumanBreastSmall", "HumanBreastDefault", "HumanBreastMedium", "HumanBreastBig", "HumanBreastSuperBig",
    };

    /// <inheritdoc cref="HumanBreastMarkings"/>
    public static readonly string[] DemonBreastMarkings =
    {
        "DemonBreastSmall", "DemonBreastDefault", "DemonBreastMedium", "DemonBreastBig", "DemonBreastSuperBig",
    };

    /// <summary>
    /// Возвращает список marking'ов груди для расы (1..5) или null, если их нет.
    /// </summary>
    public static string[]? GetBreastMarkings(ProtoId<SpeciesPrototype> species)
    {
        return species.Id switch
        {
            "Human" => HumanBreastMarkings,
            "Demon" => DemonBreastMarkings,
            _ => null,
        };
    }

    /// <summary>
    /// Максимальный запас молока при данном размере груди.
    /// </summary>
    public static FixedPoint2 GetMaxMilk(int size, FixedPoint2 baseMax, FixedPoint2 perSize)
    {
        return baseMax + perSize * Math.Max(size, 0);
    }

    /// <summary>
    /// Объём молока, вырабатываемый за один тик.
    /// </summary>
    public static FixedPoint2 GetProductionAmount(int size, float perSizePerSecond, TimeSpan interval)
    {
        if (size <= 0 || perSizePerSecond <= 0f)
            return FixedPoint2.Zero;

        return FixedPoint2.New(size * perSizePerSecond * (float) interval.TotalSeconds);
    }
}
