using Content.Shared.Armor;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Armor;

/// Implements <see cref="SuitStorageAttachableComponent"/> & <see cref="SuitStorageAttachmentComponent"/>, allowing
/// customization of equipment to allow suit storage on clothing which otherwise would not allow it.
public sealed partial class SuitStorageAttachmentSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SuitStorageAttachableComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SuitStorageAttachableComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<SuitStorageAttachableComponent, SuitStorageAttachmentAttachEvent>(OnAttachDoAfter);
        SubscribeLocalEvent<SuitStorageAttachableComponent, SuitStorageAttachmentDetachEvent>(OnDetatchDoAfter);
        SubscribeLocalEvent<SuitStorageAttachableComponent, ExaminedEvent>(AttachableOnExamined);
        SubscribeLocalEvent<SuitStorageAttachableComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<SuitStorageAttachableComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<SuitStorageAttachableComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromContainer);

        SubscribeLocalEvent<SuitStorageAttachmentComponent, ExaminedEvent>(AttachmentOnExamined);
    }
/// <summary>
/// Check Whether or not the "Item" attached to the "Entity" blocks the Suit Storage Slot
/// </summary>
/// <param name="entity"></param>
/// <param name="item"></param>
/// <returns>True If The Entity that is Attached to the Component is NOT on the Whitelist of the Attachment Component</returns>
    public bool HasAttachmentBlockSuitStorage(Entity<SuitStorageAttachableComponent?> entity, EntityUid item )
    {
        return Resolve(entity, ref entity.Comp) &&
               entity.Comp.Slot.ContainedEntity is { } attachment &&
               TryComp<SuitStorageAttachmentComponent>(attachment, out var attachmentComp) &&
               !_whitelist.IsWhitelistFailOrNull(attachmentComp.Whitelist, item);
    }
/// <summary>
///  Attempts to Fetch the Attached Entity
/// </summary>
/// <param name="ent"></param>
/// <returns>Returns the Entity if there is one attached, otherwise returns null</returns>
    private Entity<SuitStorageAttachmentComponent>? TryGetSuitStorageAttachment(Entity<SuitStorageAttachableComponent> ent)
    {
        if (ent.Comp.Slot.ContainedEntity is { } attachment &&
            TryComp<SuitStorageAttachmentComponent>(attachment, out var comp))
            return (attachment, comp);

        return null;
    }
/// <summary>
/// Ensures That the Attachment Slot is a container and sends a debugging message if Back storage and Attachment are present.
/// </summary>
    private void OnInit(Entity<SuitStorageAttachableComponent> ent, ref ComponentInit args)
    {
        DebugTools.Assert(
            !HasComp<AllowSuitStorageComponent>(ent),
            $"Entity {ToPrettyString(ent)} has both {nameof(AllowSuitStorageComponent)} and {nameof(SuitStorageAttachableComponent)}. Entities with the former should not have the latter."
        );
        ent.Comp.Slot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.AttachmentSlotId);
    }
/// <summary>
/// Adds a text Informing players what item is attached or if none is attached that, one can be .
/// </summary>
    private void AttachableOnExamined(Entity<SuitStorageAttachableComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(TryGetSuitStorageAttachment(ent) is { } attachment
                ? Loc.GetString(ent.Comp.HasAttachmentText, ("attachment", attachment))
                : Loc.GetString(ent.Comp.CanAttachText)
        );
    }
/// <summary>
/// Lets Players Know The Item is Attachable
/// </summary>

    private void AttachmentOnExamined(Entity<SuitStorageAttachmentComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.CanAttachText));
    }

/// <summary>
/// Populates the Verbs menu
/// </summary>
    private void OnGetVerbs(Entity<SuitStorageAttachableComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanComplexInteract)
            return;

        var user = args.User;
        if (TryGetSuitStorageAttachment(ent) is { } attachment)
        {
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString(ent.Comp.DetachVerbName),
                Icon = ent.Comp.DetachIcon,
                Act = () => _doAfter.TryStartDoAfter(
                    new DoAfterArgs(
                        EntityManager,
                        user,
                        attachment.Comp.AttachDelay * ent.Comp.AttachDelayModifier,
                        new SuitStorageAttachmentDetachEvent(),
                        ent,
                        target: ent
                    )
                    {
                        BreakOnDamage = true,
                        BreakOnMove = true,
                        NeedHand = true,
                        RequireCanInteract = true,
                    }
                ),
            });
        }
        else if (TryComp<SuitStorageAttachmentComponent>(args.Using, out var attachmentComp))
        {
            var used = args.Using;
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString(ent.Comp.AttachVerbName),
                Icon = ent.Comp.AttachIcon,
                Disabled = HasComp<AllowSuitStorageComponent>(ent),
                Act = () => _doAfter.TryStartDoAfter(
                    new DoAfterArgs(
                        EntityManager,
                        user,
                        attachmentComp.AttachDelay * ent.Comp.AttachDelayModifier,
                        new SuitStorageAttachmentAttachEvent(),
                        ent,
                        target: ent,
                        used: used
                    )
                    {
                        BreakOnDamage = true,
                        BreakOnMove = true,
                        NeedHand = true,
                        BreakOnDropItem = true,
                        BreakOnHandChange = true,
                        RequireCanInteract = true,
                    }
                ),
            });
        }
    }

/// <summary>
/// Pops out the Item If the Entity with the Component is destroyed
/// </summary>
    private void OnDestruction(Entity<SuitStorageAttachableComponent> ent, ref DestructionEventArgs args)
    {
        _container.EmptyContainer(ent.Comp.Slot, destination: Transform(ent).Coordinates);
    }

    private void OnInsertAttempt(Entity<SuitStorageAttachableComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled ||
            args.Container != ent.Comp.Slot || // Don't do anything if the container isn't the one we manage.
            HasComp<SuitStorageAttachmentComponent>(args.EntityUid))
            return;

        args.Cancel();
    }

    private void OnAttachDoAfter(Entity<SuitStorageAttachableComponent> ent, ref SuitStorageAttachmentAttachEvent args)
    {
        if (args.Cancelled ||
            args.Used is not { } used)
            return;

        _container.Insert(used, ent.Comp.Slot);
        args.Handled = true;
    }

    private void OnDetatchDoAfter(Entity<SuitStorageAttachableComponent> ent, ref SuitStorageAttachmentDetachEvent args)
    {
        if (args.Cancelled ||
            ent.Comp.Slot.ContainedEntity is not { } attachment)
            return;

        _container.Remove(attachment, ent.Comp.Slot);
        _hands.TryPickupAnyHand(args.User, attachment);
        args.Handled = true;
    }

    private void OnEntRemovedFromContainer(
        Entity<SuitStorageAttachableComponent> ent,
        ref EntRemovedFromContainerMessage args
    )
    {
        if (!HasComp<SuitStorageAttachmentComponent>(args.Entity))
            return;

        _inventory.TryUnequip(Transform(ent).ParentUid, "suitstorage", force: true, checkDoafter: false);
    }
}
