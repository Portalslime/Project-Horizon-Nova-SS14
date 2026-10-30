using Content.Shared._Arcane.ERP;
using Content.Shared._Arcane.ErpPanel;
using Content.Shared.Humanoid;
using Robust.Shared.Player;

namespace Content.Server._Arcane.ERP;

/// <summary>
/// HN: выдаёт компонент возбуждения персонажам игроков (порт с Arcane).
/// Статус ERP остаётся в ErpStatusComponent нашей сборки и
/// применяется в StationSpawningSystem из профиля персонажа.
/// </summary>
public sealed class ArousalInitSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HumanoidAppearanceComponent, PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(Entity<HumanoidAppearanceComponent> ent, ref PlayerAttachedEvent args)
    {
        EnsureComp<ArousalComponent>(ent);
        EnsureComp<ErpPanelOwnerComponent>(ent);
    }
}
