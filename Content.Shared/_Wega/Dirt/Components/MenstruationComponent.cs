using System.Numerics;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DirtVisuals;

/// <summary>
/// HN: менструация — периодически пачкает бельё/комбинезон кровью. Только женские персонажи.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MenstruationComponent : Component
{
    /// <summary>
    /// Выключено для мужских персонажей и видов-исключений (как у дефекации).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;

    /// <summary>Интервал между событиями.</summary>
    [DataField]
    public TimeSpan Interval = TimeSpan.FromMinutes(15);

    /// <summary>Случайный стартовый разброс в секундах.</summary>
    [DataField]
    public Vector2 StartingRange = new(0f, 900f);

    [ViewVariables]
    public TimeSpan NextEvent;

    /// <summary>Сколько грязи добавляется за событие.</summary>
    [DataField]
    public float Amount = 25f;

    /// <summary>Чем пачкается.</summary>
    [DataField]
    public ProtoId<ReagentPrototype> Reagent = "Blood";

    /// <summary>В какие слоты попадает.</summary>
    [DataField]
    public string[] Slots = { "underwearb", "jumpsuit" };
}
