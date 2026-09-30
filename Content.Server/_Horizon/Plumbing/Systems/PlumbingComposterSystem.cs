using Content.Server.Materials;
using Content.Server.Power.EntitySystems;
using Content.Shared._Horizon.Plumbing;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.NodeContainer;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.Plumbing.Systems;

/// <summary>
/// Turns the reagent of the plumbing network of a <see cref="PlumbingComposterComponent"/> into material in its
/// storage. Using the machine takes the stored material out. There is no window: it is a counter, not entities.
/// </summary>
public sealed class PlumbingComposterSystem : PlumbingTimedSystem<PlumbingComposterComponent>
{
    [Dependency] private readonly MaterialStorageSystem _materials = default!;
    [Dependency] private readonly PlumbingSystem _plumbing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingComposterComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PlumbingComposterComponent, ActivateInWorldEvent>(OnActivated);
    }

    private void OnExamined(Entity<PlumbingComposterComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("plumbing-composter-examine-stored",
            ("amount", _materials.GetMaterialAmount(ent, ent.Comp.Material.Id))));
        args.PushMarkup(Loc.GetString(_plumbing.TryGetNet(ent, ent.Comp.NodeName, out _)
            ? "plumbing-examine-connected"
            : "plumbing-examine-disconnected"));
    }

    private void OnActivated(Entity<PlumbingComposterComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        args.Handled = true;

        var amount = _materials.GetMaterialAmount(ent, ent.Comp.Material.Id);
        if (amount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("plumbing-composter-empty"), ent, args.User);
            return;
        }

        // The sheets appear at the user's feet, ready to be picked up.
        _materials.EjectMaterial(ent, ent.Comp.Material.Id, coordinates: Transform(args.User).Coordinates);
        _popup.PopupEntity(Loc.GetString("plumbing-composter-collected", ("amount", amount)), ent, args.User);
    }

    protected override void Tick(EntityUid uid, PlumbingComposterComponent composter,
        NodeContainerComponent container, float seconds)
    {
        if (!this.IsPowered(uid, EntityManager) ||
            !_plumbing.TryGetNet(uid, composter.NodeName, out var net, container))
            return;

        var amount = FixedPoint2.Min(net.Fluid.GetTotalPrototypeQuantity(composter.Reagent), composter.Rate * seconds);
        if (amount <= 0)
            return;

        // Store the whole units first: if the storage is full nothing is taken out of the pipes.
        var progress = composter.Progress + amount.Float() * composter.MaterialPerUnit;
        var whole = (int) MathF.Floor(progress);
        if (whole > 0 && !_materials.TryChangeMaterialAmount(uid, composter.Material.Id, whole))
            return;

        composter.Progress = progress - whole;
        net.Fluid.SplitSolutionWithOnly(amount, composter.Reagent);
    }
}
