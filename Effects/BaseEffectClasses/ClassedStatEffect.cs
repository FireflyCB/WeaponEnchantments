using Terraria.ModLoader;
using WeaponEnchantments.Common;
using WeaponEnchantments.Common.Utility;
using static WeaponEnchantments.WEPlayer;

namespace WeaponEnchantments.Effects {
    public abstract class ClassedStatEffect : StatEffect {
        protected ClassedStatEffect(EStatModifier sm) {
            EStatModifier = sm;
        }

        protected ClassedStatEffect(float additive = 0f, float multiplicative = 1f, float flat = 0f, float @base = 0f, DamageClass dc = null) : base(additive, multiplicative, flat, @base) {
            damageClass = dc != null ? dc : DamageClass.Generic;
		}

        public DamageClass damageClass { get; set; }
        public override string DisplayName => $"{damageClass.S()} {base.DisplayName}";
    }
}
