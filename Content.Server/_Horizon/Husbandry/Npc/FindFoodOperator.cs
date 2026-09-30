using Content.Server.NPC;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Horizon.Husbandry.Feeding;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared._Horizon.Husbandry.Feeding;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Map;

namespace Content.Server._Horizon.Husbandry.Npc;

/// <summary>
/// Looks around for the closest food the animal is willing to eat and can walk to.
/// Sets the target and its coordinates like <c>UtilityOperator</c> does, so the usual move operator follows.
/// </summary>
public sealed partial class FindFoodOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private AnimalFeedingSystem _feeding = default!;
    private EntityLookupSystem _lookup = default!;
    private PathfindingSystem _pathfinding = default!;
    private SharedTransformSystem _transform = default!;

    [DataField]
    public string Key = "Target";

    /// <summary>
    /// The EntityCoordinates of the target.
    /// </summary>
    [DataField]
    public string KeyCoordinates = "TargetCoordinates";

    [DataField]
    public float Range = 10f;

    /// <summary>
    /// How many of the closest foods are checked for a path before giving up.
    /// </summary>
    [DataField]
    public int MaxCandidates = 5;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _feeding = sysManager.GetEntitySystem<AnimalFeedingSystem>();
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<DietComponent>(owner, out var diet))
            return (false, null);

        var ownerCoordinates = _entManager.GetComponent<TransformComponent>(owner).Coordinates;
        var foods = new HashSet<Entity<FoodComponent>>();
        _lookup.GetEntitiesInRange(ownerCoordinates, Range, foods);

        var candidates = new List<(EntityUid Food, float Distance)>();
        foreach (var food in foods)
        {
            if (!_feeding.CanEat((owner, diet), food))
                continue;

            var distance = (_transform.GetWorldPosition(food) - _transform.GetWorldPosition(owner)).LengthSquared();
            candidates.Add((food, distance));
        }

        candidates.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        for (var i = 0; i < candidates.Count && i < MaxCandidates; i++)
        {
            var food = candidates[i].Food;
            var path = await _pathfinding.GetPath(owner, food, 1f, cancelToken);
            if (path.Result == PathResult.NoPath)
                continue;

            return (true, new Dictionary<string, object>
            {
                { Key, food },
                { KeyCoordinates, new EntityCoordinates(food, Vector2.Zero) },
            });
        }

        return (false, null);
    }
}
