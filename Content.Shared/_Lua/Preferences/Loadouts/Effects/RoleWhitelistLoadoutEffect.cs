using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Validates a loadout against a set of allowed roles (whitelist).
/// Used to keep job/antag-specific loadout items available only to their roles.
/// </summary>
public sealed partial class RoleWhitelistLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public List<ProtoId<RoleLoadoutPrototype>> Whitelist = default!;

    public override bool Validate(HumanoidCharacterProfile profile, RoleLoadout loadout, ICommonSession? session, IDependencyCollection collection, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        if (Whitelist.Contains(loadout.Role))
        {
            reason = null;
            return true;
        }

        reason = new FormattedMessage();
        reason.TryAddMarkup(Loc.GetString("role-whitelist-loadout-invalid"), out var _);
        return false;
    }
}
