using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;

namespace Content.Client;

public sealed partial class GasTankVisualsSystem : VisualizerSystem<GasTankVisualsComponent>
{
    [Dependency] private readonly IReflectionManager _reflect = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedItemSystem _itemSys = default!;

    private static PredefinedGasTankVisualsPrototype _default = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasTankVisualsComponent, GetInhandVisualsEvent>(OnGetHeldVisuals,
            after: [typeof(ItemSystem)]);
        SubscribeLocalEvent<GasTankVisualsComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals,
            after: [typeof(ClientClothingSystem)]);

        _default = _proto.Index<PredefinedGasTankVisualsPrototype>("Default");
    }

    protected override void OnAppearanceChange(EntityUid uid,
        GasTankVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        if (!AppearanceSystem.TryGetData<Color>(uid, GasTankVisualsLayers.Tank, out var tank, args.Component))
        {
            // There's no appearance data yet, so initialize it.
            if (!_proto.TryIndex(component.Visuals, out var visuals))
                visuals = _default;

            tank = visuals.TankColor;
            AppearanceSystem.SetData(uid, GasTankVisualsLayers.Tank, visuals.TankColor);

            if (visuals.MiddleStripeColor is { } middle)
            {
                AppearanceSystem.SetData(uid, GasTankVisualsLayers.StripeMiddle, middle);
            }

            if (visuals.LowerStripeColor is { } lower)
            {
                AppearanceSystem.SetData(uid, GasTankVisualsLayers.StripeLow, lower);
            }
        }

        sprite.LayerSetColor(GasTankVisualsLayers.Tank, tank);

        if (AppearanceSystem.TryGetData<Color>(
                uid,
                GasTankVisualsLayers.StripeMiddle,
                out var middleStripe,
                args.Component
            ))
        {
            sprite.LayerSetVisible(GasTankVisualsLayers.StripeMiddle, true);
            sprite.LayerSetColor(GasTankVisualsLayers.StripeMiddle, middleStripe);
        }
        else
        {
            sprite.LayerSetVisible(GasTankVisualsLayers.StripeMiddle, false);
        }

        if (AppearanceSystem.TryGetData<Color>(
                uid,
                GasTankVisualsLayers.StripeLow,
                out var lowerStripe,
                args.Component
            ))
        {
            sprite.LayerSetVisible(GasTankVisualsLayers.StripeLow, true);
            sprite.LayerSetColor(GasTankVisualsLayers.StripeLow, lowerStripe);
        }
        else
        {
            sprite.LayerSetVisible(GasTankVisualsLayers.StripeLow, false);
        }

        // update clothing & in-hand visuals.
        _itemSys.VisualsChanged(uid);
    }

    private void OnGetHeldVisuals(Entity<GasTankVisualsComponent> entity, ref GetInhandVisualsEvent args)
    {
        OnGetGenericVisuals(entity, ref args.Layers);
    }

    private void OnGetEquipmentVisuals(Entity<GasTankVisualsComponent> entity, ref GetEquipmentVisualsEvent args)
    {
        OnGetGenericVisuals(entity, ref args.Layers);
    }

    private void OnGetGenericVisuals(
        Entity<GasTankVisualsComponent> entity,
        ref List<(string, PrototypeLayerData)> layers)
    {
        if (!TryComp<AppearanceComponent>(entity, out var appearance))
            return;

        if (!AppearanceSystem.TryGetData<Color>(entity, GasTankVisualsLayers.Tank, out var tank, appearance))
            return;

        foreach (var (layerKey, layer) in layers)
        {
            if (!_reflect.TryParseEnumReference(layerKey, out var key))
                continue;

            var hasAppearance = AppearanceSystem.TryGetData<Color>(entity, key, out var color, appearance);

            // We only mess with the visibility of stripes.
            if (key is GasTankVisualsLayers.StripeMiddle or GasTankVisualsLayers.StripeLow)
                layer.Visible = hasAppearance;

            layer.Color = color;
        }
    }
}
