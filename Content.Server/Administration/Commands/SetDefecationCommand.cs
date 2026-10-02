using System;
using System.Collections.Generic;
using System.Globalization;
using Content.Server._Horizon.Defecation;
using Content.Shared._Horizon.Defecation;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

/// <summary>
/// Horizon: debug-команда, мгновенно заполняет уровень потребности «по-большому».
/// setdefecation &lt;value|threshold&gt; [entity]
/// threshold: normal / urge / critical / accident (без учёта регистра). Без entity — к себе.
/// </summary>
[AdminCommand(AdminFlags.Debug)]
public sealed class SetDefecationCommand : LocalizedEntityCommands
{
    [Dependency] private readonly DefecationSystem _defecation = default!;

    public override string Command => "setdefecation";

    public override string Description => "Set a humanoid's defecation need to a value or threshold (debug).";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            shell.WriteLine("Usage: setdefecation <value|normal|urge|critical|accident> [entity]");
            return;
        }

        EntityUid target;
        string amountArg;
        if (args.Length == 2)
        {
            if (!NetEntity.TryParse(args[1], out var net) || !EntityManager.TryGetEntity(net, out var targetUid))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            target = targetUid.Value;
            amountArg = args[0];
        }
        else if (shell.Player?.AttachedEntity is { } self)
        {
            target = self;
            amountArg = args[0];
        }
        else
        {
            shell.WriteError("No entity specified and you have none.");
            return;
        }

        if (!EntityManager.TryGetComponent(target, out DefecationComponent? comp))
        {
            shell.WriteError("Target has no DefecationComponent.");
            return;
        }

        if (!TryParseAmount(comp.Thresholds, amountArg, DefecationThreshold.Accident, out var value))
        {
            shell.WriteError($"Invalid value or threshold: {amountArg}");
            return;
        }

        _defecation.SetNeed(target, value, comp);
        shell.WriteLine($"Defecation set to {value:0.###}.");
    }

    private static bool TryParseAmount(Dictionary<DefecationThreshold, float> thresholds, string arg,
        DefecationThreshold maxKey, out float value)
    {
        if (float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            value = Math.Clamp(value, 0f, thresholds[maxKey]);
            return true;
        }

        if (Enum.TryParse<DefecationThreshold>(arg, true, out var threshold) && thresholds.TryGetValue(threshold, out value))
            return true;

        value = 0f;
        return false;
    }
}
