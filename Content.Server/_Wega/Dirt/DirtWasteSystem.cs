using Content.Shared._Horizon.Defecation;
using Content.Shared._Horizon.Urination;
using Content.Shared.DirtVisuals;
using Content.Shared.Inventory;

namespace Content.Server.DirtVisuals;

/// <summary>
/// HN: загрязнение белья и комбинезона при «аварии» — непроизвольном мочеиспускании/дефекации.
/// Система грязи портирована с Wega; события взяты из Content.Shared._Horizon.
/// </summary>
public sealed partial class DirtWasteSystem : EntitySystem
{
    [Dependency] private SharedDirtSystem _dirt = default!;

    /// <summary>Сколько грязи добавляется за инцидент. Порог видимости — 20.</summary>
    private const float AccidentDirtAmount = 35f;

    /// <summary>Приоритет: сначала трусы, затем майка, затем комбинезон.</summary>
    private static readonly string[] WasteSlots = { "underwearb", "underweart", "jumpsuit" };

    public override void Initialize()
    {
        base.Initialize();

        // Подписка идёт на InventoryComponent, а не на DefecationComponent: DefecationAccidentSystem
        // уже держит пару (DefecationComponent, DefecationAccidentEvent), а Robust разрешает только
        // один обработчик на пару «компонент — событие».
        SubscribeLocalEvent<InventoryComponent, DefecationAccidentEvent>(OnDefecationAccident);
        SubscribeLocalEvent<InventoryComponent, UrinationAccidentEvent>(OnUrinationAccident);
    }

    private void OnDefecationAccident(EntityUid uid, InventoryComponent comp, ref DefecationAccidentEvent args)
        => _dirt.AddDirtToSlots(uid, AccidentDirtAmount, _dirt.GetReagentColor("Feces"), WasteSlots);

    private void OnUrinationAccident(EntityUid uid, InventoryComponent comp, ref UrinationAccidentEvent args)
        => _dirt.AddDirtToSlots(uid, AccidentDirtAmount, _dirt.GetReagentColor("Urine"), WasteSlots);
}
