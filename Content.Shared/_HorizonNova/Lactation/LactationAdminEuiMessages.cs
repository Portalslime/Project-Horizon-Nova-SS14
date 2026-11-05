using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._HorizonNova.Lactation;

/// <summary>
/// HN: состояние админ-окна «Изменение груди».
/// </summary>
[Serializable, NetSerializable]
public sealed class LactationAdminEuiState : EuiStateBase
{
    /// <summary>Целевой персонаж (сеть).</summary>
    public NetEntity Target { get; }

    /// <summary>Имя цели для заголовка.</summary>
    public string TargetName { get; }

    /// <summary>Локализованное имя расы.</summary>
    public string SpeciesName { get; }

    /// <summary>Является ли цель женщиной.</summary>
    public bool Female { get; }

    /// <summary>Можно ли менять размер.</summary>
    public bool CanChange { get; }

    /// <summary>Ключ локали с причиной отказа (если CanChange == false).</summary>
    public string? DenyKey { get; }

    /// <summary>Текущий размер груди 1..5 (0 — нет).</summary>
    public int CurrentSize { get; }

    /// <summary>Есть ли у расы спрайты/маркировки груди (Human/Demon).</summary>
    public bool HasMarkings { get; }

    public LactationAdminEuiState(
        NetEntity target,
        string targetName,
        string speciesName,
        bool female,
        bool canChange,
        string? denyKey,
        int currentSize,
        bool hasMarkings)
    {
        Target = target;
        TargetName = targetName;
        SpeciesName = speciesName;
        Female = female;
        CanChange = canChange;
        DenyKey = denyKey;
        CurrentSize = currentSize;
        HasMarkings = hasMarkings;
    }
}

/// <summary>
/// HN: выбор размера груди в админ-окне.
/// </summary>
[Serializable, NetSerializable]
public sealed class LactationAdminSetBreastMessage : EuiMessageBase
{
    /// <summary>Размер 1..5; 0 — убрать грудь.</summary>
    public int Size { get; }

    public LactationAdminSetBreastMessage(int size)
    {
        Size = size;
    }
}
