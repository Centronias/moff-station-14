using Content.Shared._Moffstation.Overlay.Systems;
using Content.Shared.Inventory;
using Content.Shared.Tools;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Overlay.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(ModularHudSystem))]
public sealed partial class ModularHudComponent : Component
{
    [DataField]
    public SlotFlags ActiveSlots = SlotFlags.WITHOUT_POCKET;

    [DataField(required: true)]
    public string ModuleContainerId = default!;

    [ViewVariables]
    public Container ModuleContainer = default!;

    [DataField(required: true)]
    public int ModuleSlots;

    [DataField(required: true)]
    public ProtoId<ToolQualityPrototype> ModuleExtractionMethod;

    [DataField(required: true)]
    public TimeSpan ModuleRemovalDelay;

    [DataField] public LocId NoModulesToRemovePopupText = "modularhud-no-modules-to-remove";
    [DataField] public LocId NoModulesExamineText = "modularhud-examine-no-modules";
    [DataField] public LocId HeaderExamineText = "modularhud-examine-modules-header";
    [DataField] public LocId ModuleItemExamineText = "modularhud-examine-module-item";
}

[RegisterComponent, NetworkedComponent, Access(typeof(ModularHudSystem))]
public sealed partial class ModularHudModuleComponent : Component
{
    /// The names of HUD components on this component's entity. Note that this DOES NOT cause these components to be
    /// added to the entity, this is only used to know which existing components the modular HUD system should deal with.
    [DataField(required: true)]
    public List<string> HudComponentTypes = default!;
}

/// This event, when raised on an entity with a HUD component, will refresh the HUD's information. This just triggers
/// <see cref="Content.Client.Overlays.EquipmentHudSystem.RefreshOverlay"/>.
[ByRefEvent]
public record struct EquipmentHudNeedsRefreshEvent;
