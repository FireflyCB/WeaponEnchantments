﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WeaponEnchantments.Common.Globals;
using WeaponEnchantments.Common.Utility;

namespace WeaponEnchantments.Effects
{
	public class DamageClassChange : EnchantmentEffect
	{
		public DamageClassChange(DamageClass damageClass) {
			NewDamageClass = damageClass;
		}
		public override string DisplayName => $"Convert damage type to {NewDamageClass.DisplayName}";

		public virtual DamageClass NewDamageClass { get; }
		public DamageClass BaseDamageClass = null;

		public void Update(ref Item item, bool reset = false) {
			if (reset) {
				Reset(ref item);
			}
			else {
				ApplyTo(ref item);
			}
		}
		public void ApplyTo(ref Item item) {
			item.DamageType = NewDamageClass;
			if (BaseDamageClass == null)
				BaseDamageClass = ContentSamples.ItemsByType[item.type].DamageType;

			if (item.TryGetEnchantedItem(out EnchantedItem iGlobal)) {
				iGlobal.damageType = NewDamageClass;
				iGlobal.baseDamageType = BaseDamageClass;
			}
		}
		public void Reset(ref Item item) {
			item.DamageType = BaseDamageClass;
			BaseDamageClass = null;
		}
	}
}