using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._HorizonNova.Lactation;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server._HorizonNova.Lactation;

/// <summary>
/// HN: ПКМ-верб админа «Изменить грудь», открывает <see cref="LactationAdminEui"/>.
/// </summary>
public sealed class LactationAdminEuiSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly EuiManager _eui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HumanoidAppearanceComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<HumanoidAppearanceComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        if (!_adminManager.HasAdminFlag(actor.PlayerSession, AdminFlags.Fun))
            return;

        var user = args.User;
        var target = ent.Owner;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("lactation-admin-verb"),
            Category = VerbCategory.Tricks,
            Priority = 10,
            Impact = LogImpact.Low,
            Act = () => _eui.OpenEui(new LactationAdminEui(target), actor.PlayerSession),
        });
    }
}
