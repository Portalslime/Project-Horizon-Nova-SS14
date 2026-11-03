using Content.Server.Administration.Managers;
using Content.Server.Humanoid;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._HorizonNova.Lactation;

/// <summary>
/// Админ-инструменты для черт внешности, связанных с лактацией: быстрое изменение
/// размера груди через ПКМ → «Трюки» (admin abuse). Полный редактор marking'ов
/// по-прежнему доступен отдельным пунктом «Modify markings».
/// </summary>
public sealed class LactationAdminVerbSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;

    private static readonly (string NameKey, string MarkingId)[] HumanBreasts =
    {
        ("lactation-admin-breast-small", "HumanBreastSmall"),
        ("lactation-admin-breast-default", "HumanBreastDefault"),
        ("lactation-admin-breast-medium", "HumanBreastMedium"),
        ("lactation-admin-breast-big", "HumanBreastBig"),
        ("lactation-admin-breast-superbig", "HumanBreastSuperBig"),
    };

    private static readonly (string NameKey, string MarkingId)[] DemonBreasts =
    {
        ("lactation-admin-breast-small", "DemonBreastSmall"),
        ("lactation-admin-breast-default", "DemonBreastDefault"),
        ("lactation-admin-breast-medium", "DemonBreastMedium"),
        ("lactation-admin-breast-big", "DemonBreastBig"),
        ("lactation-admin-breast-superbig", "DemonBreastSuperBig"),
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor)
            || !_adminManager.HasAdminFlag(actor.PlayerSession, AdminFlags.Fun))
            return;

        if (!TryComp<HumanoidAppearanceComponent>(args.Target, out var humanoid))
            return;

        (string NameKey, string MarkingId)[]? breasts = null;
        if (humanoid.Species.Id == "Human")
            breasts = HumanBreasts;
        else if (humanoid.Species.Id == "Demon")
            breasts = DemonBreasts;

        if (breasts == null)
            return;

        var target = args.Target;

        foreach (var (nameKey, markingId) in breasts)
        {
            var selected = markingId;
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString(nameKey),
                Category = VerbCategory.Tricks,
                Act = () => SetBreast(target, humanoid, breasts, selected),
                Impact = LogImpact.Low,
                Message = Loc.GetString("lactation-admin-breast-desc"),
                Priority = 10,
            });
        }

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("lactation-admin-breast-remove"),
            Category = VerbCategory.Tricks,
            Act = () => SetBreast(target, humanoid, breasts, null),
            Impact = LogImpact.Low,
            Message = Loc.GetString("lactation-admin-breast-desc"),
            Priority = 9,
        });
    }

    /// <summary>
    /// Убирает все грудные marking'и расы и (если задан) добавляет выбранный размер.
    /// </summary>
    private void SetBreast(EntityUid target, HumanoidAppearanceComponent humanoid,
        (string NameKey, string MarkingId)[] breasts, string? markingId)
    {
        foreach (var (_, id) in breasts)
        {
            _humanoid.RemoveMarking(target, id, humanoid: humanoid);
        }

        if (markingId != null)
            _humanoid.AddMarking(target, markingId, forced: true, humanoid: humanoid);
    }
}
