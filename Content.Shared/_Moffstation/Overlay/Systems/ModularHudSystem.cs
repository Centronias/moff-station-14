using System.Linq;
using Content.Shared._Moffstation.Extensions;
using Content.Shared._Moffstation.Overlay.Components;
using Content.Shared.Chemistry;
using Content.Shared.Contraband;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Flash;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Overlay.Systems;

public sealed partial class ModularHudSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModularHudComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ModularHudComponent, ComponentRemove>(RefreshEffectsForContainedModules);
        SubscribeLocalEvent<ModularHudComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ModularHudComponent, GotUnequippedEvent>(OnGotUneqipped);
        SubscribeLocalEvent<ModularHudComponent, EntInsertedIntoContainerMessage>(OnContainerModifiedMessage);
        SubscribeLocalEvent<ModularHudComponent, EntRemovedFromContainerMessage>(OnContainerModifiedMessage);
        SubscribeLocalEvent<ModularHudComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ModularHudComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ModularHudComponent, HudModulesRemovalDoAfterEvent>(OnHudModulesRemovalDoAfter);
        // TODO CENT Maybe add a verb for discoverability of removing modules.

        // Relays for module events.
        SubscribeRelaysForEffectEvents<GetContrabandDetailsEvent>();
        SubscribeRelaysForEffectEvents<SolutionScanEvent>();
        SubscribeRelaysForEffectEvents<GetEyeProtectionEvent>();
        SubscribeRelaysForEffectEvents<SeeIdentityAttemptEvent>();
        SubscribeRelaysForEffectEvents<FlashAttemptEvent>();
        SubscribeRelaysForEffectEvents<GetBlurEvent>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<ShowJobIconsComponent>>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<ShowHealthBarsComponent>>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<ShowHealthIconsComponent>>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<ShowHungerIconsComponent>>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<ShowThirstIconsComponent>>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<ShowMindShieldIconsComponent>>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<ShowSyndicateIconsComponent>>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<ShowCriminalRecordIconsComponent>>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<BlackAndWhiteOverlayComponent>>();
        SubscribeRelaysForEffectEvents<RefreshEquipmentHudEvent<NoirOverlayComponent>>();

        // Relays `TArgs` to all contained modules.
        void SubscribeRelaysForEffectEvents<TArgs>() where TArgs : notnull => Subs.SubscribeWithRelay(
            delegate(Entity<ModularHudComponent> entity, ref TArgs args)
            {
                foreach (var module in GetModules(entity))
                {
                    RaiseLocalEvent(module, ref args);
                }
            }
        );
    }

    /// Yields all the modules contained in the given HUD.
    public IEnumerable<Entity<ModularHudModuleComponent>> GetModules(Entity<ModularHudComponent> entity)
    {
        foreach (var moduleEnt in entity.Comp.ModuleContainer.ContainedEntities)
        {
            if (TryComp<ModularHudModuleComponent>(moduleEnt, out var moduleComp))
                yield return (moduleEnt, moduleComp);
        }
    }

    private void OnStartup(Entity<ModularHudComponent> entity, ref ComponentStartup args)
    {
        entity.Comp.ModuleContainer = _container.EnsureContainer<Container>(entity, entity.Comp.ModuleContainerId);
        RefreshEffectsForContainedModules(entity, ref args);
    }

    private void OnInteractUsing(Entity<ModularHudComponent> entity, ref InteractUsingEvent args)
    {
        // Module insertion
        if (HasComp<ModularHudModuleComponent>(args.Used))
        {
            if (!_container.Insert(args.Used, entity.Comp.ModuleContainer))
            {
                this.AssertOrLogError($"Failed to insert {ToPrettyString(args.Used)} into {ToPrettyString(entity)}");
            }
        }

        // Module removal
        if (GetModules(entity).Any())
        {
            _tool.UseTool(
                args.Used,
                args.User,
                entity.Owner,
                entity.Comp.ModuleRemovalDelay,
                [entity.Comp.ModuleExtractionMethod],
                new HudModulesRemovalDoAfterEvent(),
                out _
            );
        }
        else
        {
            if (_tool.HasQuality(args.Used, entity.Comp.ModuleExtractionMethod))
            {
                _popup.PopupPredictedCursor(Loc.GetString(entity.Comp.NoModulesToRemovePopupText), args.User);
            }
        }
    }

    /// Describes what, if anything, is in this HUD.
    private void OnExamined(Entity<ModularHudComponent> entity, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(ModularHudComponent)))
        {
            using var modules = GetModules(entity).GetEnumerator();
            if (!modules.MoveNext())
            {
                args.PushMarkup(Loc.GetString(entity.Comp.NoModulesExamineText));
                return;
            }

            args.PushMarkup(Loc.GetString(entity.Comp.HeaderExamineText));
            do
            {
                args.PushMarkup(Loc.GetString(entity.Comp.ModuleItemExamineText, ("module", modules.Current)));
            } while (modules.MoveNext());
        }
    }

    /// Removes all modules from this HUD when the doafter is completed.
    private void OnHudModulesRemovalDoAfter(Entity<ModularHudComponent> entity, ref HudModulesRemovalDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        foreach (var module in GetModules(entity).ToList())
        {
            _container.Remove(module.Owner, entity.Comp.ModuleContainer);
            _hands.TryPickup(args.User, module);
        }
    }

    /// Refresh the effects provided by the module added/removed.
    private void OnContainerModifiedMessage<TArgs>(
        Entity<ModularHudComponent> entity,
        ref TArgs args
    ) where TArgs : ContainerModifiedMessage
    {
        if (args.Container.ID != entity.Comp.ModuleContainerId ||
            !TryComp<ModularHudModuleComponent>(args.Entity, out var moduleComp))
            return;

        RefreshEffectsForModules([(args.Entity, moduleComp)]);
    }

    private void OnGotEquipped(Entity<ModularHudComponent> entity, ref GotEquippedEvent args)
    {
        RefreshEffectsForWearerForContainedModules(entity, args.Equipee);
    }

    private void OnGotUneqipped(Entity<ModularHudComponent> entity, ref GotUnequippedEvent args)
    {
        RefreshEffectsForWearerForContainedModules(entity, args.Equipee);
    }

    private void RefreshEffectsForWearerForContainedModules(Entity<ModularHudComponent> entity, EntityUid equippee)
    {
        _blurryVision.UpdateBlurMagnitude(equippee);
        RaiseLocalEvent(equippee, new FlashImmunityChangedEvent(_flash.IsFlashImmune(equippee)));
        RefreshEffectsForModules(GetModules(entity));
    }

    /// Note that this relies on accessing the modules from the given entity's container, so if a module was
    /// added/removed, this is not the right method to use.
    private void RefreshEffectsForContainedModules<TArgs>(Entity<ModularHudComponent> entity, ref TArgs args)
    {
        RefreshEffectsForModules(GetModules(entity));
    }

    [Dependency] private readonly BlurryVisionSystem _blurryVision = default!;

    private void RefreshEffectsForModules(IEnumerable<Entity<ModularHudModuleComponent>> modules)
    {
        var ev = new EquipmentHudNeedsRefreshEvent();
        foreach (var module in modules)
        {
            RaiseLocalEvent(module, ref ev);
        }
    }

    /// This doafter event is raised when the doafter to remove the HUD's modules is complete.
    [Serializable, NetSerializable]
    private sealed partial class HudModulesRemovalDoAfterEvent : SimpleDoAfterEvent;
}
