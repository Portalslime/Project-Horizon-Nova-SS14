using System.Text;
using Content.Server._Horizon.Plumbing.NodeGroups;
using Content.Server.Chat.Managers;
using Content.Shared._Horizon.Plumbing;
using Content.Shared.Chat;
using Content.Shared.Chemistry.Components;
using Content.Shared.Interaction;
using Content.Shared.NodeContainer;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// Reports the pressure and the contents of the plumbing networks of whatever a
/// <see cref="PlumbingAnalyzerComponent"/> is used on, to the user's chat.
/// </summary>
public sealed class PlumbingAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<PlumbingAnalyzerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!TryComp<NodeContainerComponent>(target, out var container) ||
            !TryComp<ActorComponent>(args.User, out var actor))
            return;

        // A device can touch several networks (a pump has two sides), each is listed once.
        var reports = new List<(string Node, PlumbingNet Net)>();
        foreach (var (name, node) in container.Nodes)
        {
            if (node.NodeGroup is PlumbingNet net && !reports.Exists(r => ReferenceEquals(r.Net, net)))
                reports.Add((name, net));
        }

        if (reports.Count == 0)
            return;

        args.Handled = true;

        var message = new StringBuilder(Loc.GetString("plumbing-analyzer-header", ("target", target)));
        foreach (var (name, net) in reports)
        {
            // A device with several openings says which side each network is on.
            var key = reports.Count == 1 ? "plumbing-analyzer-network-single" : "plumbing-analyzer-network";
            message.Append('\n').Append(Loc.GetString(key,
                ("side", Loc.TryGetString("plumbing-node-" + name, out var side) ? side : name),
                ("percent", (int) MathF.Round(net.FillRatio * 100f)),
                ("volume", net.Fluid.Volume),
                ("capacity", net.Capacity)));

            if (net.Fluid.Volume <= 0)
            {
                message.Append('\n').Append(Loc.GetString("plumbing-analyzer-empty"));
                continue;
            }

            foreach (var (reagentId, quantity) in net.Fluid.Contents)
            {
                var name2 = _prototype.TryIndex<ReagentPrototype>(reagentId.Prototype, out var proto)
                    ? proto.LocalizedName
                    : reagentId.Prototype;
                message.Append('\n').Append(Loc.GetString("plumbing-analyzer-reagent",
                    ("reagent", name2),
                    ("quantity", quantity),
                    ("percent", (int) MathF.Round(quantity.Float() / net.Fluid.Volume.Float() * 100f))));
            }
        }

        var text = message.ToString();
        _chat.ChatMessageToOne(ChatChannel.Notifications, text, text, default, false, actor.PlayerSession.Channel);
    }
}
