using Content.Server._Horizon.Husbandry.Needs;
using Content.Shared._Horizon.Husbandry.Feeding;
using Content.Shared._Horizon.Husbandry.Needs;
using Content.Shared._Horizon.Husbandry.Visuals;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server._Horizon.Husbandry.Feeding;

/// <summary>
/// Lets animals eat food that fits their diet and drink from water sources. This replaces the usual eating
/// and drinking for them, it only touches their own satiety and hydration.
/// </summary>
public sealed class AnimalFeedingSystem : EntitySystem
{
    [Dependency] private readonly AnimalNeedsSystem _needs = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DietComponent, AnimalEatDoAfterEvent>(OnEaten);
        SubscribeLocalEvent<AnimalNeedsComponent, AnimalDrinkDoAfterEvent>(OnDrunk);
    }

    /// <summary>
    /// Whether the animal may eat this. Does not check if it is hungry or can reach it.
    /// </summary>
    public bool CanEat(Entity<DietComponent> animal, EntityUid food)
    {
        if (!HasComp<FoodComponent>(food) || _container.IsEntityInContainer(food))
            return false;

        if (!_whitelist.IsWhitelistPassOrNull(animal.Comp.Whitelist, food) ||
            !_whitelist.IsBlacklistFailOrNull(animal.Comp.Blacklist, food))
            return false;

        return GetNutrition(animal, food) > 0f;
    }

    /// <summary>
    /// How much satiety the animal gets out of the food.
    /// </summary>
    public float GetNutrition(Entity<DietComponent> animal, EntityUid food)
    {
        if (!_solutions.TryGetSolution(food, animal.Comp.Solution, out _, out var solution))
            return 0f;

        return solution.Volume.Float() * animal.Comp.NutritionPerUnit;
    }

    /// <summary>
    /// Whether the source has enough liquid to drink from.
    /// </summary>
    public bool CanDrink(Entity<WaterSourceComponent> source)
    {
        return _solutions.TryGetSolution(source.Owner, source.Comp.Solution, out _, out var solution) &&
               solution.Volume > 0;
    }

    /// <summary>
    /// Starts eating. The animal gets the food once the do-after finishes.
    /// </summary>
    /// <param name="delay">How long it takes.</param>
    public bool TryEat(EntityUid animal, EntityUid food, out TimeSpan delay)
    {
        delay = TimeSpan.Zero;

        if (!TryComp<DietComponent>(animal, out var diet) ||
            !HasComp<AnimalNeedsComponent>(animal) ||
            _mobState.IsIncapacitated(animal) ||
            !CanEat((animal, diet), food))
            return false;

        var args = new DoAfterArgs(EntityManager, animal, diet.EatDelay, new AnimalEatDoAfterEvent(), animal, target: food)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = false,
            BreakOnDropItem = false,
            DistanceThreshold = 2f,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return false;

        delay = diet.EatDelay;
        _appearance.SetData(animal, AnimalVisuals.Eating, true);
        return true;
    }

    /// <summary>
    /// Starts drinking. The animal gets the water once the do-after finishes.
    /// </summary>
    /// <param name="delay">How long it takes.</param>
    public bool TryDrink(EntityUid animal, EntityUid source, out TimeSpan delay)
    {
        delay = TimeSpan.Zero;

        if (!TryComp<WaterSourceComponent>(source, out var water) ||
            !HasComp<AnimalNeedsComponent>(animal) ||
            _mobState.IsIncapacitated(animal) ||
            !CanDrink((source, water)))
            return false;

        var args = new DoAfterArgs(EntityManager, animal, water.Delay, new AnimalDrinkDoAfterEvent(), animal, target: source)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = false,
            BreakOnDropItem = false,
            DistanceThreshold = 2f,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return false;

        delay = water.Delay;
        _appearance.SetData(animal, AnimalVisuals.Eating, true);
        return true;
    }

    private void OnEaten(Entity<DietComponent> ent, ref AnimalEatDoAfterEvent args)
    {
        _appearance.SetData(ent, AnimalVisuals.Eating, false);

        if (args.Cancelled || args.Handled || args.Target is not { } food)
            return;

        args.Handled = true;

        if (!TryComp<AnimalNeedsComponent>(ent, out var needs) || !CanEat(ent, food))
            return;

        var nutrition = GetNutrition(ent, food);
        _needs.ModifySatiety((ent.Owner, needs), nutrition);
        _audio.PlayPvs(ent.Comp.EatSound, ent);
        QueueDel(food);

        var ev = new AnimalAteEvent(food, nutrition);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnDrunk(Entity<AnimalNeedsComponent> ent, ref AnimalDrinkDoAfterEvent args)
    {
        _appearance.SetData(ent, AnimalVisuals.Eating, false);

        if (args.Cancelled || args.Handled || args.Target is not { } source)
            return;

        args.Handled = true;

        if (!TryComp<WaterSourceComponent>(source, out var water) ||
            !_solutions.TryGetSolution(source, water.Solution, out var soln, out _))
            return;

        var taken = _solutions.SplitSolution(soln.Value, FixedPoint2.New(water.AmountPerDrink)).Volume.Float();
        if (taken <= 0f)
            return;

        _needs.ModifyHydration(ent, taken * water.HydrationPerUnit);
        _audio.PlayPvs(water.DrinkSound, ent);

        var ev = new AnimalDrankEvent(source, taken);
        RaiseLocalEvent(ent, ref ev);
    }
}
