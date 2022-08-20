﻿using Terraria.ModLoader;
using WeaponEnchantments.Common;
using WeaponEnchantments.Common.Utility;
using static WeaponEnchantments.WEPlayer;

namespace WeaponEnchantments.Effects {
    public abstract class StatEffect : EnchantmentEffect {
        protected StatEffect(EStatModifier sm, bool playerStatOnWeapon = false) {
            EStatModifier = sm;
        }

        protected StatEffect(float additive = 0f, float multiplicative = 1f, float flat = 0f, float @base = 0f) {
            EStatModifier = new EStatModifier(statType, additive, multiplicative, flat, @base);
		}
        /*protected StatEffect(float[] additive = null, float[] multiplicative = null, float[] flat = null, float[] @base = null) {
            EStatModifier = new EStatModifier(statType, additive, multiplicative, flat, @base);
        }*/
        public StatEffect(DifficultyStrength additive, DifficultyStrength multiplicative, DifficultyStrength flat, DifficultyStrength @base) {
            EStatModifier = new EStatModifier(statType, additive, multiplicative, flat, @base);
        }
        public EStatModifier EStatModifier { set; get; }
        public override float EffectStrength => EStatModifier.Strength;
		public override float CombinedMultiplier {
            get => EStatModifier.EfficiencyMultiplier; 
            protected set => EStatModifier.EfficiencyMultiplier = value;
        }

        public abstract EnchantmentStat statType { get; }

        //protected virtual string modifierToString() {
        //    string final = "";
        //    float mult = EStatModifier.Multiplicative + EStatModifier.Additive - 2;
        //    float flats = EStatModifier.Base * mult + EStatModifier.Flat;

        //    if (flats > 0f) {
        //        final += $"{s(flats)}{flats}";
        //    }

        //    if (mult > 0f) {
        //        if (final != "") final += ' ';
        //        final += $"{s(mult)}{mult.Percent()}%";
        //    }

        //    return final;
        //}

        public override string Tooltip => $"{EStatModifier.SmartTooltip} {DisplayName}";
    }
}
