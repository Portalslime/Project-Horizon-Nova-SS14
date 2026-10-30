using Content.Server.Chat.Systems;
using Content.Shared._Arcane.ErpPanel;
using Content.Shared._Lua.Chat.Systems;

namespace Content.Server._Arcane.ErpPanel;

/// <summary>
/// HN: отправляет эмоции ERP-панели в чат (только серверная сторона).
/// </summary>
public sealed class ErpPanelEmoteSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ErpPanelEmoteEvent>(OnEmote);
    }

    private void OnEmote(ErpPanelEmoteEvent ev)
    {
        if (!Exists(ev.User))
            return;

        _chat.TrySendInGameICMessage(ev.User, ev.Message, InGameICChatType.Emote, false);
    }
}
