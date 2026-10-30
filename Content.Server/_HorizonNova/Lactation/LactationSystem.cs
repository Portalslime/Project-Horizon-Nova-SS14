using System.Diagnostics.CodeAnalysis;
using Content.Server.Fluids.EntitySystems;
using Content.Shared._Arcane.ERP;
using Content.Shared._HorizonNova.Lactation;
using Content.Shared._Shitmed.Humanoid.Events;
using Content.Shared.Alert;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DirtVisuals;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._HorizonNova.Lactation;

/// <summary>
/// Лактация: пассивная выработка молока в раствор на теле, алерт о наличии молока
/// и сцеживание по <see cref="ErpMilkCollectionEvent"/> — в ёмкость в руке или на пол.
/// </summary>
public sealed class LactationSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedDirtSystem _dirt = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

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
        SubscribeLocalEvent<ArousalComponent, ErpMilkCollectionEvent>(OnMilkCollection);
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

        if (TryComp<HumanoidAppearanceComponent>(ent.Owner, out var humanoid))
            Recompute(ent.Owner, ent.Comp, humanoid);
    }

    private void OnMarkingSetChanged(Entity<LactationComponent> ent, ref HumanoidMarkingSetChangedEvent args)
    {
        if (TryComp<HumanoidAppearanceComponent>(ent.Owner, out var humanoid))
            Recompute(ent.Owner, ent.Comp, humanoid);
    }

    private void OnProfileLoadFinished(Entity<LactationComponent> ent, ref ProfileLoadFinishedEvent args)
    {
        if (TryComp<HumanoidAppearanceComponent>(ent.Owner, out var humanoid))
            Recompute(ent.Owner, ent.Comp, humanoid);
    }

    /// <summary>
    /// Пересчитывает размер груди и максимальный запас молока по внешности персонажа.
    /// </summary>
    private void Recompute(EntityUid uid, LactationComponent comp, HumanoidAppearanceComponent humanoid)
    {
        comp.BreastSize = LactationHelpers.GetBreastSize(humanoid);
        comp.MaxMilk = LactationHelpers.GetMaxMilk(comp.BreastSize, comp.BaseMaxMilk, comp.MaxMilkPerSize);

        if (_solutions.ResolveSolution(uid, comp.SolutionName, ref comp.Solution, out var solution))
            comp.MilkVolume = solution.Volume;

        Dirty(uid, comp);
        UpdateAlert(uid, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<LactationComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.BreastSize <= 0)
                continue;

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
        if (!_solutions.ResolveSolution(uid, comp.SolutionName, ref comp.Solution, out var solution))
            return;

        if (solution.Volume >= comp.MaxMilk)
        {
            SyncVolume(uid, comp, solution);
            return;
        }

        // Без запаса сытости молоко не вырабатывается (как у UdderSystem).
        if (!HasEnoughNutrition(uid))
            return;

        var amount = LactationHelpers.GetProductionAmount(comp.BreastSize, comp.MilkPerSizePerSecond, comp.ProductionInterval);

        var freeSpace = comp.MaxMilk - solution.Volume;
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
    /// Сцеживание по запросу ERP-панели: в ёмкость в руке пользователя либо разливом на пол.
    /// </summary>
    private void OnMilkCollection(Entity<ArousalComponent> ent, ref ErpMilkCollectionEvent args)
    {
        var user = args.User;
        var target = args.Target;

        if (!TryComp<LactationComponent>(target, out var comp))
            return;

        if (comp.BreastSize <= 0)
        {
            PopupMilkingEmpty(user, target);
            return;
        }

        if (!_solutions.ResolveSolution(target, comp.SolutionName, ref comp.Solution, out var solution))
        {
            PopupMilkingEmpty(user, target);
            return;
        }

        if (solution.Volume <= FixedPoint2.Zero)
        {
            PopupMilkingEmpty(user, target);
            return;
        }

        var amount = LactationHelpers.GetMilkingAmount(comp.BreastSize, comp.MilkingAmounts);
        if (amount > solution.Volume)
            amount = solution.Volume;

        if (TryFindHeldContainer(user, out var held, out var containerSoln, out var containerSolution))
        {
            if (containerSolution.AvailableVolume <= FixedPoint2.Zero)
            {
                _popup.PopupEntity(Loc.GetString("lactation-container-full"), target, user);
                return;
            }

            if (amount > containerSolution.AvailableVolume)
                amount = containerSolution.AvailableVolume;

            var intoContainer = _solutions.SplitSolution(comp.Solution.Value, amount);
            if (!_solutions.TryAddSolution(containerSoln.Value, intoContainer))
            {
                // Ёмкость могла заполниться между проверкой и переносом — возвращаем молоко в грудь.
                _solutions.TryAddSolution(comp.Solution.Value, intoContainer);
                _popup.PopupEntity(Loc.GetString("lactation-container-full"), target, user);
                return;
            }

            _popup.PopupEntity(
                Loc.GetString("lactation-milking-into-container",
                    ("amount", intoContainer.Volume),
                    ("container", Identity.Entity(held, EntityManager)),
                    ("target", Identity.Entity(target, EntityManager))),
                target,
                user,
                PopupType.Medium);
        }
        else
        {
            var spill = _solutions.SplitSolution(comp.Solution.Value, amount);
            _puddle.TrySpillAt(target, spill, out _);

            _dirt.AddDirtToSlotsFromReagent(target, spill.Volume.Float() * 2f, comp.ReagentId.Id,
                "underweart", "jumpsuit", "outerClothing");

            _popup.PopupEntity(
                Loc.GetString("lactation-milking-spill",
                    ("amount", spill.Volume),
                    ("target", Identity.Entity(target, EntityManager))),
                target,
                user,
                PopupType.Medium);
        }

        SyncVolume(target, comp, solution);
    }

    /// <summary>
    /// Ищет любую ёмкость с refillable-раствором в руках пользователя:
    /// сначала активная рука, затем остальные.
    /// </summary>
    private bool TryFindHeldContainer(EntityUid user, out EntityUid held,
        [NotNullWhen(true)] out Entity<SolutionComponent>? containerSoln,
        [NotNullWhen(true)] out Solution? containerSolution)
    {
        held = default;
        containerSoln = null;
        containerSolution = null;

        if (_hands.TryGetActiveItem(user, out var activeItem)
            && _solutions.TryGetRefillableSolution(activeItem.Value, out containerSoln, out containerSolution))
        {
            held = activeItem.Value;
            return true;
        }

        foreach (var hand in _hands.EnumerateHands(user))
        {
            if (!_hands.TryGetHeldItem(user, hand, out var item))
                continue;

            if (activeItem != null && item.Value == activeItem.Value)
                continue;

            if (!_solutions.TryGetRefillableSolution(item.Value, out containerSoln, out containerSolution))
                continue;

            held = item.Value;
            return true;
        }

        return false;
    }

    private void PopupMilkingEmpty(EntityUid user, EntityUid target)
    {
        _popup.PopupEntity(
            Loc.GetString("lactation-milking-empty", ("target", Identity.Entity(target, EntityManager))),
            target,
            user);
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
        if (comp.BreastSize <= 0 || comp.MilkVolume <= FixedPoint2.Zero)
        {
            _alerts.ClearAlertCategory(uid, AlertCategory);
            return;
        }

        // Шкала наполненности: 1..5 уровней, как иконки fill-1..fill-5.
        var maxMilk = comp.MaxMilk > FixedPoint2.Zero ? comp.MaxMilk : FixedPoint2.New(1);
        var ratio = comp.MilkVolume.Float() / maxMilk.Float();
        var severity = (short) Math.Clamp((int) Math.Ceiling(ratio * 5), 1, 5);
        _alerts.ShowAlert(uid, AlertMilk, severity);
    }
}
