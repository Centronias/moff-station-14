using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Armor;

/// Can be attached to entities with <see cref="Whitelist"/> to enable storage of entities which
/// pass <see cref="SuitStorageAttachableComponent"/> in suitstorage while the attachable entity is worn.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SuitStorageAttachmentComponent : Component
{
    /// <summary>
    /// Whitelist for what entities are allowed in the suit storage slot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist Whitelist = new() { Components = ["Item"] };

    [DataField]
    public TimeSpan AttachDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan DetachDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public LocId CanAttachText = "attachablesuitstorage-attachment-can-be-attached";
}

[Serializable, NetSerializable]
public sealed partial class SuitStorageAttachmentAttachEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class SuitStorageAttachmentDetachEvent : SimpleDoAfterEvent;
