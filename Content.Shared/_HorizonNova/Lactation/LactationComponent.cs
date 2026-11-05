using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._HorizonNova.Lactation;

/// <summary>
/// Лактация: накопление «материнского молока» в отдельном растворе на теле.
/// Сила зависит от размера груди (marking'и груди у Human/Demon, у Resomi/Dwarf — минимальный размер).
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
    /// Текущий объём молока (зеркало раствора для клиента).
    /// Размер груди намеренно не кэшируется: он проверяется по marking'ам
    /// в момент взаимодействия/выработки.
    /// </summary>
    [AutoNetworkedField]
    public FixedPoint2 MilkVolume;

    /// <summary>
    /// Максимум молока при минимальном размере груди.
    /// </summary>
    [DataField]
    public FixedPoint2 BaseMaxMilk = 10;

    /// <summary>
    /// Админский размер груди для рас без грудных marking'ов (0 — не задан).
    /// Для Human/Demon размер берётся из marking'ов, тут 0.
    /// </summary>
    [DataField]
    public int BreastSizeOverride;

    /// <summary>
    /// Прибавка к максимуму за каждый размер груди.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxMilkPerSize = 10;

    /// <summary>
    /// Пассивная выработка молока в секунду за каждый размер груди.
    /// </summary>
    [DataField]
    public float MilkPerSizePerSecond = 0.05f;

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
    /// Начинать раунд с полной грудью (заполнение один раз при первом определении размера).
    /// </summary>
    [DataField]
    public bool FillAtStart = true;

    /// <summary>
    /// Заполнено ли молоко в начале раунда (только сервер).
    /// </summary>
    [ViewVariables]
    public bool FilledAtStart;

    /// <summary>
    /// Доля молока, которая сцеживается за одно действие (0..1).
    /// </summary>
    [DataField]
    public float MilkingFraction = 0.5f;

    /// <summary>
    /// Кулдаун между сцеживаниями.
    /// </summary>
    [DataField]
    public TimeSpan MilkingCooldown = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Когда можно будет сцедить молоко снова (только сервер).
    /// </summary>
    [ViewVariables]
    public TimeSpan NextMilking;

    /// <summary>
    /// Когда следующий тик выработки (только сервер).
    /// </summary>
    [ViewVariables]
    public TimeSpan NextProduction;
}
