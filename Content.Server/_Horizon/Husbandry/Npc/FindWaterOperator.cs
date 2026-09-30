using Content.Server.NPC;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Horizon.Husbandry.Feeding;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared._Horizon.Husbandry.Feeding;
using Robust.Shared.Map;

namespace Content.Server._Horizon.Husbandry.Npc;

/// <summary>
/// Looks around for the closest water source with something in it that the animal can walk to.
/// Sets the target and its coordinates like <c>UtilityOperator</c> does, so the usual move operator follows.
/// </summary>
public sealed partial class FindWaterOperator : HTNOperator
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
    /// How many of the closest sources are checked for a path before giving up.
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
        var ownerCoordinates = _entManager.GetComponent<TransformComponent>(owner).Coordinates;

        var sources = new HashSet<Entity<WaterSourceComponent>>();
        _lookup.GetEntitiesInRange(ownerCoordinates, Range, sources);

        var candidates = new List<(EntityUid Source, float Distance)>();
        foreach (var source in sources)
        {
            if (!_feeding.CanDrink(source))
                continue;

            var distance = (_transform.GetWorldPosition(source) - _transform.GetWorldPosition(owner)).LengthSquared();
            candidates.Add((source, distance));
        }

        candidates.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        for (var i = 0; i < candidates.Count && i < MaxCandidates; i++)
        {
            var source = candidates[i].Source;
            var path = await _pathfinding.GetPath(owner, source, 1f, cancelToken);
            if (path.Result == PathResult.NoPath)
                continue;

            return (true, new Dictionary<string, object>
            {
                { Key, source },
                { KeyCoordinates, new EntityCoordinates(source, Vector2.Zero) },
            });
        }

        return (false, null);
    }
}
