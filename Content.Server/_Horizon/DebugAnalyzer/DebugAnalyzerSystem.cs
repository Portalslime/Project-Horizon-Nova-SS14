using System.Globalization;
using Content.Server.Temperature.Components;
using Content.Shared._Horizon.DebugAnalyzer;
using Content.Shared._Horizon.Defecation;
using Content.Shared._Horizon.Husbandry.Growth;
using Content.Shared._Horizon.Husbandry.Needs;
using Content.Shared._Horizon.Husbandry.Production;
using Content.Shared._Horizon.Husbandry.Rideable;
using Content.Shared._Horizon.Husbandry.Sex;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.DebugAnalyzer;

/// <summary>
/// Scans a living being with a <see cref="DebugAnalyzerComponent"/> and keeps the open window up to date.
/// </summary>
public sealed class DebugAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private const string ReportPrefix = "debug-analyzer";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DebugAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DebugAnalyzerComponent, UseInHandEvent>(OnUseInHand);
        Subs.BuiEvents<DebugAnalyzerComponent>(DebugAnalyzerUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnUiClosed);
        });
    }

    private void OnAfterInteract(Entity<DebugAnalyzerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach || !HasComp<MobStateComponent>(target))
            return;

        args.Handled = true;
        Scan(ent, target, args.User);
    }

    private void OnUseInHand(Entity<DebugAnalyzerComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !HasComp<MobStateComponent>(args.User))
            return;

        args.Handled = true;
        Scan(ent, args.User, args.User);
    }

    private void OnUiClosed(Entity<DebugAnalyzerComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.ScannedEntity = null;
    }

    private void Scan(Entity<DebugAnalyzerComponent> ent, EntityUid target, EntityUid user)
    {
        ent.Comp.ScannedEntity = target;
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval;

        _ui.OpenUi(ent.Owner, DebugAnalyzerUiKey.Key, user);
        SendReport(ent.Owner, target);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DebugAnalyzerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.ScannedEntity is not { } target || _timing.CurTime < comp.NextUpdate)
                continue;

            comp.NextUpdate = _timing.CurTime + comp.UpdateInterval;

            if (Deleted(target)
                || comp.MaxScanRange is { } range
                && !_transform.InRange(Transform(target).Coordinates, xform.Coordinates, range))
            {
                comp.ScannedEntity = null;
                _ui.CloseUi(uid, DebugAnalyzerUiKey.Key);
                continue;
            }

            SendReport(uid, target);
        }
    }

    private void SendReport(EntityUid analyzer, EntityUid target)
    {
        var sections = new List<DebugAnalyzerSection>
        {
            BuildGeneral(target),
        };

        TryAdd(sections, BuildHealth(target));
        TryAdd(sections, BuildHunger(target));
        TryAdd(sections, BuildThirst(target));
        TryAdd(sections, BuildNeeds(target));
        TryAdd(sections, BuildDefecation(target));
        TryAdd(sections, BuildManure(target));
        TryAdd(sections, BuildGrowth(target));
        TryAdd(sections, BuildRideable(target));

        var message = new DebugAnalyzerScannedMessage(GetNetEntity(target), Name(target), sections);
        _ui.ServerSendUiMessage(analyzer, DebugAnalyzerUiKey.Key, message);
    }

    private static void TryAdd(List<DebugAnalyzerSection> sections, DebugAnalyzerSection? section)
    {
        if (section != null)
            sections.Add(section);
    }

    private DebugAnalyzerSection BuildGeneral(EntityUid target)
    {
        var rows = new List<DebugAnalyzerRow>
        {
            new(L("prototype"), MetaData(target).EntityPrototype?.ID ?? "-"),
            new(L("entity"), GetNetEntity(target).ToString()),
        };

        if (HasComp<MobStateComponent>(target))
        {
            var state = _mobState.IsDead(target) ? MobState.Dead
                : _mobState.IsCritical(target) ? MobState.Critical
                : MobState.Alive;
            var severity = state switch
            {
                MobState.Critical => DebugAnalyzerSeverity.Warning,
                MobState.Dead => DebugAnalyzerSeverity.Critical,
                _ => DebugAnalyzerSeverity.Normal,
            };
            rows.Add(new DebugAnalyzerRow(L("mob-state"), state.ToString(), severity: severity));
        }

        if (TryComp<AnimalSexComponent>(target, out var sex))
            rows.Add(new DebugAnalyzerRow(L("sex"), sex.Sex.ToString()));

        return new DebugAnalyzerSection(L("section-general"), rows);
    }

    private DebugAnalyzerSection? BuildHealth(EntityUid target)
    {
        if (!TryComp<DamageableComponent>(target, out var damageable))
            return null;

        var rows = new List<DebugAnalyzerRow>();
        var total = damageable.TotalDamage;

        if (_thresholds.TryGetDeadThreshold(target, out var dead))
        {
            var fraction = dead.Value > FixedPoint2.Zero ? (float) (total / dead.Value) : 0f;
            var severity = fraction switch
            {
                >= 0.75f => DebugAnalyzerSeverity.Critical,
                >= 0.4f => DebugAnalyzerSeverity.Warning,
                _ => DebugAnalyzerSeverity.Normal,
            };
            rows.Add(new DebugAnalyzerRow(L("damage-total"), $"{Fmt((float) total)} / {Fmt((float) dead.Value)}",
                Math.Clamp(fraction, 0f, 1f), severity));
        }
        else
        {
            rows.Add(new DebugAnalyzerRow(L("damage-total"), Fmt((float) total)));
        }

        if (_thresholds.TryGetThresholdForState(target, MobState.Critical, out var crit))
            rows.Add(new DebugAnalyzerRow(L("threshold-critical"), Fmt((float) crit.Value)));

        if (dead != null)
            rows.Add(new DebugAnalyzerRow(L("threshold-dead"), Fmt((float) dead.Value)));

        foreach (var (type, amount) in damageable.Damage.DamageDict)
        {
            if (amount <= FixedPoint2.Zero)
                continue;

            var name = _prototypes.TryIndex<DamageTypePrototype>(type, out var proto) ? proto.LocalizedName : type;
            rows.Add(new DebugAnalyzerRow(name, Fmt((float) amount)));
        }

        if (TryComp<TemperatureComponent>(target, out var temperature))
        {
            var kelvin = temperature.CurrentTemperature;
            rows.Add(new DebugAnalyzerRow(L("temperature"), $"{Fmt(kelvin)} K ({Fmt(kelvin - Atmospherics.T0C)} °C)"));
        }

        if (TryComp<BloodstreamComponent>(target, out var bloodstream))
        {
            if (_solutions.ResolveSolution(target, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution,
                    out var blood))
            {
                var fraction = (float) blood.FillFraction;
                var severity = fraction switch
                {
                    < 0.6f => DebugAnalyzerSeverity.Critical,
                    < 0.9f => DebugAnalyzerSeverity.Warning,
                    _ => DebugAnalyzerSeverity.Normal,
                };
                rows.Add(new DebugAnalyzerRow(L("blood"), $"{Fmt(fraction * 100f)}%", Math.Clamp(fraction, 0f, 1f),
                    severity));
            }

            var bleeding = bloodstream.BleedAmount > 0f;
            rows.Add(new DebugAnalyzerRow(L("bleeding"), bleeding ? Fmt(bloodstream.BleedAmount) : "-",
                severity: bleeding ? DebugAnalyzerSeverity.Warning : DebugAnalyzerSeverity.Normal));
        }

        return new DebugAnalyzerSection(L("section-health"), rows);
    }

    private DebugAnalyzerSection? BuildHunger(EntityUid target)
    {
        if (!TryComp<HungerComponent>(target, out var hunger))
            return null;

        var value = _hunger.GetHunger(hunger);
        var max = hunger.Thresholds[HungerThreshold.Overfed];
        var threshold = _hunger.GetHungerThreshold(hunger);
        var severity = threshold switch
        {
            HungerThreshold.Peckish => DebugAnalyzerSeverity.Warning,
            HungerThreshold.Starving or HungerThreshold.Dead => DebugAnalyzerSeverity.Critical,
            _ => DebugAnalyzerSeverity.Normal,
        };

        var rows = new List<DebugAnalyzerRow>
        {
            new(L("hunger-value"), $"{Fmt(value)} / {Fmt(max)}", Fraction(value, max), severity),
            new(L("threshold"), threshold.ToString(), severity: severity),
            new(L("decay-per-minute"), Fmt(hunger.ActualDecayRate * 60f)),
        };

        return new DebugAnalyzerSection(L("section-hunger"), rows);
    }

    private DebugAnalyzerSection? BuildThirst(EntityUid target)
    {
        if (!TryComp<ThirstComponent>(target, out var thirst))
            return null;

        var value = thirst.CurrentThirst;
        var max = thirst.ThirstThresholds[ThirstThreshold.OverHydrated];
        var threshold = thirst.CurrentThirstThreshold;
        var severity = threshold switch
        {
            ThirstThreshold.Thirsty => DebugAnalyzerSeverity.Warning,
            ThirstThreshold.Parched or ThirstThreshold.Dead => DebugAnalyzerSeverity.Critical,
            _ => DebugAnalyzerSeverity.Normal,
        };

        var rows = new List<DebugAnalyzerRow>
        {
            new(L("thirst-value"), $"{Fmt(value)} / {Fmt(max)}", Fraction(value, max), severity),
            new(L("threshold"), threshold.ToString(), severity: severity),
            new(L("decay-per-minute"), Fmt(thirst.ActualDecayRate * 60f)),
        };

        return new DebugAnalyzerSection(L("section-thirst"), rows);
    }

    private DebugAnalyzerSection? BuildNeeds(EntityUid target)
    {
        if (!TryComp<AnimalNeedsComponent>(target, out var needs))
            return null;

        var rows = new List<DebugAnalyzerRow>();
        AddNeed(rows, "satiety", needs.Satiety);
        AddNeed(rows, "hydration", needs.Hydration);

        return new DebugAnalyzerSection(L("section-needs"), rows);
    }

    private static void AddNeed(List<DebugAnalyzerRow> rows, string name, NeedState need)
    {
        var level = need.GetLevel();
        var severity = level switch
        {
            NeedLevel.Low => DebugAnalyzerSeverity.Warning,
            NeedLevel.Empty => DebugAnalyzerSeverity.Critical,
            _ => DebugAnalyzerSeverity.Normal,
        };

        rows.Add(new DebugAnalyzerRow(L($"{name}-value"), $"{Fmt(need.Value)} / {Fmt(need.Max)}",
            Fraction(need.Value, need.Max), severity));
        rows.Add(new DebugAnalyzerRow(L($"{name}-level"), level.ToString(), severity: severity));
        rows.Add(new DebugAnalyzerRow(L($"{name}-decay"), Fmt(need.DecayPerMinute)));
        rows.Add(new DebugAnalyzerRow(L($"{name}-seek"), Fmt(need.SeekBelow)));
    }

    private DebugAnalyzerSection? BuildDefecation(EntityUid target)
    {
        if (!TryComp<DefecationComponent>(target, out var defecation))
            return null;

        var value = defecation.Value;
        var threshold = defecation.CurrentThreshold;
        var severity = threshold switch
        {
            DefecationThreshold.Urge => DebugAnalyzerSeverity.Warning,
            DefecationThreshold.Critical or DefecationThreshold.Accident => DebugAnalyzerSeverity.Critical,
            _ => DebugAnalyzerSeverity.Normal,
        };

        var rows = new List<DebugAnalyzerRow>();
        if (defecation.Thresholds.TryGetValue(DefecationThreshold.Accident, out var max))
            rows.Add(new DebugAnalyzerRow(L("defecation-value"), $"{Fmt(value)} / {Fmt(max)}", Fraction(value, max), severity));
        else
            rows.Add(new DebugAnalyzerRow(L("defecation-value"), Fmt(value), severity: severity));

        rows.Add(new DebugAnalyzerRow(L("threshold"), threshold.ToString(), severity: severity));

        if (defecation.Thresholds.TryGetValue(DefecationThreshold.Urge, out var urge))
            rows.Add(new DebugAnalyzerRow(L("defecation-urge"), Fmt(urge)));

        rows.Add(new DebugAnalyzerRow(L("defecation-fill"), Fmt(defecation.BaseFillRate * 60f)));

        return new DebugAnalyzerSection(L("section-defecation"), rows);
    }

    private DebugAnalyzerSection? BuildManure(EntityUid target)
    {
        if (!TryComp<ManureProducerComponent>(target, out var manure))
            return null;

        var rows = new List<DebugAnalyzerRow>
        {
            new(L("manure-yield"), Fmt(manure.UnitsPerNutrition)),
            new(L("manure-progress"), $"{Fmt(manure.Accumulated)} / 1", Fraction(manure.Accumulated, 1f)),
            new(L("manure-product"), manure.Product.Id),
        };

        return new DebugAnalyzerSection(L("section-manure"), rows);
    }

    private DebugAnalyzerSection? BuildGrowth(EntityUid target)
    {
        if (!TryComp<GrowthComponent>(target, out var growth)
            || growth.CurrentStage < 0
            || growth.CurrentStage >= growth.Stages.Count)
        {
            return null;
        }

        var stage = growth.Stages[growth.CurrentStage];
        var rows = new List<DebugAnalyzerRow>
        {
            new(L("growth-stage"), $"{stage.Id} ({growth.CurrentStage + 1}/{growth.Stages.Count})"),
        };

        if (growth.StageEndTime is { } end)
        {
            var left = end - _timing.CurTime;
            rows.Add(new DebugAnalyzerRow(L("growth-next"), FormatTime(left)));
        }
        else
        {
            rows.Add(new DebugAnalyzerRow(L("growth-next"), "-"));
        }

        rows.Add(new DebugAnalyzerRow(L("growth-scale"), $"{Fmt(stage.Scale.X)} x {Fmt(stage.Scale.Y)}"));
        rows.Add(new DebugAnalyzerRow(L("growth-meat"), stage.MeatCount.ToString(CultureInfo.InvariantCulture)));
        rows.Add(new DebugAnalyzerRow(L("growth-price"), Fmt((float) stage.Price)));

        return new DebugAnalyzerSection(L("section-growth"), rows);
    }

    private DebugAnalyzerSection? BuildRideable(EntityUid target)
    {
        if (!TryComp<RideableComponent>(target, out var rideable))
            return null;

        var rider = rideable.Rider is { } uid ? Name(uid) : "-";
        var rows = new List<DebugAnalyzerRow>
        {
            new(L("rideable-rider"), rider),
        };

        return new DebugAnalyzerSection(L("section-rideable"), rows);
    }

    private static string L(string id) => $"{ReportPrefix}-{id}";

    private static string Fmt(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    private static float Fraction(float value, float max) => max > 0f ? Math.Clamp(value / max, 0f, 1f) : 0f;

    private static string FormatTime(TimeSpan time)
    {
        if (time < TimeSpan.Zero)
            time = TimeSpan.Zero;

        return $"{(int) time.TotalMinutes}:{time.Seconds:00}";
    }
}
