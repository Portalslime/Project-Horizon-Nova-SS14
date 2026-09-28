// BOOSTY (Lua) integration.
// Per-owner listing check (personal sponsor loadouts) is TEMPORARILY DISABLED on this branch.
//
// What this file is:
//   * The active condition below no longer checks the buyer login and always returns true.
//   * The original Lua/Boosty owner check is preserved verbatim inside "#if false" at the
//     bottom of the file so it can be restored later by ourselves.
//
// How to re-enable: remove the "#if false" / "#endif" markers around the preserved
// class and delete the stub class above.
using Content.Shared.Store;

namespace Content.Server.Store.Conditions;

public sealed partial class BuyerSponsorOwnerCondition : ListingCondition
{
    [DataField("ownerLogin", required: true)]
    public string OwnerLogin = string.Empty;

    public override bool Condition(ListingConditionArgs args)
    {
        // BOOSTY: owner check disabled - listing is available to everyone.
        return true;
    }
}

#if false
// ============================================================================
// BOOSTY: original per-owner check below. Disabled on this branch.
// ============================================================================
using Content.Shared.Mind;
using Robust.Server.Player;
using Robust.Shared.IoC;

public sealed partial class BuyerSponsorOwnerCondition : ListingCondition
{
    [DataField("ownerLogin", required: true)]
    public string OwnerLogin = string.Empty;

    public override bool Condition(ListingConditionArgs args)
    {
        if (!args.EntityManager.TryGetComponent<MindComponent>(args.Buyer, out var mind)) return false;
        if (mind.UserId is not { } userId) return false;
        var playerManager = IoCManager.Resolve<IPlayerManager>();
        if (!playerManager.TryGetSessionById(userId, out var session)) return false;
        return string.Equals(session.Name, OwnerLogin, StringComparison.OrdinalIgnoreCase);
    }
}
#endif
