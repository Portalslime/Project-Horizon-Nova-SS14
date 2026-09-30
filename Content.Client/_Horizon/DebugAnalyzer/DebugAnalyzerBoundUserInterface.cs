using Content.Shared._Horizon.DebugAnalyzer;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Horizon.DebugAnalyzer;

[UsedImplicitly]
public sealed class DebugAnalyzerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private DebugAnalyzerWindow? _window;

    public DebugAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<DebugAnalyzerWindow>();
        _window.Title = Loc.GetString("debug-analyzer-window-title");
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (message is DebugAnalyzerScannedMessage cast)
            _window?.Populate(cast);
    }
}
