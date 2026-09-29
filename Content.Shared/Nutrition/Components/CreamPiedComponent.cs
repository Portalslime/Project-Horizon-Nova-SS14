using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Serialization;

namespace Content.Shared.Nutrition.Components
{
    [Access(typeof(SharedCreamPieSystem))]
    [RegisterComponent]
    public sealed partial class CreamPiedComponent : Component
    {
        [ViewVariables]
        public bool CreamPied { get; set; } = false;

        [ViewVariables]
        public Color Color { get; set; } = Color.White;
    }

    [Serializable, NetSerializable]
    public enum CreamPiedVisuals
    {
        Creamed,
        Color,
    }
}
