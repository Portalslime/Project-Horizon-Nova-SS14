using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Medical.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Temperature.Components;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Medical.Cryogenics;
using Content.Shared.MedicalScanner;
using Content.Shared.UserInterface;

namespace Content.Server.Medical;

public sealed partial class CryoPodSystem : SharedCryoPodSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly GasCanisterSystem _gasCanisterSystem = default!;
    [Dependency] private readonly GasAnalyzerSystem _gasAnalyzerSystem = default!;
    [Dependency] private readonly HealthAnalyzerSystem _healthAnalyzerSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoPodComponent, AtmosDeviceUpdateEvent>(OnCryoPodUpdateAtmosphere);
        SubscribeLocalEvent<CryoPodComponent, GasAnalyzerScanEvent>(OnGasAnalyzed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = Timing.CurTime;
        var query = EntityQueryEnumerator<ActiveCryoPodComponent, CryoPodComponent>();

        while (query.MoveNext(out var uid, out _, out var cryoPod))
        {
            if (curTime < cryoPod.NextUiUpdateTime)
                continue;

            cryoPod.NextUiUpdateTime += cryoPod.UiUpdateInterval;
            Dirty(uid, cryoPod);
            UpdateUi((uid, cryoPod));
        }
    }

    protected override void UpdateUi(Entity<CryoPodComponent> entity)
    {
        if (!UI.IsUiOpen(entity.Owner, CryoPodUiKey.Key))
            return;

        var patient = entity.Comp.BodyContainer.ContainedEntity;
        var gasMix = TryComp<CryoPodAirComponent>(entity, out var air)
            ? _gasAnalyzerSystem.GenerateGasMixEntry("Cryo pod", air.Air)
            : _gasAnalyzerSystem.GenerateGasMixEntry("Cryo pod", null);
        var patientTemperature = patient is { } p && TryComp<TemperatureComponent>(p, out var temperature)
            ? temperature.CurrentTemperature
            : float.NaN;
        var (beakerCapacity, beaker) = GetBeakerInfo(entity);
        var injecting = GetInjectingReagents(entity);

        UI.ServerSendUiMessage(
            entity.Owner,
            CryoPodUiKey.Key,
            new CryoPodUserMessage(gasMix, patient != null, patientTemperature, beakerCapacity, beaker, injecting)
        );

        var health = patient is { } target && HasComp<DamageableComponent>(target)
            ? _healthAnalyzerSystem.CreateHealthAnalyzerMessage(target, true)
            : new HealthAnalyzerScannedUserMessage(null, float.NaN, float.NaN, null, null, null, null);

        UI.ServerSendUiMessage(entity.Owner, CryoPodUiKey.Key, health);
    }

    #region Atmos handler

    private void OnCryoPodUpdateAtmosphere(Entity<CryoPodComponent> entity, ref AtmosDeviceUpdateEvent args)
    {
        if (!_nodeContainer.TryGetNode(entity.Owner, entity.Comp.PortName, out PortablePipeNode? portNode))
            return;

        if (!TryComp(entity, out CryoPodAirComponent? cryoPodAir))
            return;

        _atmosphereSystem.React(cryoPodAir.Air, portNode);

        if (portNode.NodeGroup is PipeNet { NodeCount: > 1 } net)
        {
            _gasCanisterSystem.MixContainerWithPipeNet(cryoPodAir.Air, net.Air);
        }
    }

    private void OnGasAnalyzed(Entity<CryoPodComponent> entity, ref GasAnalyzerScanEvent args)
    {
        if (!TryComp(entity, out CryoPodAirComponent? cryoPodAir))
            return;

        args.GasMixtures ??= new List<(string, GasMixture?)>();
        args.GasMixtures.Add((Name(entity.Owner), cryoPodAir.Air));
        // If it's connected to a port, include the port side
        // multiply by volume fraction to make sure to send only the gas inside the analyzed pipe element, not the whole pipe system
        if (_nodeContainer.TryGetNode(entity.Owner, entity.Comp.PortName, out PipeNode? port) && port.Air.Volume != 0f)
        {
            var portAirLocal = port.Air.Clone();
            portAirLocal.Multiply(port.Volume / port.Air.Volume);
            portAirLocal.Volume = port.Volume;
            args.GasMixtures.Add((entity.Comp.PortName, portAirLocal));
        }
    }

    #endregion
}
