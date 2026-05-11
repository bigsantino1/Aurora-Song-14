using Content.Server.Tools;
using Content.Shared.Damage.Events;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Content.Shared._NF.Weapons.Components;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Containers;

namespace Content.Server.Abilities.Oni
{
    public sealed class OniSystem : EntitySystem
    {
        [Dependency] private readonly SharedGunSystem _gunSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<OniComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
            SubscribeLocalEvent<OniComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
            SubscribeLocalEvent<OniComponent, MeleeHitEvent>(OnOniMeleeHit);
            SubscribeLocalEvent<HeldByOniComponent, MeleeHitEvent>(OnHeldMeleeHit);
            SubscribeLocalEvent<HeldByOniComponent, StaminaMeleeHitEvent>(OnStamHit);
        }

        private void OnEntInserted(EntityUid uid, OniComponent component, EntInsertedIntoContainerMessage args)
        {
            var heldComp = EnsureComp<HeldByOniComponent>(args.Entity);
            heldComp.Holder = uid;

            // Frontier: Oni-friendly "guns" (crusher)
            if (TryComp<GunComponent>(args.Entity, out var gun) && !HasComp<NFOniFriendlyGunComponent>(args.Entity))
            {
                // Frontier: adjust penalty for wielded malus (ensuring it's actually wieldable)
                if (TryComp<GunWieldBonusComponent>(args.Entity, out var bonus) && HasComp<WieldableComponent>(args.Entity))
                {
                    //GunWieldBonus values are stored as negative.
                    heldComp.minAngleAdded = (gun.MinAngle + bonus.MinAngle) * component.GunInaccuracyFactor;
                    heldComp.angleIncreaseAdded = (gun.AngleIncrease + bonus.AngleIncrease) * component.GunInaccuracyFactor;
                    heldComp.maxAngleAdded = (gun.MaxAngle + bonus.MaxAngle) * component.GunInaccuracyFactor;
                }
                else
                {
                    heldComp.minAngleAdded = gun.MinAngle * component.GunInaccuracyFactor;
                    heldComp.angleIncreaseAdded = gun.AngleIncrease * component.GunInaccuracyFactor;
                    heldComp.maxAngleAdded = gun.MaxAngle * component.GunInaccuracyFactor;
                }

                gun.MinAngle += heldComp.minAngleAdded;
                gun.AngleIncrease += heldComp.angleIncreaseAdded;
                gun.MaxAngle += heldComp.maxAngleAdded;
                _gunSystem.RefreshModifiers(args.Entity); // Make sure values propagate to modified values (this also dirties the gun for us)
                // End Frontier
            }

            if (TryComp<MeleeWeaponComponent>(args.Entity, out var meleeComp))
            {
                heldComp.attackRateAdded = meleeComp.AttackRate * component.MeleeSwingSpeedMultipler;
                meleeComp.AttackRate += heldComp.attackRateAdded;
            }
        }

        private void OnEntRemoved(EntityUid uid, OniComponent component, EntRemovedFromContainerMessage args)
        {
            if (!TryComp<HeldByOniComponent>(args.Entity, out var heldComp))
                return;

            // Frontier: angle manipulation stored in HeldByOniComponent
            // Frontier: Oni-friendly "guns" (crusher)
            if (TryComp<GunComponent>(args.Entity, out var gun)
                && !HasComp<NFOniFriendlyGunComponent>(args.Entity))
            {
                gun.MinAngle -= heldComp.minAngleAdded;
                gun.AngleIncrease -= heldComp.angleIncreaseAdded;
                gun.MaxAngle -= heldComp.maxAngleAdded;
                _gunSystem.RefreshModifiers(args.Entity); // Make sure values propagate to modified values (this also dirties the gun for us)
            }
            // End Frontier

            if (TryComp<MeleeWeaponComponent>(args.Entity, out var meleeComp))
                meleeComp.AttackRate -= heldComp.attackRateAdded;

            RemComp<HeldByOniComponent>(args.Entity);
        }

        private void OnOniMeleeHit(EntityUid uid, OniComponent component, MeleeHitEvent args)
        {
            args.ModifiersList.Add(component.MeleeModifiers);
        }

        private void OnHeldMeleeHit(EntityUid uid, HeldByOniComponent component, MeleeHitEvent args)
        {
            if (!TryComp<OniComponent>(component.Holder, out var oni))
                return;

            args.ModifiersList.Add(oni.MeleeModifiers);
        }

        private void OnStamHit(EntityUid uid, HeldByOniComponent component, StaminaMeleeHitEvent args)
        {
            if (!TryComp<OniComponent>(component.Holder, out var oni))
                return;

            args.Multiplier *= oni.StamDamageMultiplier;
        }
    }
}
