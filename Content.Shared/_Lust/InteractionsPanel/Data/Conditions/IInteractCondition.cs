namespace Content.Shared._Lust.InteractionsPanel.Data.Conditions;

public interface IInteractionCondition
{
    bool IsMet(EntityUid initiator, EntityUid target, EntityManager entityManager);
}

public interface IAppearCondition : IInteractionCondition
{
}
