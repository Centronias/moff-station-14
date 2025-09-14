using Content.Shared.EntityEffects;
using Content.Shared.PowerCell;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.EntityEffects.EffectConditions;

/// <summary>
/// A conditions which passes if the subject entity: does not have a <see cref="PowerCellDrawComponent"/>, or has
/// adequate power to be activated, as defined by that component.
/// </summary>
[UsedImplicitly]
public sealed partial class HasActivatableCharge : EntityEffectCondition
{
    public override bool Condition(EntityEffectBaseArgs args)
    {
        return args.EntityManager.System<SharedPowerCellSystem>().HasActivatableCharge(args.TargetEntity);
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("effect-condition-guidebook-has-activatable-charge");
    }
}
