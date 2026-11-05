using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.Humanoid;
using Content.Shared._HorizonNova.Lactation;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared.Humanoid;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._HorizonNova.Lactation;

/// <summary>
/// HN: админ-окно «Изменение груди» — целевой персонаж и варианты размера.
/// </summary>
public sealed class LactationAdminEui : BaseEui
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly EntityUid _target;
    private readonly HumanoidAppearanceSystem _humanoid;
    private readonly LactationSystem _lactation;

    public LactationAdminEui(EntityUid target)
    {
        IoCManager.InjectDependencies(this);
        _target = target;
        _humanoid = _entity.System<HumanoidAppearanceSystem>();
        _lactation = _entity.System<LactationSystem>();
    }

    public override EuiStateBase GetNewState()
    {
        return BuildState();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not LactationAdminSetBreastMessage set)
            return;

        if (!_adminManager.HasAdminFlag(Player, AdminFlags.Fun))
            return;

        if (!_entity.TryGetComponent<HumanoidAppearanceComponent>(_target, out var humanoid)
            || humanoid.Sex != Sex.Female
            || !LactationHelpers.LactatingSpecies.Contains(humanoid.Species))
        {
            return;
        }

        var size = Math.Clamp(set.Size, 0, 5);
        var markings = LactationHelpers.GetBreastMarkings(humanoid.Species);

        if (markings != null)
        {
            foreach (var marking in markings)
                _humanoid.RemoveMarking(_target, marking, humanoid: humanoid);

            if (size > 0)
                _humanoid.AddMarking(_target, markings[size - 1], forced: true, humanoid: humanoid);

            if (_entity.TryGetComponent<LactationComponent>(_target, out var lacMarkings))
            {
                lacMarkings.BreastSizeOverride = 0;
                _entity.Dirty(_target, lacMarkings);
            }
        }
        else if (_entity.TryGetComponent<LactationComponent>(_target, out var lac))
        {
            // У Resomi/Dwarf нет грудных marking'ов — меняем только механику.
            // 0 — «убрать грудь», поэтому храним -1 как явное «нет груди».
            lac.BreastSizeOverride = size == 0 ? -1 : size;
            _entity.Dirty(_target, lac);
        }

        _lactation.RefreshLactation(_target);
        StateDirty();
    }

    private EuiStateBase BuildState()
    {
        var netTarget = _entity.GetNetEntity(_target);

        if (!_entity.TryGetComponent<HumanoidAppearanceComponent>(_target, out var humanoid)
            || !_entity.TryGetComponent<MetaDataComponent>(_target, out var metadata))
        {
            return new LactationAdminEuiState(
                netTarget,
                string.Empty,
                string.Empty,
                false,
                false,
                "lactation-admin-deny-missing",
                0,
                false);
        }

        var speciesName = _prototypes.TryIndex(humanoid.Species, out var species)
            ? Loc.GetString(species.Name)
            : humanoid.Species.Id;

        var female = humanoid.Sex == Sex.Female;
        var supported = LactationHelpers.LactatingSpecies.Contains(humanoid.Species);
        var markings = LactationHelpers.GetBreastMarkings(humanoid.Species);
        var overrideSize = _entity.TryGetComponent<LactationComponent>(_target, out var lac)
            ? lac.BreastSizeOverride
            : 0;
        var size = LactationHelpers.GetBreastSize(humanoid, overrideSize);

        string? deny = null;
        if (!female)
            deny = "lactation-admin-deny-female";
        else if (!supported)
            deny = "lactation-admin-deny-species";

        return new LactationAdminEuiState(
            netTarget,
            metadata.EntityName,
            speciesName,
            female,
            deny == null,
            deny,
            size,
            markings != null);
    }
}
