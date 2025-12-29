using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Armor;

/// Allow an entity with <see cref="SuitStorageAttachableComponent"/> to be attached to this entity.
[RegisterComponent, NetworkedComponent]
public sealed partial class SuitStorageAttachableComponent : Component
{
    [DataField]
    public string AttachmentSlotId = "suit-storage-attachment";

    [ViewVariables]
    public ContainerSlot Slot = default!;

    [DataField]
    public float AttachDelayModifier = 1.0f;

    [DataField]
    public LocId CanAttachText = "attachablesuitstorage-attachable-can-attach";

    [DataField]
    public LocId HasAttachmentText = "attachablesuitstorage-attachable-has-attachment";

    [DataField]
    public LocId AttachVerbName = "attachablesuitstorage-attachable-verb-attach";

    [DataField]
    public SpriteSpecifier? AttachIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/insert.svg.192dpi.png"));

    [DataField]
    public LocId DetachVerbName = "attachablesuitstorage-attachable-verb-detach";

    [DataField]
    public SpriteSpecifier? DetachIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/eject.svg.192dpi.png"));
}
