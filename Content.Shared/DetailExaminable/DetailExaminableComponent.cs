using Content.Shared.Humanoid; //HN: замена Lua EnumERPStatus на Lust Erp
using Content.Shared.Preferences;
using Robust.Shared.GameStates;

namespace Content.Shared.DetailExaminable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DetailExaminableComponent : Component
{
    [DataField, AutoNetworkedField] // Erida-Edit | Removed: "required: true"
    public string Content = string.Empty;

    //HN: замена Lua EnumERPStatus на Lust Erp
    [DataField("Erp", required: true), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public Erp Erp = Erp.Ask;

    public string GetERPStatusName()
    {
        return Erp switch
        {
            Erp.Yes => Loc.GetString("humanoid-profile-editor-erp-yes-text"),
            Erp.Ask => Loc.GetString("humanoid-profile-editor-erp-ask-text"),
            _ => Loc.GetString("humanoid-profile-editor-erp-no-text"),
        };
    }
    // Erida-Start
    [DataField, AutoNetworkedField]
    public string CharacterContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string OOCContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string TagsContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string LinksContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string GreenContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string YellowContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string RedContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string NSFWContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string NSFWOOCContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string NSFWLinksContent { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string NSFWTagsContent { get; set; } = string.Empty;

    public void SetProfile(HumanoidCharacterProfile profile)
    {
        Content = profile.FlavorText;
        CharacterContent = profile.CharacterFlavorText;
        OOCContent = profile.OOCFlavorText;
        TagsContent = profile.TagsFlavorText;
        LinksContent = profile.LinksFlavorText;
        GreenContent = profile.GreenFlavorText;
        YellowContent = profile.YellowFlavorText;
        RedContent = profile.RedFlavorText;
        NSFWContent = profile.NSFWFlavorText;
        NSFWOOCContent = profile.NSFWOOCFlavorText;
        NSFWLinksContent = profile.NSFWLinksFlavorText;
        NSFWTagsContent = profile.NSFWTagsFlavorText;
        Erp = profile.Erp; //HN: Lust Erp
    }
    // Erida-End
}
