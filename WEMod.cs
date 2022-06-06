﻿using IL.Terraria.Localization;
using MonoMod.RuntimeDetour.HookGen;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;
using WeaponEnchantments.Common;
using WeaponEnchantments.Common.Globals;
using WeaponEnchantments.Items;


namespace WeaponEnchantments
{
    public class WEMod : Mod
    {
		internal static bool IsEnchantable(Item item)
        {
			if((IsWeaponItem(item) || IsArmorItem(item) || IsAccessoryItem(item)) && !item.consumable)
            {
				return true;
			}
            else
            {
				return false;
            }
        }
		internal static bool IsWeaponItem(Item item)
		{
			return item != null && (item.damage > 0 && item.ammo == 0 || item.type == ItemID.CoinGun) && !item.accessory && !item.IsAir;
		}
		internal static bool IsArmorItem(Item item)
		{
			return item != null && !item.vanity && (item.headSlot > -1 || item.bodySlot > -1 || item.legSlot > -1) && !item.IsAir;
		}
		internal static bool IsAccessoryItem(Item item)
		{
			return item != null && item.accessory && !IsArmorItem(item) && !item.IsAir;
		}
		internal static bool IsEnchantmentItem(Item item, bool utility)
        {
			if(item.ModItem is AllForOneEnchantmentBasic)
            {
                if (utility)
                {
                    if (((AllForOneEnchantmentBasic)item.ModItem).Utility)
                    {
						return true;
                    }
                    else
                    {
						return false;
                    }
                }
                else
                {
					return true;
				}
			}
            else
            {
				return false;
            }
        }
		internal static bool IsEssenceItem(Item item)
        {
			if (item.ModItem is EnchantmentEssenceBasic)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		public static class PacketIDs
		{
			public const byte TransferGlobalItemFields = 0;
			public const byte Enchantment = 1;
		}
		public void SendEnchantmentPacket(byte enchantmentSlotNumber, byte slotNumber, short itemType, short bank = -1, byte type = 1)
		{
			ModPacket packet = GetPacket();
			packet.Write(type);
			packet.Write(enchantmentSlotNumber);
			packet.Write(slotNumber);
			if(bank != -1)
				packet.Write(bank);
			packet.Write(itemType);
			packet.Send();
		}
		public void SendPacket(byte type, Item newItem, Item oldItem, bool weapon = true, byte armorSlot = 0)
        {
			ModPacket packet = GetPacket();
			packet.Write(type);
            switch (type)
            {
				case PacketIDs.TransferGlobalItemFields:
					packet.Write(weapon);
					packet.Write(armorSlot);
					bool writeNewItem = IsEnchantable(newItem);
					packet.Write(writeNewItem);
					if (writeNewItem)
						WriteItem(newItem, packet);
					bool writeOldItem = IsEnchantable(oldItem);
					packet.Write(writeOldItem);
					if (writeOldItem)
						WriteItem(oldItem, packet);
					break;
            }
			packet.Send();
		}
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
			byte type = reader.ReadByte();
			("\\/HandlePacket(reader, " + whoAmI + ": " + Main.player[whoAmI].name + ") type: " + type).Log();
			switch (type)
			{
				case PacketIDs.TransferGlobalItemFields:
					bool weapon = reader.ReadBoolean();
					byte armorSlot = reader.ReadByte();
					WEPlayer wePlayer = Main.player[whoAmI].GetModPlayer<WEPlayer>();
					Item newItem = weapon ? armorSlot == 0 ? wePlayer.Player.HeldItem : Main.mouseItem : wePlayer.Player.armor[armorSlot];
					Item oldItem = weapon ? wePlayer.trackedWeapon : wePlayer.equipArmor[armorSlot];
					bool readNewItem = reader.ReadBoolean();
					if (readNewItem)
						ReadItem(newItem, reader);
					else
						newItem = new Item();
					bool readOldItem = reader.ReadBoolean();
					if (readOldItem)
					{
						oldItem = new Item(ItemID.CopperShortsword);
						ReadItem(oldItem, reader);
					}
					wePlayer.UpdatePotionBuffs(ref newItem, ref oldItem);
					wePlayer.UpdatePlayerStats(ref newItem, ref oldItem);
					/*bool weapon = reader.ReadBoolean();
					byte armorSlot = reader.ReadByte();
					WEPlayer wePlayer = Main.player[whoAmI].GetModPlayer<WEPlayer>();
					Item newItem = weapon ? wePlayer.trackedWeapon : wePlayer.Player.armor[armorSlot];
					Item oldItem = new Item();
					bool readNewItem = reader.ReadBoolean();
					if (readNewItem)
						ReadItem(newItem, reader);
					else
						newItem = new Item();
					bool readOldItem = reader.ReadBoolean();
                    if (readOldItem)
                    {
						oldItem = new Item(ItemID.CopperShortsword);
						ReadItem(oldItem, reader);
					}
					wePlayer.UpdatePotionBuffs(ref newItem, ref oldItem);
					wePlayer.UpdatePlayerStats(ref newItem, ref oldItem);*/
					break;
				case PacketIDs.Enchantment:
					byte enchantmentSlotNumber = reader.ReadByte();
					byte slotNumber = reader.ReadByte();
					short bank = -1;
					if(slotNumber >= 50 && slotNumber < 90)
						bank = reader.ReadByte();
					short itemType = reader.ReadInt16();
					Item item = new Item();
					switch(slotNumber)
					{
						case < 50:
							item = Main.player[whoAmI].inventory[slotNumber];
							break;
						case < 90:
							switch(bank)
							{
								case -2:
									item = Main.player[whoAmI].bank.item[slotNumber - 50];
									break;
								case -3:
									item = Main.player[whoAmI].bank2.item[slotNumber - 50];
									break;
								case -4:
									item = Main.player[whoAmI].bank3.item[slotNumber - 50];
									break;
								case -5:
									item = Main.player[whoAmI].bank4.item[slotNumber - 50];
									break;
							}
							break;
						case 90:
							item = Main.player[whoAmI].GetModPlayer<WEPlayer>().enchantingTableUI.itemSlotUI[0].Item;
							break;
						case <= 100:
							item = Main.player[whoAmI].armor[slotNumber - 91];
							break;
					}
					item.G().enchantments[enchantmentSlotNumber] = new Item(itemType);
					AllForOneEnchantmentBasic enchantment = (AllForOneEnchantmentBasic)item.G().enchantments[enchantmentSlotNumber].ModItem;
					item.UpdateEnchantment(Main.player[whoAmI], ref enchantment, enchantmentSlotNumber);
					/*int itemWhoAmI = reader.ReadInt32();
					byte i = reader.ReadByte();
					short enchantmentType = reader.ReadInt16();
					Main.item[itemWhoAmI].G().enchantments[i] = new Item(enchantmentType);*/
					break;
				default:
					ModContent.GetInstance<WEMod>().Logger.Debug("*NOT RECOGNIZED*\ncase: " + type + "\n*NOT RECOGNIZED*");
					break;
			}
			("/\\HandlePacket(reader, " + whoAmI + ": " + Main.player[whoAmI].name + ") type: " + type).Log();
		}
		private void ReadItem(Item item, BinaryReader reader)
        {
            if (IsEnchantable(item))
            {
				EnchantedItem iGlobal = item.G();
				iGlobal.experience = reader.ReadInt32();
				iGlobal.powerBoosterInstalled = reader.ReadBoolean();
				for (int i = 0; i < EnchantingTable.maxEnchantments; i++)
				{
					iGlobal.enchantments[i] = new Item(reader.ReadUInt16());
				}
				iGlobal.eStats.Clear();
				int count = reader.ReadUInt16();
				for (int i = 0; i < count; i++)
				{
					string key = reader.ReadString();
					float additive = reader.ReadSingle();
					float multiplicative = reader.ReadSingle();
					float flat = reader.ReadSingle();
					float @base = reader.ReadSingle();
					iGlobal.eStats.Add(key, new StatModifier(additive, multiplicative, flat, @base));
				}
				iGlobal.statModifiers.Clear();
				count = reader.ReadUInt16();
				for (int i = 0; i < count; i++)
				{
					string key = reader.ReadString();
					float additive = reader.ReadSingle();
					float multiplicative = reader.ReadSingle();
					float flat = reader.ReadSingle();
					float @base = reader.ReadSingle();
					iGlobal.statModifiers.Add(key, new StatModifier(additive, multiplicative, flat, @base));
				} 
			}
		}
		private void WriteItem(Item item, ModPacket packet)
        {
			EnchantedItem iGlobal = item.G();
			packet.Write(iGlobal.experience);
			packet.Write(iGlobal.powerBoosterInstalled);
			for (int i = 0; i < EnchantingTable.maxEnchantments; i++)
			{
				packet.Write((short)iGlobal.enchantments[i].type);
			}
			short count = (short)iGlobal.eStats.Count;
			packet.Write(count);
			foreach (string key in iGlobal.eStats.Keys)
			{
				packet.Write(key);
				packet.Write(iGlobal.eStats[key].Additive);
				packet.Write(iGlobal.eStats[key].Multiplicative);
				packet.Write(iGlobal.eStats[key].Base);
				packet.Write(iGlobal.eStats[key].Flat);
			}
			count = (short)iGlobal.statModifiers.Count;
			packet.Write(count);
			foreach (string key in iGlobal.statModifiers.Keys)
			{
				packet.Write(key);
				packet.Write(iGlobal.statModifiers[key].Additive);
				packet.Write(iGlobal.statModifiers[key].Multiplicative);
				packet.Write(iGlobal.statModifiers[key].Flat);
				packet.Write(iGlobal.statModifiers[key].Base);
			}
		}
	public override void AddRecipeGroups()
	{
		RecipeGroup group = new RecipeGroup(() => "Any Common Gem", new int[]
		{
			180, 181, 178, 179, 177
		});
		RecipeGroup.RegisterGroup("WeaponEnchantments:CommonGems", group);
		group = new RecipeGroup(() => "Any Rare Gem", new int[]
		{
			999, 182
		});
		RecipeGroup.RegisterGroup("WeaponEnchantments:RareGems", group);
	}
        private delegate Item orig_ItemIOLoad(TagCompound tag);
		private delegate Item hook_ItemIOLoad(orig_ItemIOLoad orig, TagCompound tag);
		private static readonly MethodInfo ModLoaderIOItemIOLoadMethodInfo = typeof(Main).Assembly.GetType("Terraria.ModLoader.IO.ItemIO")!.GetMethod("Load", BindingFlags.Public | BindingFlags.Static, new System.Type[] { typeof(TagCompound) })!;
		public override void Load()
        {
			HookEndpointManager.Add<hook_ItemIOLoad>(ModLoaderIOItemIOLoadMethodInfo, ItemIOLoadDetour);
        }
		public override void Unload()
		{
			return;
		}
		private Item ItemIOLoadDetour(orig_ItemIOLoad orig, TagCompound tag)
        {
			Item item = orig(tag);
			if(item.ModItem is UnloadedItem)
            {
				OldItemManager.ReplaceOldItem(ref item);
			}
			return item;
        }


		/*private delegate void orig_ItemIOLoad(Item item, TagCompound tag);
		private delegate void hook_ItemIOLoad(orig_ItemIOLoad orig, Item item, TagCompound tag);
		private static readonly MethodInfo ModLoaderIOItemIOLoadMethodInfo = 
			typeof(Main).Assembly.GetType("Terraria.ModLoader.IO.ItemIO")!.GetMethod("Load", BindingFlags.Public | BindingFlags.Static, new System.Type[] { typeof(Item), typeof(TagCompound) })!;

		public override void Load()
        {
			HookEndpointManager.Add<hook_ItemIOLoad>(ModLoaderIOItemIOLoadMethodInfo, ItemIOLoadDetour);
        }
		public override void Unload()
		{
			return;
		}
		private void ItemIOLoadDetour(orig_ItemIOLoad orig, Item item, TagCompound tag)
        {
			orig(item, tag);
			if(item.ModItem is UnloadedItem)
            {
				OldItemManager.ReplaceOldItem(ref item);
				//orig(item, tag);
			 }
        }*/

		/*private delegate void orig_ItemIOLoad(Item item, TagCompound tag);
		private delegate void hook_ItemIOLoad(orig_ItemIOLoad orig, Item item, TagCompound tag);
		private static readonly MethodInfo ModLoaderIOItemIOLoadMethodInfo = 
			typeof(Main).Assembly.GetType("Terraria.ModLoader.Default.UnloadedGlobalItem")!.GetMethod("LoadData", BindingFlags.Public, new System.Type[] { typeof(Item), typeof(TagCompound) })!;
		public override void Load()
        {
			HookEndpointManager.Add<hook_ItemIOLoad>(ModLoaderIOItemIOLoadMethodInfo, ItemIOLoadDetour);
        }
		public override void Unload()
		{
			return;
		}
		private void ItemIOLoadDetour(orig_ItemIOLoad orig, Item item, TagCompound tag)
        {
			orig(item, tag);
			if(item.ModItem is UnloadedItem)
            {
				OldItemManager.ReplaceOldItem(ref item);
				//orig(item, tag);
			}
        }*/



		/*OldItemManager.ReplaceOldItem(ref item);
            if(item.Name == "Unloaded Item")
                base.LoadData(item, tag);*/
	}
}
