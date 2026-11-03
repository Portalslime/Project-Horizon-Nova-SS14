using Content.Shared._HorizonNova.Lactation;
using Content.Shared._Shitmed.Humanoid.Events;
using Content.Shared.Alert;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._HorizonNova.Lactation;

/// <summary>
/// Лактация: пассивная выработка молока в раствор на теле и алерт о наличии молока.
/// </summary>
public sealed class LactationSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    /// <summary>
    /// Размер груди, который считается «нормальным» и не даёт штрафа к скорости.
    /// </summary>
    private const int NormalBreastSize = 2;

    /// <summary>
    /// Штраф к скорости за каждый размер груди сверх нормального.
    /// </summary>
    private const float SpeedPenaltyPerExtraSize = 0.03f;

    /// <summary>
    /// Категория алертов лактации.
    /// </summary>
    private static readonly ProtoId<AlertCategoryPrototype> AlertCategory = "Lactation";

    /// <summary>
    /// Алерт «в груди есть молоко».
    /// </summary>
    private static readonly ProtoId<AlertPrototype> AlertMilk = "LactationMilk";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LactationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LactationComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LactationComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<LactationComponent, HumanoidMarkingSetChangedEvent>(OnMarkingSetChanged);
        SubscribeLocalEvent<LactationComponent, ProfileLoadFinishedEvent>(OnProfileLoadFinished);
        SubscribeLocalEvent<LactationComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    /// <summary>
    /// Большая грудь немного замедляет: каждый размер сверх нормального — −3% к скорости.
    /// </summary>
    private void OnRefreshSpeed(Entity<LactationComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
            return;

        var extraSize = LactationHelpers.GetBreastSize(humanoid) - NormalBreastSize;
        if (extraSize <= 0)
            return;

        var modifier = Math.Max(0.5f, 1f - extraSize * SpeedPenaltyPerExtraSize);
        args.ModifySpeed(modifier, modifier);
    }

    private void OnMobStateChanged(Entity<LactationComponent> ent, ref MobStateChangedEvent args)
    {
        if (_mobState.IsDead(ent))
            _alerts.ClearAlertCategory(ent.Owner, AlertCategory);
        else
            UpdateAlert(ent.Owner, ent.Comp);
    }

    private void OnShutdown(Entity<LactationComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlertCategory(ent.Owner, AlertCategory);
    }

    private void OnMapInit(Entity<LactationComponent> ent, ref MapInitEvent args)
    {
        // Раствор создаётся с запасом на максимальный размер груди (5) из текущих DataField'ов.
        var maxVolume = LactationHelpers.GetMaxMilk(5, ent.Comp.BaseMaxMilk, ent.Comp.MaxMilkPerSize);
        _solutions.EnsureSolution(ent.Owner, ent.Comp.SolutionName, out _, maxVol: maxVolume);
        ent.Comp.NextProduction = _timing.CurTime + ent.Comp.ProductionInterval;

        RefreshAppearance(ent);
    }

    private void OnMarkingSetChanged(Entity<LactationComponent> ent, ref HumanoidMarkingSetChangedEvent args)
    {
        RefreshAppearance(ent);
    }

    private void OnProfileLoadFinished(Entity<LactationComponent> ent, ref ProfileLoadFinishedEvent args)
    {
        RefreshAppearance(ent);
    }

    /// <summary>
    /// Обновляет состояние раствора/алерта и скорость после смены внешности.
    /// Размер груди не хранится — он считается по marking'ам при необходимости.
    /// </summary>
    private void RefreshAppearance(Entity<LactationComponent> ent)
    {
        if (_solutions.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution))
            ent.Comp.MilkVolume = solution.Volume;

        Dirty(ent.Owner, ent.Comp);
        UpdateAlert(ent.Owner, ent.Comp);
        _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<LactationComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_mobState.IsDead(uid))
                continue;

            if (now < comp.NextProduction)
                continue;

            comp.NextProduction += comp.ProductionInterval;
            Produce(uid, comp);
        }
    }

    /// <summary>
    /// Один тик пассивной выработки: молоко добавляется в раствор, пока есть место,
    /// пока персонаж жив и не голоден.
    /// </summary>
    private void Produce(EntityUid uid, LactationComponent comp)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        var size = LactationHelpers.GetBreastSize(humanoid);
        if (size <= 0)
            return;

        var maxMilk = LactationHelpers.GetMaxMilk(size, comp.BaseMaxMilk, comp.MaxMilkPerSize);

        if (!_solutions.ResolveSolution(uid, comp.SolutionName, ref comp.Solution, out var solution))
            return;

        if (solution.Volume >= maxMilk)
        {
            SyncVolume(uid, comp, solution);
            return;
        }

        // Без запаса сытости/питья молоко не вырабатывается (как у UdderSystem).
        if (!HasEnoughNutrition(uid))
            return;

        var amount = LactationHelpers.GetProductionAmount(size, comp.MilkPerSizePerSecond, comp.ProductionInterval);

        var freeSpace = maxMilk - solution.Volume;
        if (amount > freeSpace)
            amount = freeSpace;

        if (amount <= FixedPoint2.Zero)
            return;

        if (!_solutions.TryAddReagent(comp.Solution.Value, comp.ReagentId, amount, out var accepted) || accepted <= FixedPoint2.Zero)
            return;

        // Ресурсы тратятся только на то молоко, которое действительно попало в раствор.
        if (TryComp<HungerComponent>(uid, out var hunger))
            _hunger.ModifyHunger(uid, -(accepted.Float() * comp.HungerUsagePerUnit), hunger);

        if (TryComp<ThirstComponent>(uid, out var thirst))
            _thirst.ModifyThirst(uid, thirst, -(accepted.Float() * comp.ThirstUsagePerUnit));

        SyncVolume(uid, comp, solution);
    }

    /// <summary>
    /// Есть ли у персонажа достаточная сытость для выработки молока.
    /// Персонажи без <see cref="HungerComponent"/> не ограничены.
    /// </summary>
    private bool HasEnoughNutrition(EntityUid uid)
    {
        if (TryComp<HungerComponent>(uid, out var hunger) && _hunger.GetHungerThreshold(hunger) < HungerThreshold.Okay)
            return false;

        if (TryComp<ThirstComponent>(uid, out var thirst) && thirst.CurrentThirstThreshold < ThirstThreshold.Okay)
            return false;

        return true;
    }

    /// <summary>
    /// Синхронизирует зеркало объёма молока с раствором и обновляет состояние/алерт.
    /// </summary>
    private void SyncVolume(EntityUid uid, LactationComponent comp, Solution solution)
    {
        if (comp.MilkVolume == solution.Volume)
            return;

        comp.MilkVolume = solution.Volume;
        Dirty(uid, comp);
        UpdateAlert(uid, comp);
    }

    /// <summary>
    /// Показывает алерт, если молоко есть, иначе убирает всю категорию.
    /// </summary>
    private void UpdateAlert(EntityUid uid, LactationComponent comp)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
        {
            _alerts.ClearAlertCategory(uid, AlertCategory);
            return;
        }

        var size = LactationHelpers.GetBreastSize(humanoid);
        if (size <= 0 || comp.MilkVolume <= FixedPoint2.Zero)
        {
            _alerts.ClearAlertCategory(uid, AlertCategory);
            return;
        }

        // Шкала наполненности: 1..5 уровней, как иконки fill-1..fill-5.
        var maxMilk = LactationHelpers.GetMaxMilk(size, comp.BaseMaxMilk, comp.MaxMilkPerSize);
        if (maxMilk <= FixedPoint2.Zero)
            maxMilk = FixedPoint2.New(1);

        var ratio = comp.MilkVolume.Float() / maxMilk.Float();
        var severity = (short) Math.Clamp((int) Math.Ceiling(ratio * 5), 1, 5);
        _alerts.ShowAlert(uid, AlertMilk, severity);
    }
}
