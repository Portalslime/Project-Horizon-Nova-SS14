using Robust.Shared.Serialization;

namespace Content.Shared._Arcane.ErpPanel;

[Serializable, NetSerializable]
public enum ErpPanelKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ErpPanelBuiState(NetEntity user, NetEntity target, List<ErpPanelCustomInteraction> customInteractions) : BoundUserInterfaceState
{
    public NetEntity User { get; } = user;
    public NetEntity Target { get; } = target;

    /// <summary>
    /// HN: эмоции, созданные самим игроком.
    /// </summary>
    public List<ErpPanelCustomInteraction> CustomInteractions { get; } = customInteractions;
}

[Serializable, NetSerializable]
public sealed partial class ErpPanelSendMessage(string interaction) : BoundUserInterfaceMessage
{
    public readonly string Interaction = interaction;
}

[Serializable, NetSerializable]
public sealed partial class ErpPanelSetMultipliersMessage(float arousal, float moaning) : BoundUserInterfaceMessage
{
    public readonly float Arousal = arousal;
    public readonly float Moaning = moaning;
}

[ByRefEvent]
public readonly record struct ErpPanelOpenEvent(EntityUid Target);

/// <summary>
/// HN: событие отправки эмоции в чат. Shared-код не может звать серверный ChatSystem.
/// </summary>
public sealed class ErpPanelEmoteEvent(EntityUid user, string message) : EntityEventArgs
{
    public readonly EntityUid User = user;
    public readonly string Message = message;
}
