// BOOSTY (Lua) integration.
// Subscription/tier check for store listings is TEMPORARILY DISABLED on this branch.
//
// What this file is:
//   * The active condition below no longer checks the sponsor tier and always returns
//     true, so VIP listings are effectively unrestricted.
//   * The original Lua/Boosty tier check is preserved verbatim inside "#if false" at
//     the bottom of the file so it can be restored later by ourselves.
//
// How to re-enable: remove the "#if false" / "#endif" markers around the preserved
// class and delete the stub class above.
using Content.Shared.Store;

namespace Content.Server.Store.Conditions;

public sealed partial class BuyerSponsorTierCondition : ListingCondition
{
    [DataField("whitelist")]
    public HashSet<string>? Whitelist;
    [DataField("blacklist")]
    public HashSet<string>? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        // BOOSTY: sponsor-tier check disabled - listing is available to everyone.
        return true;
    }
}

#if false
// ============================================================================
// BOOSTY: original sponsor-tier check below. Disabled on this branch.
// ============================================================================
using Content.Server.Sponsors;
using Content.Shared._Lua.SponsorLoadout;
using Content.Shared.Mind;
using Robust.Shared.IoC;
using System.Linq;

public sealed partial class BuyerSponsorTierCondition : ListingCondition
{
    [DataField("whitelist")]
    public HashSet<string>? Whitelist;
    [DataField("blacklist")]
    public HashSet<string>? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        if (!args.EntityManager.TryGetComponent<MindComponent>(args.Buyer, out var mind)) return false;
        if (mind.UserId is not { } userId) return false;
        var sponsorManager = IoCManager.Resolve<SponsorManager>();
        IEnumerable<string> roles;
        if (sponsorManager.TryGetAllActiveSponsors(userId, out var allSponsors)) roles = allSponsors.Select(s => s.Role).Where(DonorGroups.IsKnownTier);
        else if (sponsorManager.TryGetActiveSponsor(userId, out var sponsor) && DonorGroups.IsKnownTier(sponsor.Role)) roles = [sponsor.Role];
        else return false;
        var roleSet = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (roleSet.Count == 0) return false;
        if (Blacklist != null && roleSet.Any(r => Blacklist.Contains(r))) return false;
        if (Whitelist != null && !roleSet.Any(r => Whitelist.Contains(r))) return false;
        return true;
    }
}
#endif
