using System.Numerics;
using Content.Shared.Alert;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Urination;

/// <summary>
/// Horizon: потребность сходить по-маленькому. По смыслу как у дефекации,
/// но копится в 2 раза быстрее, а терпеть можно в 2 раза дольше.
/// При сильной нужде персонаж немного замедляется; при перетерпевании под ним образуется лужа мочи.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class UrinationComponent : Component
{
    /// <summary>
    /// Horizon: если false — потребность никогда не накапливается и не справляется
    /// (используется для отказа видам, например Дионе, у которых иначе не убрать унаследованный компонент).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;

    /// <summary>
    /// Текущее заполнение, от 0 до порога <see cref="UrinationThreshold.Accident"/>.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Value = -1f;

    /// <summary>
    /// Случайное стартовое значение, если <see cref="Value"/> не задано явно.
    /// </summary>
    [DataField]
    public Vector2 StartingRange = new(0f, 40f);

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public UrinationThreshold CurrentThreshold = UrinationThreshold.Normal;

    /// <summary>
    /// Значение, с которого начинается каждый порог.
    /// Значения выше, чем у дефекации, чтобы «терпеть» можно было дольше.
    /// </summary>
    [DataField]
    public Dictionary<UrinationThreshold, float> Thresholds = new()
    {
        { UrinationThreshold.Normal, 0f },
        { UrinationThreshold.Urge, 100f },
        { UrinationThreshold.Critical, 320f },
        { UrinationThreshold.Accident, 400f },
    };

    /// <summary>
    /// Прирост в секунду без еды и питья. В 2 раза быстрее дефекации.
    /// </summary>
    [DataField]
    public float BaseFillRate = 0.05f;

    [DataField]
    public float HungerFactor = 0.1f;

    [DataField]
    public float ThirstFactor = 0.05f;

    [ViewVariables]
    public float LastHunger = -1f;

    [ViewVariables]
    public float LastThirst = -1f;

    [DataField, AutoPausedField]
    public TimeSpan NextUpdateTime;

    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Реагент лужи под персонажем при аварии.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> AccidentReagent = "Urine";

    /// <summary>
    /// Сколько реагента выливается при аварии.
    /// </summary>
    [DataField]
    public FixedPoint2 AccidentAmount = 20;

    [DataField]
    public EntProtoId Action = "ActionUrinate";

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

    [DataField]
    public SoundSpecifier SeatSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splash.ogg");

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public ProtoId<AlertCategoryPrototype> AlertCategory = "Urination";

    [DataField]
    public Dictionary<UrinationThreshold, ProtoId<AlertPrototype>> Alerts = new()
    {
        { UrinationThreshold.Urge, "UrinationUrge" },
        { UrinationThreshold.Critical, "UrinationCritical" },
    };

    /// <summary>
    /// Множитель скорости, пока персонаж хочет писать (порог <see cref="UrinationThreshold.Urge"/> и выше).
    /// 0.93 — примерно на 7% медленнее.
    /// </summary>
    [DataField]
    public float SlowdownModifier = 0.93f;
}

public enum UrinationThreshold : byte
{
    Normal,
    Urge,
    Critical,
    Accident,
}
