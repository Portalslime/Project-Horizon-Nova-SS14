using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._HorizonNova.Lactation;

/// <summary>
/// HN: сцеживание молока через ПКМ-меню (см. <c>LactationSystem</c>).
/// </summary>
[Serializable, NetSerializable]
public sealed partial class LactationMilkingDoAfterEvent : SimpleDoAfterEvent
{
}
