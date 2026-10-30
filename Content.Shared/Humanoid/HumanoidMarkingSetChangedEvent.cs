namespace Content.Shared.Humanoid;

/// <summary>
/// Поднимается на гуманоиде после того, как его набор marking'ов
/// («черты внешности») изменился — добавление, удаление, смена вида, клонирование и т.п.
/// Служит точкой входа для систем, которым нужно отслеживать текущую внешность.
/// </summary>
public sealed class HumanoidMarkingSetChangedEvent : EntityEventArgs;
