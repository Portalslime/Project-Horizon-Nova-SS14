using Content.Shared.Examine;
using Content.Shared.Lua.CLVar;
using Content.Shared._NF.Bank.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Shared.DetailExaminable;

public sealed class DetailExaminableSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DetailExaminableComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<DetailExaminableComponent> ent, ref ExaminedEvent args)
    {
        if (!_cfg.GetCVar(CLVars.IsERP))
            return;

        if (!HasComp<BankAccountComponent>(ent))
            return;

        //HN: замена Lua EnumERPStatus на Lust Erp (4 варианта)
        var color = ent.Comp.Erp switch
        {
            Content.Shared.Humanoid.Erp.No => "red",
            Content.Shared.Humanoid.Erp.Ask => "yellow",
            Content.Shared.Humanoid.Erp.Yes => "green",
            Content.Shared.Humanoid.Erp.Absolute => "#ff69b4",
            _ => "yellow"
        };

        var statusText = FormattedMessage.EscapeText(ent.Comp.GetERPStatusName());
        args.PushMarkup($"[color={color}]{statusText}[/color]");
    }
}
