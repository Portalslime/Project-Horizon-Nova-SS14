using System.Numerics;
using Content.Client.Eui;
using Content.Shared._HorizonNova.Lactation;
using Content.Shared.Eui;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client._HorizonNova.Lactation;

/// <summary>
/// HN: клиентское окно «Изменение груди» (открывается админ-вербом).
/// </summary>
public sealed class LactationAdminEui : BaseEui
{
    private static readonly string[] SizeKeys =
    {
        "lactation-admin-breast-small",
        "lactation-admin-breast-default",
        "lactation-admin-breast-medium",
        "lactation-admin-breast-big",
        "lactation-admin-breast-superbig",
    };

    private readonly DefaultWindow _window;
    private readonly Label _targetLabel;
    private readonly Label _speciesLabel;
    private readonly Label _sizeLabel;
    private readonly Label _denyLabel;
    private readonly BoxContainer _options;
    private readonly Label _noteLabel;

    public LactationAdminEui()
    {
        _window = new DefaultWindow
        {
            Title = Loc.GetString("lactation-admin-eui-title"),
            MinSize = new Vector2(380, 220),
        };
        _window.OnClose += () => SendMessage(new CloseEuiMessage());

        var content = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(8),
        };
        _window.Contents.AddChild(content);

        _targetLabel = new Label();
        _speciesLabel = new Label();
        _sizeLabel = new Label();
        _denyLabel = new Label
        {
            FontColorOverride = Color.Red,
            Visible = false,
        };
        _options = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        _noteLabel = new Label
        {
            FontColorOverride = Color.Gray,
            Visible = false,
        };

        content.AddChild(_targetLabel);
        content.AddChild(_speciesLabel);
        content.AddChild(_sizeLabel);
        content.AddChild(_denyLabel);
        content.AddChild(_options);
        content.AddChild(_noteLabel);
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not LactationAdminEuiState cast)
            return;

        _targetLabel.Text = Loc.GetString("lactation-admin-eui-target", ("name", cast.TargetName));
        _speciesLabel.Text = Loc.GetString("lactation-admin-eui-species", ("species", cast.SpeciesName));
        _sizeLabel.Text = cast.CanChange
            ? Loc.GetString("lactation-admin-eui-current", ("size", cast.CurrentSize))
            : string.Empty;

        _denyLabel.Visible = !cast.CanChange;
        _denyLabel.Text = !cast.CanChange && cast.DenyKey != null
            ? Loc.GetString(cast.DenyKey)
            : string.Empty;

        _noteLabel.Visible = cast.CanChange && !cast.HasMarkings;
        _noteLabel.Text = Loc.GetString("lactation-admin-eui-note-no-sprites");

        _options.RemoveAllChildren();
        _options.Visible = cast.CanChange;

        if (!cast.CanChange)
            return;

        for (var i = 0; i < SizeKeys.Length; i++)
        {
            var size = i + 1;
            var button = new Button
            {
                Text = Loc.GetString(SizeKeys[i]),
                Disabled = cast.CurrentSize == size,
            };
            button.OnPressed += _ => SendMessage(new LactationAdminSetBreastMessage(size));
            _options.AddChild(button);
        }

        var remove = new Button
        {
            Text = Loc.GetString("lactation-admin-breast-remove"),
            Disabled = cast.CurrentSize == 0,
        };
        remove.OnPressed += _ => SendMessage(new LactationAdminSetBreastMessage(0));
        _options.AddChild(remove);
    }
}
