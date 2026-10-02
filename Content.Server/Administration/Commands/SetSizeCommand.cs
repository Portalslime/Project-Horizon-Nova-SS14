using System;
using System.Numerics;
using Content.Server.Humanoid;
using Content.Shared.Administration;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Sprite;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands;

/// <summary>
/// Horizon: debug-команда для изменения роста и ширины (веса) гуманоида.
/// setsize &lt;height&gt; &lt;width&gt; [entity]
/// Множители клампятся по диапазону вида. Без entity применяется к себе.
/// </summary>
[AdminCommand(AdminFlags.Debug)]
public sealed class SetSizeCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly HumanoidSizeSystem _size = default!;
    [Dependency] private readonly SharedScaleVisualsSystem _scale = default!;

    public override string Command => "setsize";

    public override string Description => "Set a humanoid's height and width multipliers (debug).";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 2 or > 3)
        {
            shell.WriteLine("Usage: setsize <height> <width> [entity]");
            return;
        }

        if (!float.TryParse(args[0], out var height) || !float.TryParse(args[1], out var width))
        {
            shell.WriteError("Height and width must be numbers.");
            return;
        }

        EntityUid target;
        if (args.Length == 3)
        {
            if (!NetEntity.TryParse(args[2], out var net) || !EntityManager.TryGetEntity(net, out var targetUid))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            target = targetUid.Value;
        }
        else if (shell.Player?.AttachedEntity is { } self)
        {
            target = self;
        }
        else
        {
            shell.WriteError("No entity specified and you have none.");
            return;
        }

        if (!EntityManager.TryGetComponent(target, out HumanoidAppearanceComponent? humanoid))
        {
            shell.WriteError("Target has no HumanoidAppearanceComponent.");
            return;
        }

        if (!_proto.TryIndex(humanoid.Species, out var species))
        {
            shell.WriteError("Target has an unknown species.");
            return;
        }

        height = Math.Clamp(height, species.MinHeight, species.MaxHeight);
        width = Math.Clamp(width, species.MinWidth, species.MaxWidth);

        humanoid.Width = width;
        humanoid.Height = height;
        EntityManager.Dirty(target, humanoid);

        _scale.SetSpriteScale(target, new Vector2(width, height));
        _size.ApplyPhysicalStats(target, humanoid.Species, width, height);

        shell.WriteLine($"Size set: height {height:0.###}, width {width:0.###}.");
    }
}
