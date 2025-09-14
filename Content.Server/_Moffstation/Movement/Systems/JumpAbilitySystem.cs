using Content.Server.PowerCell;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.PowerCell;

namespace Content.Server._Moffstation.Movement.Systems;

/// <summary>
/// This "extension" to <see cref="SharedJumpAbilitySystem"/> causes the jump to consume power as defined by
/// <see cref="PowerCellDrawComponent"/> when the jump is used.
/// </summary>
public sealed partial class JumpAbilityPowerConsumptionSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PowerCellDrawComponent, GravityJumpEvent>(OnGravityJump);
    }

    private void OnGravityJump(Entity<PowerCellDrawComponent> entity, ref GravityJumpEvent args)
    {
        // We're tolerant of the attempt failing because the action _should_ have verified the charge before
        // activating fully.
        _powerCell.TryUseActivatableCharge(entity, entity, user: args.Performer);
    }
}
