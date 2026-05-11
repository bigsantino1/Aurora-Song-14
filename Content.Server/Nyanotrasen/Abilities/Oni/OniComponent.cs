using Content.Shared.Damage;

namespace Content.Server.Abilities.Oni
{
    [RegisterComponent]
    public sealed partial class OniComponent : Component
    {
        [DataField("modifiers", required: true)]
        public DamageModifierSet MeleeModifiers = default!;

        [DataField("stamDamageBonus")]
        public float StamDamageMultiplier = 1.20f;

        [DataField("gunInaccuracyMultipler")]
        public double GunInaccuracyFactor = 1.0; // 100% multipler aka 2x

        [DataField("meleeSwingSpeedBonus")]
        public float MeleeSwingSpeedMultipler = 0.2F; // 20%
    }
}
