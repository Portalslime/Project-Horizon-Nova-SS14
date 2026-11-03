using System.Diagnostics.CodeAnalysis;
using Content.Server.Fluids.EntitySystems;
using Content.Shared._HorizonNova.Lactation;
using Content.Shared._Shitmed.Humanoid.Events;
using Content.Shared.Alert;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
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
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    /// <summary>
    /// Слоты, которые должны быть пустыми, чтобы грудь считалась открытой.
    /// </summary>
    private static readonly string[] ChestSlots = { "underweart", "jumpsuit", "outerClothing" };

    /// <summary>
    /// Длительность сцеживания.
    /// </summary>
    private static readonly TimeSpan MilkingDelay = TimeSpan.FromSeconds(2);

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
        SubscribeLocalEvent<LactationComponent, GetVerbsEvent<Verb>>(OnGetMilkingVerbs);
        SubscribeLocalEvent<LactationComponent, LactationMilkingDoAfterEvent>(OnMilkingDoAfter);
        SubscribeLocalEvent<LactationComponent, ExaminedEvent>(OnExamined);
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
    /// ПКМ-верб «сцедить молоко»: только для женщин четырёх рас с грудью.
    /// Верб виден всегда, но заблокирован, если грудь закрыта одеждой или молока нет.
    /// </summary>
    private void OnGetMilkingVerbs(Entity<LactationComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (!TryComp<HumanoidAppearanceComponent>(ent.Owner, out var humanoid))
            return;

        if (!LactationHelpers.CanMilk(humanoid))
            return;

        var target = ent.Owner;
        var user = args.User;
        var reason = GetMilkingBlockReason(target, ent.Comp);

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("lactation-verb-milk", ("amount", ent.Comp.MilkVolume)),
            Disabled = reason != null,
            Message = reason ?? Loc.GetString("lactation-verb-milk-desc"),
            Priority = 2,
            Impact = LogImpact.Low,
            Act = () => TryStartMilking(user, target, ent.Comp),
        });
    }

    /// <summary>
    /// Причина, по которой сцеживание сейчас недоступно, либо null, если всё в порядке.
    /// </summary>
    private string? GetMilkingBlockReason(EntityUid target, LactationComponent comp)
    {
        if (!IsChestExposed(target))
            return Loc.GetString("lactation-needs-exposed");

        if (comp.MilkVolume <= FixedPoint2.Zero)
            return Loc.GetString("lactation-milking-empty", ("target", Identity.Entity(target, EntityManager)));

        return null;
    }

    /// <summary>
    /// Грудь открыта, если пусты слоты белья, комбинезона и верхней одежды.
    /// </summary>
    private bool IsChestExposed(EntityUid uid)
    {
        foreach (var slot in ChestSlots)
        {
            if (_inventory.TryGetSlotEntity(uid, slot, out _))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Запускает сцеживание с задержкой; повторно проверяет условия в do-after.
    /// </summary>
    private void TryStartMilking(EntityUid user, EntityUid target, LactationComponent comp)
    {
        if (GetMilkingBlockReason(target, comp) != null)
            return;

        var doAfter = new DoAfterArgs(EntityManager, user, MilkingDelay, new LactationMilkingDoAfterEvent(), target, target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            DistanceThreshold = 2f,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    /// <summary>
    /// Завершение сцеживания: выдаивает всё молоко, что сейчас есть в груди
    /// (но не больше свободного места в ёмкости), в ёмкость в руке или на пол.
    /// </summary>
    private void OnMilkingDoAfter(Entity<LactationComponent> ent, ref LactationMilkingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var user = args.User;
        var target = ent.Owner;

        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoid) || !LactationHelpers.CanMilk(humanoid))
            return;

        if (!IsChestExposed(target))
            return;

        if (!_solutions.ResolveSolution(target, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution))
            return;

        if (solution.Volume <= FixedPoint2.Zero)
        {
            PopupMilkingEmpty(user, target);
            return;
        }

        // Сколько выдаивается, зависит от того, сколько молока сейчас в груди.
        var amount = solution.Volume;

        if (TryFindHeldContainer(user, out var held, out var containerSoln, out var containerSolution))
        {
            if (containerSolution.AvailableVolume <= FixedPoint2.Zero)
            {
                _popup.PopupEntity(Loc.GetString("lactation-container-full"), target, user);
                return;
            }

            if (amount > containerSolution.AvailableVolume)
                amount = containerSolution.AvailableVolume;

            var intoContainer = _solutions.SplitSolution(ent.Comp.Solution!.Value, amount);
            if (!_solutions.TryAddSolution(containerSoln.Value, intoContainer))
            {
                // Ёмкость могла заполниться между проверкой и переносом — возвращаем молоко в грудь.
                _solutions.TryAddSolution(ent.Comp.Solution!.Value, intoContainer);
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
            var spill = _solutions.SplitSolution(ent.Comp.Solution!.Value, amount);
            _puddle.TrySpillAt(target, spill, out _);

            _popup.PopupEntity(
                Loc.GetString("lactation-milking-spill",
                    ("amount", spill.Volume),
                    ("target", Identity.Entity(target, EntityManager))),
                target,
                user,
                PopupType.Medium);
        }

        SyncVolume(target, ent.Comp, solution);
        args.Handled = true;
    }

    /// <summary>
    /// Ищет ёмкость с refillable-раствором в руках пользователя:
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
    /// Индикатор в осмотре: сколько молока в груди, максимум и размер груди.
    /// </summary>
    private void OnExamined(Entity<LactationComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryComp<HumanoidAppearanceComponent>(ent.Owner, out var humanoid) || !LactationHelpers.CanMilk(humanoid))
            return;

        var size = LactationHelpers.GetBreastSize(humanoid);
        var maxMilk = LactationHelpers.GetMaxMilk(size, ent.Comp.BaseMaxMilk, ent.Comp.MaxMilkPerSize);
        if (maxMilk <= FixedPoint2.Zero)
            return;

        args.PushMarkup(Loc.GetString("lactation-examined-milk",
            ("amount", ent.Comp.MilkVolume),
            ("max", maxMilk),
            ("size", size)));
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
