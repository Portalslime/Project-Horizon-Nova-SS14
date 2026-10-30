using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._HorizonNova.Lactation;

/// <summary>
/// Лактация: накопление «материнского молока» в отдельном растворе на теле.
/// Сила зависит от размера груди (marking'и груди у Human/Demon, у Resomi/Dwarf — минимальный размер).
/// Сцеживание выполняется через ERP-панель (см. <c>LactationSystem</c>).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LactationComponent : Component
{
    /// <summary>
    /// Реагент, который вырабатывается в растворе.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> ReagentId = "LactationMilk";

    /// <summary>
    /// Имя раствора на теле.
    /// </summary>
    [DataField]
    public string SolutionName = "lactation";

    /// <summary>
    /// Ссылка на раствор (только на сервере, заполняется лениво).
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution;

    /// <summary>
    /// Размер груди 0..5. 0 — лактация недоступна.
    /// </summary>
    [AutoNetworkedField]
    public int BreastSize;

    /// <summary>
    /// Текущий объём молока (зеркало раствора для клиента и требований).
    /// </summary>
    [AutoNetworkedField]
    public FixedPoint2 MilkVolume;

    /// <summary>
    /// Максимальный объём молока при текущем размере груди.
    /// </summary>
    [AutoNetworkedField]
    public FixedPoint2 MaxMilk = 20;

    /// <summary>
    /// Максимум молока при минимальном размере груди.
    /// </summary>
    [DataField]
    public FixedPoint2 BaseMaxMilk = 10;

    /// <summary>
    /// Прибавка к максимуму за каждый размер груди.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxMilkPerSize = 10;

    /// <summary>
    /// Пассивная выработка молока в секунду за каждый размер груди.
    /// </summary>
    [DataField]
    public float MilkPerSizePerSecond = 0.1f;

    /// <summary>
    /// Интервал между тиками выработки.
    /// </summary>
    [DataField]
    public TimeSpan ProductionInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Расход сытости за единицу произведённого молока.
    /// </summary>
    [DataField]
    public float HungerUsagePerUnit = 0.4f;

    /// <summary>
    /// Расход жажды за единицу произведённого молока.
    /// </summary>
    [DataField]
    public float ThirstUsagePerUnit = 0.4f;

    /// <summary>
    /// Объём молока за одно сцеживание по размерам груди 1..5.
    /// </summary>
    [DataField]
    public List<FixedPoint2> MilkingAmounts = new() { 10, 14, 18, 22, 28 };

    /// <summary>
    /// Когда следующий тик выработки (только сервер).
    /// </summary>
    [ViewVariables]
    public TimeSpan NextProduction;
}
