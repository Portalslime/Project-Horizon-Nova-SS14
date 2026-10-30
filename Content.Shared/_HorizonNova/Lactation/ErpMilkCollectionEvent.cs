namespace Content.Shared._HorizonNova.Lactation;

/// <summary>
/// Поднимается на пользователе после выполнения ERP-интеракции с флагом
/// <c>PanelInteractionPrototype.MilkCollection</c>: нужно сцедить молоко
/// у <see cref="Target"/> (может совпадать с <see cref="User"/>).
/// </summary>
public sealed class ErpMilkCollectionEvent(EntityUid user, EntityUid target) : EntityEventArgs
{
    public readonly EntityUid User = user;
    public readonly EntityUid Target = target;
}
