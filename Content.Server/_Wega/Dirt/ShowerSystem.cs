using Content.Server.Fluids.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.DirtVisuals;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Shower
{
    public sealed partial class ShowerSystem : EntitySystem
    {
        /// <summary>
        /// Name of the solution the shower sprays from. Must match the prototype.
        /// </summary>
        private const string ShowerSolutionName = "shower";

        [Dependency] private SharedAmbientSoundSystem _ambient = default!;
        [Dependency] private SharedAppearanceSystem _appearance = default!;
        [Dependency] private SharedAudioSystem _audio = default!;
        [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
        [Dependency] private PuddleSystem _puddle = default!;
        [Dependency] private EntityLookupSystem _lookup = default!;
        [Dependency] private ReactiveSystem _reactive = default!;
        [Dependency] private SharedPopupSystem _popup = default!;
        [Dependency] private SharedDirtSystem _dirt = default!; //HN: загрязнение одежды (порт Wega)

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShowerComponent, GetVerbsEvent<AlternativeVerb>>(AddShowerVerb);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ShowerComponent>();
            while (query.MoveNext(out var uid, out var shower))
            {
                if (!shower.IsSpraying)
                    continue;

                shower.RemainingTime -= frameTime;
                if (shower.RemainingTime <= 0)
                {
                    SprayWater(uid, shower);
                    shower.RemainingTime = shower.SprayTime;
                }
            }
        }

        private void AddShowerVerb(EntityUid uid, ShowerComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || !this.IsPowered(uid, EntityManager))
                return;

            AlternativeVerb verb = new()
            {
                Act = () => ToggleSpraying(uid, component),
                Text = component.IsSpraying
                    ? Loc.GetString("shower-verb-stop")
                    : Loc.GetString("shower-verb-start"),
                Priority = 2
            };

            args.Verbs.Add(verb);
        }

        public void ToggleSpraying(EntityUid uid, ShowerComponent component)
        {
            if (component.IsSpraying)
                StopSpraying(uid, component);
            else
                StartSpraying(uid, component);
        }

        public void StartSpraying(EntityUid uid, ShowerComponent component)
        {
            if (component.IsSpraying)
                return;

            // Horizon: the shower is fed from the plumbing supply line, so it can be empty. No water, no spray.
            // HN: debug-душ (requiresWater: false) работает без воды.
            if (component.RequiresWater)
            {
                if (!_solutionContainer.TryGetSolution(uid, ShowerSolutionName, out _, out var solution) ||
                    solution.Volume <= 0)
                {
                    _popup.PopupEntity(Loc.GetString("shower-no-water"), uid);
                    return;
                }
            }

            component.IsSpraying = true;
            component.RemainingTime = component.SprayTime;

            _audio.PlayPvs(component.SprayStartSound, uid);
            _ambient.SetAmbience(uid, true);

            _appearance.SetData(uid, ShowerVisuals.Spraying, true);
        }

        private void StopSpraying(EntityUid uid, ShowerComponent component)
        {
            if (!component.IsSpraying)
                return;

            component.IsSpraying = false;

            _audio.PlayPvs(component.SprayEndSound, uid);
            _ambient.SetAmbience(uid, false);

            _appearance.SetData(uid, ShowerVisuals.Spraying, false);
        }

        private void SprayWater(EntityUid uid, ShowerComponent component)
        {
            var coordinates = Transform(uid).Coordinates;
            var mobsInRange = _lookup.GetEntitiesInRange<ReactiveComponent>(coordinates, 0.5f);
            var amountPerTarget = component.WaterAmount / (mobsInRange.Count + 1);

            // HN: debug-душ — бесконечная вода, лужу не разливает.
            if (!component.RequiresWater)
            {
                foreach (var mob in mobsInRange)
                {
                    var debugSolution = new Solution(component.WaterReagent, FixedPoint2.New(amountPerTarget));
                    ApplyShowerToMob(mob, debugSolution);
                }

                return;
            }

            if (!_solutionContainer.TryGetSolution(uid, ShowerSolutionName, out var showerSol, out var solution))
            {
                StopSpraying(uid, component);
                return;
            }

            if (solution.Volume == 0)
            {
                StopSpraying(uid, component);
                return;
            }

            foreach (var mob in mobsInRange)
            {
                var splitSolution = _solutionContainer.SplitSolution(showerSol.Value, FixedPoint2.New(amountPerTarget));
                ApplyShowerToMob(mob, splitSolution);
            }

            var floorSolution = _solutionContainer.SplitSolution(showerSol.Value, FixedPoint2.New(amountPerTarget));
            _puddle.TrySpillAt(coordinates, floorSolution, out _);
        }

        private void ApplyShowerToMob(EntityUid mob, Solution splitSolution)
        {
            //HN Start: загрязнение одежды — вода смывает грязь, прочие реагенты пачкают.
            // Душ обрабатывает только верхние слои, бельё и носки остаются.
            _dirt.ApplyDirtToUpperClothing(mob, splitSolution);
            //HN: под душем вся одежда (кроме сумки) постепенно намокает.
            _dirt.WetClothing(mob, splitSolution.Volume.Float());
            //HN End
            _reactive.DoEntityReaction(mob, splitSolution, ReactionMethod.Touch);
        }
    }
}
