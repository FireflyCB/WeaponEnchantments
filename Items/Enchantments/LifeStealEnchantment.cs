﻿using System.Collections.Generic;
using Terraria.ModLoader;
using WeaponEnchantments.Effects;

namespace WeaponEnchantments.Items.Enchantments {
    public abstract class LifeStealEnchantment : Enchantment {
        public override string CustomTooltip => $"(remainder is saved to prevent always rounding to 0 for low damage weapons)";
        public override float ScalePercent => 0.8f;
        public override bool Max1 => true;
        public override float CapacityCostMultiplier => 2f;
		public override int StrengthGroup => 5;
		public override void GetMyStats() {
            Effects = new() {
                new LifeSteal(@base: EnchantmentStrengthData)
            };
		}
		public override string Artist => "Zorutan";
        public override string Designer => "andro951";
    }
    public class LifeStealEnchantmentBasic : LifeStealEnchantment { }
    public class LifeStealEnchantmentCommon : LifeStealEnchantment { }
    public class LifeStealEnchantmentRare : LifeStealEnchantment { }
    public class LifeStealEnchantmentSuperRare : LifeStealEnchantment { }
    public class LifeStealEnchantmentUltraRare : LifeStealEnchantment { }

}
