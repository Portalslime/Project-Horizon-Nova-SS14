using System;
using System.Collections.Generic;
using System.Globalization;
using Content.Server._Horizon.Urination;
using Content.Shared._Horizon.Urination;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

/// <summary>
/// Horizon: debug-команда, мгновенно заполняет уровень потребности «по-маленькому».
/// seturination &lt;value|threshold&gt; [entity]
/// threshold: normal / urge / critical / accident (без учёта регистра). Без entity — к себе.
/// </summary>
[AdminCommand(AdminFlags.Debug)]
public sealed class SetUrinationCommand : LocalizedEntityCommands
{
    [Dependency] private readonly UrinationSystem _urination = default!;

    public override string Command => "seturination";

    public override string Description => "Set a humanoid's urination need to a value or threshold (debug).";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            shell.WriteLine("Usage: seturination <value|normal|urge|critical|accident> [entity]");
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

        if (!EntityManager.TryGetComponent(target, out UrinationComponent? comp))
        {
            shell.WriteError("Target has no UrinationComponent.");
            return;
        }

        if (!TryParseAmount(comp.Thresholds, amountArg, UrinationThreshold.Accident, out var value))
        {
            shell.WriteError($"Invalid value or threshold: {amountArg}");
            return;
        }

        _urination.SetNeed(target, value, comp);
        shell.WriteLine($"Urination set to {value:0.###}.");
    }

    private static bool TryParseAmount(Dictionary<UrinationThreshold, float> thresholds, string arg,
        UrinationThreshold maxKey, out float value)
    {
        if (float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            value = Math.Clamp(value, 0f, thresholds[maxKey]);
            return true;
        }

        if (Enum.TryParse<UrinationThreshold>(arg, true, out var threshold) && thresholds.TryGetValue(threshold, out value))
            return true;

        value = 0f;
        return false;
    }
}
