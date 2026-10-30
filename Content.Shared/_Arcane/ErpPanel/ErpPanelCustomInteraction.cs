using Robust.Shared.Serialization;

namespace Content.Shared._Arcane.ErpPanel;

/// <summary>
/// HN: пользовательское взаимодействие (эмоция), созданное игроком.
/// Хранится на сервере в БД и приходит в состоянии панели.
/// </summary>
[Serializable, NetSerializable]
public sealed class ErpPanelCustomInteraction
{
    public const string CustomCategoryId = "Custom";
    public const string DefaultIcon = "/Textures/_Arcane/Interface/ErpPanel/heart_icon.png";

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = CustomCategoryId;
    public string Message { get; set; } = string.Empty;
    public string SelfMessage { get; set; } = string.Empty;
    public float Cooldown { get; set; } = 3f;

    public ErpPanelCustomInteraction()
    {
    }

    public ErpPanelCustomInteraction(int id, string name, string category, string message, string selfMessage, float cooldown)
    {
        Id = id;
        Name = name;
        Category = category;
        Message = message;
        SelfMessage = selfMessage;
        Cooldown = cooldown;
    }

    public ErpPanelCustomInteraction Clone()
    {
        return new ErpPanelCustomInteraction(Id, Name, Category, Message, SelfMessage, Cooldown);
    }
}

[Serializable, NetSerializable]
public sealed partial class ErpPanelSaveCustomInteractionMessage : BoundUserInterfaceMessage
{
    public ErpPanelCustomInteraction Interaction { get; }

    public ErpPanelSaveCustomInteractionMessage(ErpPanelCustomInteraction interaction)
    {
        Interaction = interaction;
    }
}

[Serializable, NetSerializable]
public sealed partial class ErpPanelDeleteCustomInteractionMessage : BoundUserInterfaceMessage
{
    public int Id { get; }

    public ErpPanelDeleteCustomInteractionMessage(int id)
    {
        Id = id;
    }
}

/// <summary>
/// HN: серверная система добавляет сюда пользовательские взаимодействия при сборке состояния панели.
/// </summary>
public sealed class ErpPanelCollectCustomInteractionsEvent : EntityEventArgs
{
    public readonly EntityUid User;
    public readonly EntityUid Target;
    public readonly List<ErpPanelCustomInteraction> Interactions = new();

    public ErpPanelCollectCustomInteractionsEvent(EntityUid user, EntityUid target)
    {
        User = user;
        Target = target;
    }
}
