using Content.Client.Clothing;
using Content.Client.DisplacementMap;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.DisplacementMap;
using Content.Shared.DirtVisuals;
using Content.Shared.Foldable;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client.DirtVisuals;

// Порт с ss14-wega (wega-team/ss14-wega), Content.Client/_Wega/Dirt/DirtVisualsSystem.cs
// Адаптация под HN: убрана ветка ToggleableSpriteClothingComponent (в этой сборке такого компонента нет).
public sealed partial class DirtVisualsSystem : EntitySystem
{
    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private DisplacementMapSystem _displacement = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private ClientClothingSystem _clothing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DirtableComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<DirtableComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
    }

    private void OnHandleState(EntityUid uid, DirtableComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not DirtableComponentState state)
            return;

        comp.CurrentDirtLevel = state.CurrentDirtLevel;
        comp.DirtColor = state.DirtColor;
        comp.CurrentWetness = state.CurrentWetness;

        comp.DirtyReagents.Clear();
        foreach (var id in state.DirtyReagents)
            comp.DirtyReagents.Add(new ProtoId<ReagentPrototype>(id));

        UpdateDirtVisuals(uid, comp);
    }

    private void UpdateDirtVisuals(EntityUid uid, DirtableComponent comp)
    {
        if (!HasComp<SpriteComponent>(uid))
            return;

        var isFolded = false;
        if (HasComp<AppearanceComponent>(uid) && _appearance.TryGetData<bool>(uid, FoldableSystem.FoldedVisuals.State, out var folded))
            isFolded = folded;

        var layerKey = $"dirt_{uid}";
        var dirtState = isFolded && !string.IsNullOrEmpty(comp.FoldingDirtState)
            ? comp.FoldingDirtState
            : comp.DirtState;

        if (comp.IsDirty)
        {
            if (!_sprite.LayerMapTryGet(uid, layerKey, out var layerIndex, false))
            {
                layerIndex = _sprite.AddLayer(uid,
                    new SpriteSpecifier.Rsi(
                    new ResPath(comp.DirtSpritePath),
                    dirtState
                ));
                _sprite.LayerMapSet(uid, layerKey, layerIndex);
            }

            _sprite.LayerSetVisible(uid, layerIndex, true);
            _sprite.LayerSetColor(uid, layerIndex, comp.DirtColor);

            _sprite.LayerSetRsiState(uid, layerIndex, dirtState);
        }
        else if (_sprite.LayerMapTryGet(uid, layerKey, out var layerIndex, false))
        {
            _sprite.LayerSetVisible(uid, layerIndex, false);
        }

        if (TryComp(Transform(uid).ParentUid, out InventoryComponent? inventory))
            _clothing.InitClothing(Transform(uid).ParentUid, inventory);

        // HN: мокрая одежда темнеет/синеет.
        _sprite.SetColor(uid, GetWetTint(comp.CurrentWetness));
    }

    // HN: оттенок мокрости — от белого (сухо) к холодному синему.
    private static Color GetWetTint(float wetness)
    {
        var t = Math.Clamp(wetness / SharedDirtSystem.MaxWetness, 0f, 1f);
        return Color.InterpolateBetween(Color.White, Color.FromHex("#5a7ea8"), t * 0.6f);
    }

    // HN: тонируем надетые слои одежды — их цвета лежат не на предмете, а на спрайте носителя.
    private void OnEquipmentVisualsUpdated(EntityUid uid, DirtableComponent comp, EquipmentVisualsUpdatedEvent args)
    {
        if (comp.CurrentWetness <= 0f)
            return;

        if (!TryComp<SpriteComponent>(args.Equipee, out var sprite))
            return;

        var tint = GetWetTint(comp.CurrentWetness);
        foreach (var key in args.RevealedLayers)
        {
            if (key.StartsWith("dirt_"))
                continue;

            if (!_sprite.LayerMapTryGet((args.Equipee, sprite), key, out var index, false))
                continue;

            if (sprite[index] is not SpriteComponent.Layer layer)
                continue;

            _sprite.LayerSetColor((args.Equipee, sprite), index, layer.Color * tint);
        }
    }

    public void TryAddEquipmentDirtLayer(
        EntityUid equipee,
        EntityUid equipment,
        InventoryComponent inventory,
        SpriteComponent sprite,
        ClothingComponent clothingComponent,
        SlotDefinition slotDef,
        bool slotLayerExists,
        ref int index,
        DisplacementData? displacementData,
        HashSet<string> revealedLayers)
    {
        if (!TryComp<DirtableComponent>(equipment, out var dirtable) || !dirtable.IsDirty
           || revealedLayers.Contains($"dirt_{equipment}"))
            return;

        var dirtRsi = _cache.GetResource<RSIResource>(
            SpriteSpecifierSerializer.TextureRoot / dirtable.DirtSpritePath).RSI;

        var state = dirtable.EquippedDirtState;
        if (!string.IsNullOrEmpty(clothingComponent.EquippedPrefix))
            state = $"{clothingComponent.EquippedPrefix}-{state}";
        if (inventory.SpeciesId != null && dirtRsi.TryGetState($"{state}-{inventory.SpeciesId}", out _))
            state = $"{state}-{inventory.SpeciesId}";

        if (!dirtRsi.TryGetState(state, out _))
            return;

        var dirtLayer = new PrototypeLayerData
        {
            RsiPath = dirtable.DirtSpritePath,
            State = state,
            Color = dirtable.DirtColor
        };

        var dirtKey = $"dirt_{equipment}";
        if (slotLayerExists)
        {
            index++;
            _sprite.AddBlankLayer((equipee, sprite), index);
            _sprite.LayerMapSet((equipee, sprite), dirtKey, index);
        }
        else
        {
            index = _sprite.LayerMapReserve((equipee, sprite), dirtKey);
        }

        if (sprite[index] is SpriteComponent.Layer layer)
        {
            _sprite.LayerSetData((equipee, sprite), index, dirtLayer);
            _sprite.LayerSetOffset(layer, layer.Offset + slotDef.Offset);
            revealedLayers.Add(dirtKey);

            if (displacementData != null)
            {
                if (_displacement.TryAddDisplacement(
                    displacementData,
                    (equipee, sprite),
                    index,
                    dirtKey,
                    out var displacementKey))
                {
                    revealedLayers.Add(displacementKey);
                    index++;
                }
            }
        }
    }
}
