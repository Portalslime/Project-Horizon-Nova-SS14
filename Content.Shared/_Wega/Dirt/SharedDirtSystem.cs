using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Foldable;
using Content.Shared.Inventory;
using Content.Shared.Standing;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.DirtVisuals;

// Порт с ss14-wega (wega-team/ss14-wega), Content.Shared/_Wega/Dirt/SharedDirtSystem.cs
// Адаптация под HN: кровь берётся из BloodstreamComponent.BloodReagent (в Wega было BloodReferenceSolution).
public sealed partial class SharedDirtSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    public const float MaxDirtLevel = 100f;
    private const float DirtAccumulationRate = 0.01f;

    /// <summary>
    /// HN: во сколько раз сильнее пачкает (и смывает) контакт с лужей/душем. В Wega было 1,
    /// при этом пачкалась только обувь и незаметно.
    /// </summary>
    private const float ContactDirtMultiplier = 10f;

    /// <summary>
    /// HN: множитель душа — он моет верхние слои заметно сильнее контакта с лужей.
    /// </summary>
    private const float ShowerDirtMultiplier = 50f;

    // HN: защита нижних слоёв верхними (замедляет и пачкание, и смывание).
    private const float OuterClothingProtection = 0.5f; // куртка -> комбинезон: на 50% медленнее
    private const float JumpsuitProtection = 0.1f;      // комбинезон -> бельё: на 90% медленнее
    private const float ShoesProtection = 0.1f;         // обувь -> носки: на 90% медленнее

    private ProtoId<TagPrototype> _hardsuit = "Hardsuit";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DirtableComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DirtableComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<DirtableComponent, FoldedEvent>(OnFolded);
        SubscribeLocalEvent<DirtableComponent, ExaminedEvent>(OnExamined);
    }

    private void OnInit(EntityUid uid, DirtableComponent comp, ref ComponentInit args)
        => Dirty(uid, comp);

    private void OnGetState(EntityUid uid, DirtableComponent comp, ref ComponentGetState args)
    {
        args.State = new DirtableComponentState(
            comp.CurrentDirtLevel,
            comp.DirtColor,
            comp.IsDirty
        );
    }

    private void OnFolded(EntityUid uid, DirtableComponent comp, ref FoldedEvent args)
    {
        // Finally update this shit
        Dirty(uid, comp);
    }

    private void OnExamined(Entity<DirtableComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || entity.Comp.CurrentDirtLevel < entity.Comp.Threshold)
            return;

        float dirtPercentage = Math.Clamp(entity.Comp.CurrentDirtLevel.Float() / MaxDirtLevel * 100f, 0f, 100f);
        string colorHex = entity.Comp.DirtColor.ToHex();

        string dirtLevel = dirtPercentage switch
        {
            < 30 => Loc.GetString("dirt-examined-level-low"),
            < 70 => Loc.GetString("dirt-examined-level-medium"),
            _ => Loc.GetString("dirt-examined-level-high")
        };

        args.PushMarkup(
            Loc.GetString("dirt-examined-message", ("color", colorHex), ("percentage", (int)dirtPercentage), ("level", dirtLevel))
        );
    }

    public void AddBloodDirtFromDamage(EntityUid target, EntityUid attacker, DamageSpecifier damage, bool isGunshot = false)
    {
        if (!TryComp<BloodstreamComponent>(target, out var bloodstream))
            return;

        var bloodReagentId = bloodstream.BloodReagent.Id;

        if (string.IsNullOrEmpty(bloodReagentId))
            return;

        var bloodTypes = new[] { "Slash", "Piercing", "Blunt" };
        FixedPoint2 bloodAmount = 0;
        foreach (var type in bloodTypes)
        {
            if (damage.DamageDict.TryGetValue(type, out var amount))
                bloodAmount += amount;
        }

        if (bloodAmount <= 0)
            return;

        var bloodSolution = new Solution();
        bloodSolution.AddReagent(bloodReagentId, bloodAmount * (0.2f / DirtAccumulationRate));

        var slots = new List<string> { "outerClothing", "jumpsuit", "gloves", "belt", "mask", "head" };
        ApplyDirtToClothing(target, bloodSolution, _random.Pick(slots));

        if (attacker != target)
        {
            if (isGunshot)
            {
                var targetPosition = _transform.GetWorldPosition(target);
                var attackerPosition = _transform.GetWorldPosition(attacker);
                var distance = (attackerPosition - targetPosition).Length();
                if (distance > 2.5f)
                    return;
            }
            ApplyDirtToClothing(attacker, bloodSolution, _random.Pick(slots));
        }
    }

    public void ApplyDirtToClothing(EntityUid wearer, Solution solution)
    {
        // HN: контакт с лужей пачкает не только обувь — бельё и комбинезон в первую очередь,
        // как и испражнения (в Wega при ходьбе пачкалась только обувь). Головные уборы тоже пачкаются.
        var slots = new List<string> { "underwearb", "underweart", "jumpsuit", "socks", "shoes", "head" };

        var isLyingDown = TryComp<StandingStateComponent>(wearer, out var standing) && !standing.Standing;
        if (isLyingDown)
        {
            slots.AddRange(new[] { "outerClothing", "gloves", "belt", "mask" });
        }
        else
        {
            if (_inventory.TryGetSlotEntity(wearer, "outerClothing", out var outerClothing)
                && _tag.HasTag(outerClothing.Value, _hardsuit))
                slots.Add("outerClothing");
        }

        foreach (var slot in slots)
        {
            if (_inventory.TryGetSlotEntity(wearer, slot, out var clothing))
            {
                var rate = ContactDirtMultiplier * GetLayerDirtMultiplier(wearer, slot);
                UpdateDirtFromSolution(clothing.Value, solution, rate);
            }
        }
    }

    public void ApplyDirtToClothing(EntityUid wearer, Solution solution, string slot)
    {
        if (_inventory.TryGetSlotEntity(wearer, slot, out var clothing))
            UpdateDirtFromSolution(clothing.Value, solution, GetLayerDirtMultiplier(wearer, slot));
    }

    /// <summary>
    /// HN: душ обрабатывает только верхние (внешние) слои — бельё и носки под ними остаются грязными.
    /// </summary>
    public void ApplyDirtToUpperClothing(EntityUid wearer, Solution solution)
    {
        var slots = new[] { "outerClothing", "jumpsuit", "gloves", "head", "mask", "shoes" };
        foreach (var slot in slots)
        {
            if (_inventory.TryGetSlotEntity(wearer, slot, out var clothing))
                UpdateDirtFromSolution(clothing.Value, solution, ShowerDirtMultiplier);
        }
    }

    /// <summary>
    /// HN: верхняя одежда прикрывает нижние слои — те пачкаются (и отмываются) медленнее.
    /// Куртка -> комбинезон ×0.5, комбинезон -> бельё ×0.1, обувь -> носки ×0.1.
    /// </summary>
    private float GetLayerDirtMultiplier(EntityUid wearer, string slot)
    {
        switch (slot)
        {
            case "jumpsuit":
                if (_inventory.TryGetSlotEntity(wearer, "outerClothing", out _))
                    return OuterClothingProtection;
                break;
            case "underwearb":
            case "underweart":
                if (_inventory.TryGetSlotEntity(wearer, "jumpsuit", out _))
                    return JumpsuitProtection;
                break;
            case "socks":
                if (_inventory.TryGetSlotEntity(wearer, "shoes", out _))
                    return ShoesProtection;
                break;
        }

        return 1f;
    }

    private void UpdateDirtFromSolution(EntityUid uid, Solution solution, float rateMultiplier = 1f, DirtableComponent? comp = null)
    {
        if (!TryComp(uid, out comp) || solution.Volume == 0)
            return;

        bool isOnlyWater = solution.Contents.Count == 1
            && solution.Contents[0].Reagent.Prototype == "Water";

        if (isOnlyWater)
        {
            float removedDirt = Math.Min(solution.Volume.Float() * DirtAccumulationRate * 2f * rateMultiplier, comp.CurrentDirtLevel.Float());
            CleanDirt(uid, removedDirt);
            return;
        }

        float addedDirt = Math.Min(solution.Volume.Float() * DirtAccumulationRate * rateMultiplier, 10f * rateMultiplier);
        comp.CurrentDirtLevel = FixedPoint2.New(
            Math.Min(comp.CurrentDirtLevel.Float() + addedDirt, MaxDirtLevel)
        );

        if (comp.CurrentDirtLevel < MaxDirtLevel)
        {
            var colors = new List<Color>();
            foreach (var reagent in solution.Contents)
            {
                if (reagent.Reagent.Prototype == "Water")
                    continue;

                if (!_prototype.TryIndex<ReagentPrototype>(reagent.Reagent.Prototype, out var proto))
                    continue;

                colors.Add(proto.SubstanceColor);
            }

            if (colors.Count > 0)
                comp.DirtColor = BlendColorsProperly(comp.DirtColor, colors);
        }

        // HN: состояние шлём всегда — иначе при пустом списке цветов клиент не увидит грязь,
        // а при достижении максимума уровень не дойдёт до клиента.
        Dirty(uid, comp);
    }

    public void CleanDirt(EntityUid uid, float amount, float efficiency = 1f, DirtableComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false) || amount <= 0)
            return;

        var actualRemoval = amount * efficiency;
        comp.CurrentDirtLevel = FixedPoint2.Max(comp.CurrentDirtLevel - actualRemoval, 0);

        if (comp.CurrentDirtLevel <= 0)
            comp.DirtColor = Color.White;

        Dirty(uid, comp);
    }

    /// <summary>
    /// HN: цвет реагента-прототипа (нужен там, где нет готового раствора — испражнения, сперма).
    /// </summary>
    public Color GetReagentColor(string reagentId)
    {
        return _prototype.TryIndex<ReagentPrototype>(reagentId, out var proto)
            ? proto.SubstanceColor
            : Color.White;
    }

    /// <summary>
    /// HN: добавить грязь напрямую, без раствора.
    /// </summary>
    public void AddDirt(EntityUid uid, float amount, Color? color = null, DirtableComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false) || amount <= 0)
            return;

        comp.CurrentDirtLevel = FixedPoint2.New(
            Math.Min(comp.CurrentDirtLevel.Float() + amount, MaxDirtLevel));

        if (color is { } newColor)
            comp.DirtColor = BlendColorsProperly(comp.DirtColor, new List<Color> { newColor });

        Dirty(uid, comp);
    }

    /// <summary>
    /// HN: добавить грязь предмету в конкретном слоте wearer (например, комбинезону).
    /// </summary>
    public void AddDirtToSlot(EntityUid wearer, string slot, float amount, Color? color = null)
    {
        if (_inventory.TryGetSlotEntity(wearer, slot, out var clothing))
            AddDirt(clothing.Value, amount, color);
    }

    /// <summary>
    /// HN: добавить грязь сразу нескольким слотам в заданном порядке (первый — приоритетный).
    /// </summary>
    public void AddDirtToSlots(EntityUid wearer, float amount, Color? color, params string[] slots)
    {
        foreach (var slot in slots)
            AddDirtToSlot(wearer, slot, amount, color);
    }

    private Color BlendColorsProperly(Color currentColor, List<Color> newColors)
    {
        if (newColors.Count == 1 && currentColor == Color.White)
            return newColors[0];

        float rNew = 0, gNew = 0, bNew = 0;
        foreach (var color in newColors)
        {
            rNew += color.R / 255f;
            gNew += color.G / 255f;
            bNew += color.B / 255f;
        }
        rNew /= newColors.Count;
        gNew /= newColors.Count;
        bNew /= newColors.Count;

        float rCurrent = currentColor.R / 255f;
        float gCurrent = currentColor.G / 255f;
        float bCurrent = currentColor.B / 255f;

        float r = rCurrent * 0.85f + rNew * 0.15f;
        float g = gCurrent * 0.85f + gNew * 0.15f;
        float b = bCurrent * 0.85f + bNew * 0.15f;

        r = Math.Clamp(r, 0f, 1f) * 255f;
        g = Math.Clamp(g, 0f, 1f) * 255f;
        b = Math.Clamp(b, 0f, 1f) * 255f;

        return new Color(r, g, b);
    }
}
