using Content.Shared.Chemistry.Reagent;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;

namespace Content.Client._Horizon.Plumbing;

/// <summary>
/// The window of a plumbing filter: switch it on and off and pick the reagent that is sent to the filtered side.
/// </summary>
public sealed class PlumbingFilterWindow : DefaultWindow
{
    private readonly Button _toggleButton = new();
    private readonly Label _currentLabel = new();
    private readonly LineEdit _search = new() { PlaceHolder = Loc.GetString("plumbing-filter-ui-search") };
    private readonly ItemList _list = new() { SelectMode = ItemList.ItemListSelectMode.Single, VerticalExpand = true };
    private readonly Button _confirmButton = new() { Text = Loc.GetString("plumbing-filter-ui-confirm"), Disabled = true };

    private readonly List<(string Id, string Name)> _reagents = new();
    private bool _enabled = true;
    private string? _current;
    private string? _selected;

    public event Action<bool>? ToggleRequested;
    public event Action<ProtoId<ReagentPrototype>?>? ReagentSelected;

    public PlumbingFilterWindow()
    {
        Title = Loc.GetString("plumbing-filter-ui-title");
        MinSize = new(360, 420);

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new(5),
            SeparationOverride = 8,
        };

        box.AddChild(_toggleButton);
        box.AddChild(_currentLabel);
        box.AddChild(_search);
        box.AddChild(_list);
        box.AddChild(_confirmButton);
        Contents.AddChild(box);

        _toggleButton.OnPressed += _ => ToggleRequested?.Invoke(!_enabled);
        _search.OnTextChanged += _ => Repopulate();
        _list.OnItemSelected += args =>
        {
            _selected = (string?) args.ItemList[args.ItemIndex].Metadata;
            _confirmButton.Disabled = _selected == _current;
        };
        _list.OnItemDeselected += _ =>
        {
            _selected = _current;
            _confirmButton.Disabled = true;
        };
        _confirmButton.OnPressed += _ =>
        {
            ReagentSelected?.Invoke(_selected);
            _confirmButton.Disabled = true;
        };
    }

    public void PopulateReagents(IEnumerable<ReagentPrototype> reagents)
    {
        _reagents.Clear();
        foreach (var reagent in reagents)
        {
            _reagents.Add((reagent.ID, reagent.LocalizedName));
        }

        _reagents.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        Repopulate();
    }

    public void SetState(bool enabled, string? reagent)
    {
        _enabled = enabled;
        _current = reagent;
        _selected = reagent;

        _toggleButton.Text = Loc.GetString(enabled ? "plumbing-filter-ui-enabled" : "plumbing-filter-ui-disabled");
        _currentLabel.Text = Loc.GetString("plumbing-filter-ui-current",
            ("reagent", reagent == null ? Loc.GetString("plumbing-filter-ui-none") : ReagentName(reagent)));
        _confirmButton.Disabled = true;
        Repopulate();
    }

    private string ReagentName(string id)
    {
        foreach (var (reagentId, name) in _reagents)
        {
            if (reagentId == id)
                return name;
        }

        return id;
    }

    private void Repopulate()
    {
        _list.Clear();

        // The first entry filters nothing.
        _list.Add(new ItemList.Item(_list)
        {
            Metadata = null,
            Text = Loc.GetString("plumbing-filter-ui-none"),
            Selected = _selected == null,
        });

        var filter = _search.Text.Trim();
        foreach (var (id, name) in _reagents)
        {
            if (filter.Length > 0 && !name.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                continue;

            _list.Add(new ItemList.Item(_list)
            {
                Metadata = id,
                Text = name,
                Selected = id == _selected,
            });
        }
    }
}
