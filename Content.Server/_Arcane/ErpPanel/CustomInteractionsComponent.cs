namespace Content.Server._Arcane.ErpPanel;

/// <summary>
/// HN: кэш пользовательских эмоций игрока, загруженный из БД.
/// </summary>
[RegisterComponent]
public sealed partial class CustomInteractionsComponent : Component
{
    public readonly List<Shared._Arcane.ErpPanel.ErpPanelCustomInteraction> Interactions = new();

    public bool Loaded;
}
