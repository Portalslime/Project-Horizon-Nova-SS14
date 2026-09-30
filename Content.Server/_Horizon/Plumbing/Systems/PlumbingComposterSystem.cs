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
public sealed class PlumbingComposterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MaterialStorageSystem _materials = default!;
    [Dependency] private readonly PlumbingSystem _plumbing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingComposterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PlumbingComposterComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PlumbingComposterComponent, ActivateInWorldEvent>(OnActivated);
    }

    private void OnMapInit(Entity<PlumbingComposterComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval * _random.NextFloat();
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<PlumbingComposterComponent, NodeContainerComponent>();
        while (query.MoveNext(out var uid, out var composter, out var container))
        {
            if (curTime < composter.NextUpdate)
                continue;

            composter.NextUpdate = curTime + composter.UpdateInterval;
            Compost((uid, composter, container), (float) composter.UpdateInterval.TotalSeconds);
        }
    }

    private void Compost(Entity<PlumbingComposterComponent, NodeContainerComponent> ent, float seconds)
    {
        var (uid, composter, container) = ent;

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
