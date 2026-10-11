using Content.Shared.DirtVisuals;
using Robust.Client.GameObjects;

namespace Content.Client.WashingMachine;

// Порт с ss14-wega (wega-team/ss14-wega), Content.Client/_Wega/Dirt/WashingMachineSystem.cs
public sealed partial class WashingMachineSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WashingMachineComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(EntityUid uid, WashingMachineComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData(uid, WashingMachineVisuals.IsWashing, out bool washing))
            washing = false;

        _sprite.LayerSetVisible(uid, WashingMachineVisuals.Washing, washing);
    }
}
