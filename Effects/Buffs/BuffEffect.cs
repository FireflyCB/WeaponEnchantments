﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WeaponEnchantments.Common.Configs;
using WeaponEnchantments.Common.Utility;

namespace WeaponEnchantments.Effects {
    public class BuffEffect : EnchantmentEffect, IPassiveEffect {
        public static string GetBuffName(int id) { // C# is crying
            if (id < BuffID.Count) {
                BuffID buffID = new();
                return buffID.GetType().GetFields().Where(field => field.FieldType == typeof(int) && (int)field.GetValue(buffID) == id).First().Name;
            }
            return ModContent.GetModBuff(id).Name;
        }

        public BuffEffect(int debuffID, bool isQuiet = true) : base() {
            AppliedBuffID = debuffID;
            IsQuiet = isQuiet;
            BuffName = GetBuffName(AppliedBuffID);
        }

        private int AppliedBuffID { get; set; }
        private bool IsQuiet { get; set; }
        private string BuffName;

        // Could move into constructor?
        public override sealed string DisplayName => $"Passive {GetBuffName(AppliedBuffID)}";
        public override sealed string Tooltip => $"Passively grants {GetBuffName(AppliedBuffID)}";

        public void PostUpdateMiscEffects(WEPlayer player) {
            player.Player.AddBuff(AppliedBuffID, 1, IsQuiet);
        }
    }
}