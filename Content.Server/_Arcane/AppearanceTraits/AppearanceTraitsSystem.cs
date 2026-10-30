using System.Linq;
using Content.Shared._Arcane.AppearanceTraits;
using Content.Shared._Shitmed.Humanoid.Events;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;

namespace Content.Server._Arcane.AppearanceTraits;

/// <summary>
/// Отслеживает marking'и гуманоида и превращает их в «черты внешности»
/// (<see cref="AppearanceTraitPrototype"/>): считает активный набор, суммирует эффекты
/// и выдаёт/снимает компоненты. Единственная точка истины для других систем —
/// <see cref="HasTrait"/> и <see cref="GetActiveTraits"/>.
/// </summary>
public sealed class AppearanceTraitsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    private List<AppearanceTraitPrototype> _traitCache = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidAppearanceComponent, MapInitEvent>(OnHumanoidMapInit);
        SubscribeLocalEvent<HumanoidAppearanceComponent, HumanoidMarkingSetChangedEvent>(OnMarkingSetChanged);
        SubscribeLocalEvent<HumanoidAppearanceComponent, ProfileLoadFinishedEvent>(OnProfileLoadFinished);
        SubscribeLocalEvent<AppearanceTraitsComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<AppearanceTraitDamageModifierComponent, DamageModifyEvent>(OnPartDamageModify);
        SubscribeLocalEvent<AppearanceTraitsComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        _traitCache = _proto.EnumeratePrototypes<AppearanceTraitPrototype>().ToList();
    }

    private void OnHumanoidMapInit(Entity<HumanoidAppearanceComponent> ent, ref MapInitEvent args)
    {
        EnsureComp<AppearanceTraitsComponent>(ent.Owner);

        // Marking'и на этом этапе обычно ещё пустые: окончательный пересчёт
        // произойдёт по ProfileLoadFinishedEvent или HumanoidMarkingSetChangedEvent.
        RefreshTraits(ent.Owner, ent.Comp);
    }

    private void OnMarkingSetChanged(Entity<HumanoidAppearanceComponent> ent, ref HumanoidMarkingSetChangedEvent args)
    {
        RefreshTraits(ent.Owner, ent.Comp);
    }

    private void OnProfileLoadFinished(Entity<HumanoidAppearanceComponent> ent, ref ProfileLoadFinishedEvent args)
    {
        RefreshTraits(ent.Owner, ent.Comp);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<AppearanceTraitPrototype>())
            return;

        _traitCache = _proto.EnumeratePrototypes<AppearanceTraitPrototype>().ToList();

        var query = AllEntityQuery<HumanoidAppearanceComponent>();
        while (query.MoveNext(out var uid, out var humanoid))
        {
            // При перезагрузке прототипов эффекты могли поменяться даже без изменения
            // набора marking'ов, поэтому насильно пересчитываем всё.
            RefreshTraits(uid, humanoid, force: true);
        }
    }

    /// <summary>
    /// Активна ли указанная черта на сущности.
    /// </summary>
    public bool HasTrait(EntityUid uid, ProtoId<AppearanceTraitPrototype> trait, AppearanceTraitsComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        return component.ActiveTraits.Contains(trait);
    }

    /// <summary>
    /// Набор активных черт. Если компонента нет — пустой набор.
    /// </summary>
    public IReadOnlyCollection<ProtoId<AppearanceTraitPrototype>> GetActiveTraits(EntityUid uid, AppearanceTraitsComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return Array.Empty<ProtoId<AppearanceTraitPrototype>>();

        return component.ActiveTraits;
    }

    /// <summary>
    /// Пересчитывает активные черты по текущему набору marking'ов и применяет их эффекты.
    /// </summary>
    /// <param name="force">Пересчитать эффекты, даже если набор активных черт не изменился.</param>
    public void RefreshTraits(EntityUid uid, HumanoidAppearanceComponent? humanoid = null, bool force = false)
    {
        if (!Resolve(uid, ref humanoid, false) || TerminatingOrDeleted(uid))
            return;

        var component = EnsureComp<AppearanceTraitsComponent>(uid);

        var markingIds = new HashSet<string>();
        foreach (var marking in humanoid.MarkingSet.GetForwardEnumerator())
        {
            markingIds.Add(marking.MarkingId);
        }

        var newTraits = new HashSet<ProtoId<AppearanceTraitPrototype>>();
        var activePrototypes = new List<AppearanceTraitPrototype>();
        foreach (var prototype in _traitCache)
        {
            if (!Matches(prototype, humanoid, markingIds))
                continue;

            newTraits.Add(prototype.ID);
            activePrototypes.Add(prototype);
        }

        var added = new HashSet<ProtoId<AppearanceTraitPrototype>>(newTraits);
        added.ExceptWith(component.ActiveTraits);

        var removed = new HashSet<ProtoId<AppearanceTraitPrototype>>(component.ActiveTraits);
        removed.ExceptWith(newTraits);

        if (added.Count == 0 && removed.Count == 0 && !force)
            return;

        component.ActiveTraits.Clear();
        component.ActiveTraits.UnionWith(newTraits);

        // Эффекты считаются от старшей черты к младшей: при конфликте компонентов
        // одного типа побеждает черта с большим Priority.
        activePrototypes.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        ApplyEffects(uid, component, activePrototypes);

        _movement.RefreshMovementSpeedModifiers(uid);

        if (added.Count > 0 || removed.Count > 0)
            RaiseLocalEvent(uid, new AppearanceTraitsChangedEvent(added, removed));
    }

    private static bool Matches(
        AppearanceTraitPrototype prototype,
        HumanoidAppearanceComponent humanoid,
        HashSet<string> markingIds)
    {
        if (prototype.Species.Count > 0 && !prototype.Species.Contains(humanoid.Species))
            return false;

        // Черта без условий бесполезна и не должна активироваться.
        if (prototype.Markings.Count == 0 && prototype.Categories.Count == 0)
            return false;

        var markingsMatched = false;
        if (prototype.Markings.Count > 0)
        {
            if (prototype.RequireAllMarkings)
            {
                markingsMatched = true;
                foreach (var marking in prototype.Markings)
                {
                    if (markingIds.Contains(marking.Id))
                        continue;

                    markingsMatched = false;
                    break;
                }
            }
            else
            {
                foreach (var marking in prototype.Markings)
                {
                    if (!markingIds.Contains(marking.Id))
                        continue;

                    markingsMatched = true;
                    break;
                }
            }
        }

        var categoriesMatched = false;
        foreach (var category in prototype.Categories)
        {
            if (!humanoid.MarkingSet.Markings.TryGetValue(category, out var markings) || markings.Count == 0)
                continue;

            categoriesMatched = true;
            break;
        }

        return markingsMatched || categoriesMatched;
    }

    private void ApplyEffects(
        EntityUid uid,
        AppearanceTraitsComponent component,
        List<AppearanceTraitPrototype> activePrototypes)
    {
        var melee = new DamageSpecifier();
        var walk = 1f;
        var sprint = 1f;
        DamageModifierSet? combined = null;
        var registry = new ComponentRegistry();

        foreach (var prototype in activePrototypes)
        {
            if (prototype.MeleeDamage is { } meleeDamage)
                melee += meleeDamage;

            walk *= prototype.WalkSpeedModifier;
            sprint *= prototype.SprintSpeedModifier;

            if (prototype.DamageModifier is { } modifier)
            {
                if (combined == null)
                {
                    // Копируем, чтобы не мутировать прототипные данные.
                    combined = new DamageModifierSet
                    {
                        Coefficients = new Dictionary<string, float>(modifier.Coefficients),
                        FlatReduction = new Dictionary<string, float>(modifier.FlatReduction),
                    };
                }
                else
                {
                    foreach (var (damageType, coefficient) in modifier.Coefficients)
                    {
                        combined.Coefficients[damageType] = combined.Coefficients.TryGetValue(damageType, out var existing)
                            ? existing * coefficient
                            : coefficient;
                    }

                    foreach (var (damageType, flatReduction) in modifier.FlatReduction)
                    {
                        combined.FlatReduction[damageType] = combined.FlatReduction.TryGetValue(damageType, out var existing)
                            ? existing + flatReduction
                            : flatReduction;
                    }
                }
            }

            foreach (var (name, entry) in prototype.Components)
            {
                registry.TryAdd(name, entry);
            }
        }

        component.MeleeDamage = melee;
        component.DamageModifier = combined;
        component.WalkSpeedModifier = walk;
        component.SprintSpeedModifier = sprint;

        ApplyDamageModifierToParts(uid, combined);
        ApplyComponents(uid, component, registry);
    }

    private void ApplyDamageModifierToParts(EntityUid uid, DamageModifierSet? modifier)
    {
        // Сначала собираем части тела, потом меняем компоненты — чтобы не мутировать
        // коллекцию во время перебора.
        var parts = new List<EntityUid>();
        var query = AllEntityQuery<BodyPartComponent>();
        while (query.MoveNext(out var partUid, out var part))
        {
            if (part.Body == uid)
                parts.Add(partUid);
        }

        foreach (var partUid in parts)
        {
            if (modifier == null)
            {
                RemComp<AppearanceTraitDamageModifierComponent>(partUid);
                continue;
            }

            var partModifier = EnsureComp<AppearanceTraitDamageModifierComponent>(partUid);
            partModifier.Modifier = modifier;
        }
    }

    private void ApplyComponents(EntityUid uid, AppearanceTraitsComponent component, ComponentRegistry registry)
    {
        var desiredTypes = new HashSet<Type>();
        foreach (var entry in registry.Values)
        {
            desiredTypes.Add(entry.Component.GetType());
        }

        // Снимаем только те компоненты, которые выдала эта система.
        if (component.AddedComponents.Count > 0)
        {
            var stale = new List<Type>();
            foreach (var type in component.AddedComponents)
            {
                if (!desiredTypes.Contains(type))
                    stale.Add(type);
            }

            foreach (var type in stale)
            {
                if (EntityManager.HasComponent(uid, type))
                    EntityManager.RemoveComponent(uid, type);

                component.AddedComponents.Remove(type);
            }
        }

        foreach (var (name, entry) in registry)
        {
            var type = entry.Component.GetType();
            if (EntityManager.HasComponent(uid, type))
                continue;

            var single = new ComponentRegistry { { name, entry } };
            EntityManager.AddComponents(uid, single, false);
            component.AddedComponents.Add(type);
        }
    }

    private void OnGetMeleeDamage(EntityUid uid, AppearanceTraitsComponent component, ref GetMeleeDamageEvent args)
    {
        // Событие приходит на оружие; без оружия им является сам моб.
        if (args.Weapon != uid || component.MeleeDamage.Empty)
            return;

        args.Damage += component.MeleeDamage;
    }

    private void OnPartDamageModify(EntityUid uid, AppearanceTraitDamageModifierComponent component, DamageModifyEvent args)
    {
        if (component.Modifier is not { } modifier || args.Damage.Empty)
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifier);
    }

    private void OnRefreshSpeed(EntityUid uid, AppearanceTraitsComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }
}
