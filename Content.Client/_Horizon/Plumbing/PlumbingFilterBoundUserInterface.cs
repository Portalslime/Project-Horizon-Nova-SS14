using Content.Shared._Horizon.Plumbing;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Horizon.Plumbing;

[UsedImplicitly]
public sealed class PlumbingFilterBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private PlumbingFilterWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PlumbingFilterWindow>();
        _window.PopulateReagents(_prototype.EnumeratePrototypes<ReagentPrototype>());

        _window.ToggleRequested += enabled => SendMessage(new PlumbingFilterToggleMessage(enabled));
        _window.ReagentSelected += reagent => SendMessage(new PlumbingFilterSelectReagentMessage(reagent));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not PlumbingFilterBoundUserInterfaceState cast)
            return;

        _window.SetState(cast.Enabled, cast.Reagent);
    }
}
