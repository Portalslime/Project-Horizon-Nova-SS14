// LuaWorld - This file is licensed under AGPLv3
// Copyright (c) 2025 LuaWorld
// See AGPLv3.txt for details.

using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Server._Lua.ERP;

/// <summary>
///     Гарантирует, что персонажи с выбранным полом "Женский" всегда появляются
///     в бюстгальтере ("ливчике") в слоте верхнего белья.
///     Работает для всех должностей и фракций, так как срабатывает при любом
///     обычном спавне игрока.
/// </summary>
public sealed class FemaleUndergarmentSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    /// <summary>
    ///     Слот верхнего белья.
    /// </summary>
    private const string TopUndergarmentSlot = "underweart";

    /// <summary>
    ///     Бюстгальтер, который выдаётся по умолчанию, если слот пуст.
    /// </summary>
    private const string DefaultBra = "ClothingUnderTopBraGrey";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        // Только для выбранного пола "Женский".
        if (args.Profile.Sex != Sex.Female)
            return;

        // Только для гуманоидов с инвентарём.
        if (!HasComp<HumanoidAppearanceComponent>(args.Mob)
            || !TryComp<InventoryComponent>(args.Mob, out var inventory))
        {
            return;
        }

        if (!_prototypes.HasIndex<EntityPrototype>(DefaultBra))
            return;

        // Если в слоте уже что-то есть — уважаем выбор игрока (майка, корсет и т.п.).
        if (_inventory.TryGetSlotEntity(args.Mob, TopUndergarmentSlot, out _, inventory))
            return;

        var bra = Spawn(DefaultBra, Transform(args.Mob).Coordinates);
        if (!_inventory.TryEquip(args.Mob, bra, TopUndergarmentSlot, silent: true, force: true, inventory: inventory))
            Del(bra);
    }
}
