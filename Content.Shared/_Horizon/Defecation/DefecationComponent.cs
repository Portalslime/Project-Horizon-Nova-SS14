using System.Numerics;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Defecation;

/// <summary>
/// A need for going to the toilet, similar to hunger/thirst. The value grows over time and faster
/// when the entity eats and drinks. Handled by DefecationSystem.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class DefecationComponent : Component
{
    /// <summary>
    /// Current fill of the need, from 0 up to the <see cref="DefecationThreshold.Accident"/> threshold.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Value = -1f;

    /// <summary>
    /// Random starting value range, used when <see cref="Value"/> is not defined explicitly.
    /// </summary>
    [DataField]
    public Vector2 StartingRange = new(0f, 30f);

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public DefecationThreshold CurrentThreshold = DefecationThreshold.Normal;

    /// <summary>
    /// The value at which each threshold begins.
    /// </summary>
    [DataField]
    public Dictionary<DefecationThreshold, float> Thresholds = new()
    {
        { DefecationThreshold.Normal, 0f },
        { DefecationThreshold.Urge, 50f },
        { DefecationThreshold.Critical, 80f },
        { DefecationThreshold.Accident, 100f },
    };

    /// <summary>
    /// Fill added per second with no eating or drinking.
    /// </summary>
    [DataField]
    public float BaseFillRate = 0.025f;

    /// <summary>
    /// Fill added for each point of hunger gained (eating).
    /// </summary>
    [DataField]
    public float HungerFactor = 0.1f;

    /// <summary>
    /// Fill added for each point of thirst gained (drinking).
    /// </summary>
    [DataField]
    public float ThirstFactor = 0.05f;

    /// <summary>
    /// Last observed hunger/thirst values, used to detect eating and drinking.
    /// </summary>
    [ViewVariables]
    public float LastHunger = -1f;

    [ViewVariables]
    public float LastThirst = -1f;

    [DataField, AutoPausedField]
    public TimeSpan NextUpdateTime;

    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The entity spawned under the owner.
    /// </summary>
    [DataField]
    public EntProtoId Product = "Feces";

    [DataField]
    public EntProtoId Action = "ActionDefecate";

    /// <summary>
    /// Played when relieving themselves on the floor, by choice or by accident.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

    /// <summary>
    /// Played when relieving themselves on a toilet.
    /// </summary>
    [DataField]
    public SoundSpecifier SeatSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splash.ogg");

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public ProtoId<AlertCategoryPrototype> AlertCategory = "Defecation";

    [DataField]
    public Dictionary<DefecationThreshold, ProtoId<AlertPrototype>> Alerts = new()
    {
        { DefecationThreshold.Urge, "DefecationUrge" },
        { DefecationThreshold.Critical, "DefecationCritical" },
    };

    /// <summary>
    /// Gases the owner emits for <see cref="AccidentDuration"/> after an accident, moles per second.
    /// </summary>
    [DataField]
    public Dictionary<Gas, float> AccidentGases = new()
    {
        { Gas.Ammonia, 0.2f },
    };

    /// <summary>
    /// How often the accident gases are released. With the defaults that is 2 moles of ammonia every 10 seconds.
    /// </summary>
    [DataField]
    public TimeSpan AccidentEmitInterval = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan AccidentDuration = TimeSpan.FromMinutes(5);
}

public enum DefecationThreshold : byte
{
    Normal,
    Urge,
    Critical,
    Accident,
}
