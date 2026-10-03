using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client._HorizonNova.Humanoid;

/// <summary>
/// Horizon Nova: view model for UIs manipulating a set of markings.
/// Port of the Wega markings view model (Content.Client/Humanoid/MarkingsViewModel.cs),
/// adapted to the category-based markings backend used by Horizon Nova.
/// </summary>
public sealed partial class MarkingsViewModel
{
    [Dependency] private readonly MarkingManager _marking = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    /// <summary>
    /// Whether the view model will enforce the per-category marking limits.
    /// </summary>
    public bool EnforceLimits { get; set; } = true;

    /// <summary>
    /// Whether the view model will enforce species and sex restrictions.
    /// </summary>
    public bool EnforceSpeciesAndSex { get; set; } = true;

    /// <summary>
    /// Whether newly added markings should be forced (ignoring point limits). Used by admin tools.
    /// </summary>
    public bool Forced { get; set; }

    public string Species = SharedHumanoidAppearanceSystem.DefaultSpecies;
    public Sex Sex = Sex.Male;
    public Color SkinColor = Color.White;
    public Color EyeColor = Color.Black;

    private MarkingSet _markings = new();

    /// <summary>
    /// The currently applied set of markings.
    /// </summary>
    public MarkingSet Markings
    {
        get => _markings;
        set
        {
            _markings = value;
            MarkingsReset?.Invoke();
        }
    }

    private Dictionary<MarkingRegion, HashSet<MarkingCategories>> _organData = new();

    /// <summary>
    /// The categories that can be edited for the current species and sex, grouped by body region.
    /// Mirrors the organ data of the Wega markings view model.
    /// </summary>
    public Dictionary<MarkingRegion, HashSet<MarkingCategories>> OrganData
    {
        get => _organData;
        set
        {
            _organData = value;
            OrganDataChanged?.Invoke();
        }
    }

    /// <summary>
    /// Raised whenever the set of possible markings and their grouping may have changed.
    /// </summary>
    public event Action? OrganDataChanged;

    /// <summary>
    /// Raised whenever the set of markings has fully changed and requires a UI reload.
    /// </summary>
    public event Action? MarkingsReset;

    /// <summary>
    /// Raised whenever a specific category's markings have changed.
    /// </summary>
    public event Action<MarkingCategories, MarkingChangeType>? MarkingsChanged;

    public MarkingsViewModel()
    {
        IoCManager.InjectDependencies(this);
    }

    /// <summary>
    /// Returns the markings that can be applied to the given category for the current species and sex.
    /// </summary>
    public IReadOnlyDictionary<string, MarkingPrototype> GetAvailable(MarkingCategories category)
    {
        return EnforceSpeciesAndSex
            ? _marking.MarkingsByCategoryAndSpeciesAndSex(category, Species, Sex)
            : _marking.MarkingsByCategoryAndSex(category, Sex);
    }

    /// <summary>
    /// Maps a flat marking category onto the body region used by the picker navigation.
    /// </summary>
    public static MarkingRegion RegionOf(MarkingCategories category)
    {
        return category switch
        {
            MarkingCategories.Hair or MarkingCategories.FacialHair or MarkingCategories.Head
                or MarkingCategories.HeadTop or MarkingCategories.HeadSide or MarkingCategories.Snout
                => MarkingRegion.Head,
            MarkingCategories.Arms => MarkingRegion.Arms,
            MarkingCategories.Legs => MarkingRegion.Legs,
            MarkingCategories.Tail => MarkingRegion.Tail,
            MarkingCategories.Special => MarkingRegion.Special,
            _ => MarkingRegion.Torso,
        };
    }

    /// <summary>
    /// Returns whether the given marking is currently selected in the model.
    /// </summary>
    public bool IsMarkingSelected(MarkingCategories category, string markingId)
    {
        return GetMarking(category, markingId) is not null;
    }

    /// <summary>
    /// Returns whether the marking can have its color customized by the user.
    /// </summary>
    public bool IsMarkingColorCustomizable(MarkingCategories category, string markingId)
    {
        if (!_marking.Markings.TryGetValue(markingId, out var prototype))
            return false;

        if (prototype.ForcedColoring)
            return false;

        if (_marking.MustMatchSkin(Species, prototype.BodyPart, out _, _prototype))
            return false;

        if (_marking.MustMatchColor(Species, prototype.BodyPart, out _, _prototype) is not null)
            return false;

        return true;
    }

    /// <summary>
    /// Returns the currently applied marking by its ID, if it exists.
    /// </summary>
    public Marking? GetMarking(MarkingCategories category, string markingId)
    {
        return _markings.TryGetMarking(category, markingId, out var marking) ? marking : null;
    }

    /// <summary>
    /// Attempts to add a marking to the current set of markings.
    /// </summary>
    public bool TrySelectMarking(MarkingCategories category, string markingId)
    {
        if (!_marking.Markings.TryGetValue(markingId, out var prototype))
            return false;

        if (prototype.MarkingCategory != category)
            return false;

        if (EnforceSpeciesAndSex && !_marking.CanBeApplied(Species, Sex, prototype, _prototype))
            return false;

        var layerMarkings = GetOrCreateCategory(category);

        if (EnforceLimits && !Forced && _markings.Points.TryGetValue(category, out var points))
        {
            if (points.Points <= 0)
            {
                // If this category only accepts a single marking, replace the existing one.
                var max = points.Points + layerMarkings.Count(m => !m.Forced);
                if (max == 1 && layerMarkings.Count == 1)
                {
                    _markings.Remove(category, layerMarkings[0].MarkingId);
                    layerMarkings = GetOrCreateCategory(category);
                }
                else
                {
                    return false;
                }
            }
        }

        var markingObject = prototype.AsMarking();

        if (!_marking.MustMatchSkin(Species, prototype.BodyPart, out var skinAlpha, _prototype))
        {
            var colors = MarkingColoring.GetMarkingLayerColors(prototype, SkinColor, EyeColor, _markings);
            for (var i = 0; i < colors.Count && i < prototype.Sprites.Count; i++)
            {
                markingObject.SetColor(i, colors[i]);
            }
        }
        else
        {
            for (var i = 0; i < prototype.Sprites.Count; i++)
            {
                markingObject.SetColor(i, SkinColor.WithAlpha(skinAlpha));
            }
        }

        if (_marking.MustMatchColor(Species, prototype.BodyPart, out var forcedAlpha, _prototype) is Color forcedColor)
        {
            for (var i = 0; i < prototype.Sprites.Count; i++)
            {
                markingObject.SetColor(i, forcedColor.WithAlpha(forcedAlpha));
            }
        }

        markingObject.Forced = Forced || !EnforceLimits;

        _markings.AddBack(category, markingObject);
        RaiseChanged(category, MarkingChangeType.Added);
        return true;
    }

    /// <summary>
    /// Attempts to remove a marking from the current set of markings.
    /// </summary>
    public bool TryDeselectMarking(MarkingCategories category, string markingId)
    {
        if (EnforceLimits && _markings.Points.TryGetValue(category, out var points) && points.Required)
        {
            if (_markings.Markings.TryGetValue(category, out var existing))
            {
                var removing = existing.Count(m => m.MarkingId == markingId);
                if (removing > 0 && existing.Count - removing <= 0)
                    return false;
            }
        }

        if (!_markings.Remove(category, markingId))
            return false;

        RaiseChanged(category, MarkingChangeType.Removed);
        return true;
    }

    /// <summary>
    /// Attempts to set the color of the specified marking at the given index.
    /// </summary>
    public void TrySetMarkingColor(MarkingCategories category, string markingId, int colorIndex, Color color)
    {
        if (!_markings.TryGetMarking(category, markingId, out var marking) || marking is null)
            return;

        if (!IsMarkingColorCustomizable(category, markingId))
            return;

        if (colorIndex < 0 || colorIndex >= marking.MarkingColors.Count)
            return;

        marking.SetColor(colorIndex, color);
        RaiseChanged(category, MarkingChangeType.Color);
    }

    /// <summary>
    /// Reorders the specified marking to a position relative to the given index.
    /// </summary>
    public void ChangeMarkingOrder(MarkingCategories category,
        string markingId,
        CandidatePosition position,
        int positionIndex)
    {
        if (!_markings.Markings.TryGetValue(category, out var layerMarkings))
            return;

        var currentIndex = layerMarkings.FindIndex(marking => marking.MarkingId == markingId);
        if (currentIndex < 0)
            return;

        var currentMarking = layerMarkings[currentIndex];

        if (position == CandidatePosition.Before)
        {
            layerMarkings.RemoveAt(currentIndex);
            var insertionIndex = currentIndex < positionIndex ? positionIndex - 1 : positionIndex;
            layerMarkings.Insert(Math.Clamp(insertionIndex, 0, layerMarkings.Count), currentMarking);
        }
        else if (position == CandidatePosition.After)
        {
            layerMarkings.RemoveAt(currentIndex);
            var insertionIndex = currentIndex > positionIndex ? positionIndex + 1 : positionIndex;
            layerMarkings.Insert(Math.Clamp(insertionIndex, 0, layerMarkings.Count), currentMarking);
        }
        else
        {
            return;
        }

        RaiseChanged(category, MarkingChangeType.Rank);
    }

    /// <summary>
    /// Gets the count data for a category.
    /// </summary>
    public void GetMarkingCounts(MarkingCategories category, out bool isRequired, out int count, out int selected)
    {
        isRequired = false;
        count = -1;
        selected = 0;

        if (_markings.Markings.TryGetValue(category, out var layerMarkings))
            selected = layerMarkings.Count;

        if (!_markings.Points.TryGetValue(category, out var points))
            return;

        isRequired = points.Required;

        var nonForced = _markings.Markings.TryGetValue(category, out var selectedMarkings)
            ? selectedMarkings.Count(m => !m.Forced)
            : 0;
        count = points.Points + nonForced;
    }

    /// <summary>
    /// Ensures the markings within the model are valid for the current species and sex.
    /// </summary>
    public void ValidateMarkings()
    {
        _markings.EnsureSpecies(Species, SkinColor, _marking, _prototype);
        _markings.EnsureSexes(Sex, _marking);
        MarkingsReset?.Invoke();
    }

    private List<Marking> GetOrCreateCategory(MarkingCategories category)
    {
        if (!_markings.Markings.TryGetValue(category, out var markings))
        {
            markings = new List<Marking>();
            _markings.Markings[category] = markings;
        }

        return markings;
    }

    private void RaiseChanged(MarkingCategories category, MarkingChangeType type)
    {
        MarkingsChanged?.Invoke(category, type);
    }
}

/// <summary>
/// Specifies whether an item in a list will be moved to before or after a corresponding index.
/// </summary>
public enum CandidatePosition
{
    Before,
    After,
}

/// <summary>
/// The kind of change that happened to a category's markings.
/// </summary>
public enum MarkingChangeType : byte
{
    Added,
    Removed,
    Color,
    Rank,
}

/// <summary>
/// Coarse body region grouping used by the markings picker navigation.
/// Horizon Nova markings are stored per <see cref="MarkingCategories"/>, so a region is a
/// presentational grouping of categories, analogous to an organ in Wega.
/// </summary>
public enum MarkingRegion : byte
{
    Head,
    Torso,
    Arms,
    Legs,
    Tail,
    Special,
}
