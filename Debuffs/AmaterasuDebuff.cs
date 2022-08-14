﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WeaponEnchantments.Debuffs
{
    public class AmaterasuDebuff : ModBuff
    {
        static int[] notImmuneBuffs = new int[] { ModContent.BuffType<AmaterasuDebuff>(), BuffID.OnFire, BuffID.CursedInferno, BuffID.ShadowFlame, BuffID.OnFire3, BuffID.Oiled};
        public override void SetStaticDefaults() {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
        public override void Update(NPC npc, ref int buffIndex) {
            ForceUpdate(npc);
        }
        public static void ForceUpdate(NPC npc) {
            //Make not immune to the other buffs that WorldAblaze applies
            foreach (int notImmuneBuff in notImmuneBuffs) {
                npc.buffImmune[notImmuneBuff] = false;
            }
        }
	}
}
