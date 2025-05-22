using Content.Shared.Atmos;
using Robust.Shared.Prototypes;

namespace Content.Shared;

[RegisterComponent]
public sealed partial class GasTankVisualsComponent : Component
{
    [DataField(required: true)]
    public ProtoId<PredefinedGasTankVisualsPrototype> Visuals;
}

public enum GasTankVisualsLayers : byte
{
    Tank,
    Hardware,
    StripeMiddle,
    StripeLow,
}

[Prototype]
public sealed partial class PredefinedGasTankVisualsPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)] public Color TankColor;
    [DataField] public Color? MiddleStripeColor = null;
    [DataField] public Color? LowerStripeColor = null;
}
