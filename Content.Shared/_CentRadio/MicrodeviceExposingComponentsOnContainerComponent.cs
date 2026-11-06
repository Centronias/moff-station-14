using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CentRadio;

/// When an entity with this component is inserted into a container identified by
/// <see cref="MicrodeviceContainerComponent.ContainerIds"/>, adds <see cref="Components"/> to the container. These
/// components are removed when the entity is removed from the container.
[RegisterComponent, NetworkedComponent]
public sealed partial class MicrodeviceExposingComponentsOnContainerComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Components = default!;
}
