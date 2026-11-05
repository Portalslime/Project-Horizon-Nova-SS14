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

        // Подписка на уровне сущности: на (HumanoidAppearanceComponent, GetVerbsEvent<Verb>)
        // уже подписан HumanoidAppearanceSystem, повторная подписка на ту же пару запрещена шиной.
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
            return;

        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        if (!_adminManager.HasAdminFlag(actor.PlayerSession, AdminFlags.Fun))
            return;

        var target = args.Target;
        var session = actor.PlayerSession;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("lactation-admin-verb"),
            Category = VerbCategory.Tricks,
            Priority = 10,
            Impact = LogImpact.Low,
            Act = () => _eui.OpenEui(new LactationAdminEui(target), session),
        });
    }
}
