using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._HorizonNova.Lactation;

/// <summary>
/// Общие правила лактации: список рас, размер груди по marking'ам и формулы
/// объёмов. Используется и серверной системой, и ERP-требованием (на клиенте).
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
    /// </summary>
    public static int GetBreastSize(HumanoidAppearanceComponent humanoid)
    {
        if (!LactatingSpecies.Contains(humanoid.Species))
            return 0;

        var size = 0;
        foreach (var marking in humanoid.MarkingSet.GetForwardEnumerator())
        {
            if (BreastMarkingSizes.TryGetValue(marking.MarkingId, out var markingSize) && markingSize > size)
                size = markingSize;
        }

        if (size > 0)
            return size;

        // Resomi/Dwarf: без грудных marking'ов — минимальный размер, только женский пол.
        if (humanoid.Species == "Resomi" || humanoid.Species == "Dwarf")
            return humanoid.Sex == Sex.Female ? 1 : 0;

        return 0;
    }

    /// <summary>
    /// Максимальный запас молока при данном размере груди.
    /// </summary>
    public static FixedPoint2 GetMaxMilk(int size, FixedPoint2 baseMax, FixedPoint2 perSize)
    {
        return baseMax + perSize * Math.Max(size, 0);
    }

    /// <summary>
    /// Сколько молока сцеживается за одно действие (по размеру груди 1..5).
    /// </summary>
    public static FixedPoint2 GetMilkingAmount(int size, IReadOnlyList<FixedPoint2> amounts)
    {
        if (amounts.Count == 0)
            return FixedPoint2.Zero;

        var index = Math.Clamp(size - 1, 0, amounts.Count - 1);
        return amounts[index];
    }

    /// <summary>
    /// Порог возбуждения для сцеживания: чем больше грудь, тем ниже порог.
    /// </summary>
    public static float GetArousalThreshold(int size, IReadOnlyList<float> thresholds)
    {
        if (thresholds.Count == 0)
            return 0f;

        var index = Math.Clamp(size - 1, 0, thresholds.Count - 1);
        return thresholds[index];
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
