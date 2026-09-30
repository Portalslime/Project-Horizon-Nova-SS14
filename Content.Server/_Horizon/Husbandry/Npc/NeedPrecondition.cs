using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared._Horizon.Husbandry.Needs;

namespace Content.Server._Horizon.Husbandry.Npc;

public enum NeedKind : byte
{
    Satiety,
    Hydration,
}

/// <summary>
/// Met when the animal is low enough on the need to go looking for something to fill it.
/// </summary>
public sealed partial class NeedPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField(required: true)]
    public NeedKind Need;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager) ||
            !_entManager.TryGetComponent<AnimalNeedsComponent>(owner, out var needs))
            return false;

        var need = Need == NeedKind.Satiety ? needs.Satiety : needs.Hydration;
        return need.Value <= need.SeekBelow;
    }
}
