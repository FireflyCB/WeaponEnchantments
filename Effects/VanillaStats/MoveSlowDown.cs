﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using WeaponEnchantments.Common;
using static WeaponEnchantments.WEPlayer;

namespace WeaponEnchantments.Effects {
    public class MoveSlowDown : StatEffect, IVanillaStat {
        public MoveSlowDown(DifficultyStrength additive = null, DifficultyStrength multiplicative = null, DifficultyStrength flat = null, DifficultyStrength @base = null) : base(additive, multiplicative, flat, @base) {

        }
        public override EnchantmentStat statType => EnchantmentStat.MoveSlowdown;
        public override string DisplayName { get; } = "Movement Slowdown";
    }
}
