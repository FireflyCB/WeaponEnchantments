﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using WeaponEnchantments.Items;
using WeaponEnchantments.UI;

namespace WeaponEnchantments.Common.Globals
{
    public class EnchantedItem : GlobalItem
    {
        //Start Packet fields
        public int experience;//current experience of a weapon/armor/accessory item
        public Item[] enchantments = new Item[EnchantingTable.maxEnchantments];//Track enchantment items on a weapon/armor/accessory item
        //End Packet fields

        public Dictionary<string, StatModifier> statModifiers;
        public Dictionary<string, StatModifier> appliedStatModifiers;
        public Dictionary<string, StatModifier> eStats;
        public Dictionary<string, StatModifier> appliedEStats;
        public Dictionary<int, int> buffs;
        public Dictionary<int, int> debuffs;
        public Dictionary<int, int> onHitBuffs;

        public int lastValueBonus;
        public int levelBeforeBooster;
        public int level;
        public bool powerBoosterInstalled;//Tracks if Power Booster is installed on item +10 levels to spend on enchantments (Does not affect experience)
        public bool inEnchantingTable;
        public bool equip = false;
        public bool trackedWeapon = false;
        public bool hoverItem = false;
        public bool trashItem = false;
        public bool favorited = false;
        public const int maxLevel = 40;
        public int prefix;
        public EnchantedItem()
        {
            for (int i = 0; i < EnchantingTable.maxEnchantments; i++) 
            {
                enchantments[i] = new Item();
            }
            statModifiers = new Dictionary<string, StatModifier>();
            appliedStatModifiers = new Dictionary<string, StatModifier>();
            appliedEStats = new Dictionary<string, StatModifier>();
            eStats = new Dictionary<string, StatModifier>();
            buffs = new Dictionary<int, int>();
            debuffs = new Dictionary<int, int>();
            onHitBuffs = new Dictionary<int, int>();
        }//Constructor
        public override bool InstancePerEntity => true;
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return WEMod.IsEnchantable(entity);
        }
        public override GlobalItem Clone(Item item, Item itemClone)
        {
            EnchantedItem clone = (EnchantedItem)base.Clone(item, itemClone);
            clone.enchantments = (Item[])enchantments.Clone();
            for (int i = 0; i < enchantments.Length; i++)
            {
               clone.enchantments[i] = enchantments[i].Clone();
            }//fixes enchantments being applied to all of an item instead of just the instance
            clone.statModifiers = new Dictionary<string, StatModifier>(statModifiers);
            clone.eStats = new Dictionary<string, StatModifier>(eStats);
            clone.buffs = new Dictionary<int, int>(buffs);
            clone.debuffs = new Dictionary<int, int>(debuffs);
            clone.onHitBuffs = new Dictionary<int, int>(onHitBuffs);
            clone.appliedStatModifiers = new Dictionary<string, StatModifier>(appliedStatModifiers);
            clone.appliedEStats = new Dictionary<string, StatModifier>(appliedEStats);
            clone.equip = false;
            if(!Main.mouseItem.IsSameEnchantedItem(itemClone))
                clone.trackedWeapon = false;
            return clone;
        }
        public override void NetSend(Item item, BinaryWriter writer)
        {
            if (WEMod.IsEnchantable(item))
            {
                if(UtilityMethods.debugging) ($"\\/NetSend(" + item.Name + ")").Log();
                if(UtilityMethods.debugging) ($"eStats.Count: " + eStats.Count + ", statModifiers.Count: " + statModifiers.Count).Log();
                writer.Write(experience);
                writer.Write(powerBoosterInstalled);
                for (int i = 0; i < EnchantingTable.maxEnchantments; i++)
                {
                    writer.Write((short)enchantments[i].type);
                    if(UtilityMethods.debugging) ($"e " + i + ": " + enchantments[i].Name).Log();
                }
                short count = (short)eStats.Count;
                writer.Write(count);
                foreach(string key in eStats.Keys)
                {
                    writer.Write(key);
                    writer.Write(eStats[key].Additive);
                    writer.Write(eStats[key].Multiplicative);
                    writer.Write(eStats[key].Base);
                    writer.Write(eStats[key].Flat);
                }
                count = (short)statModifiers.Count;
                writer.Write(count);
                foreach (string key in statModifiers.Keys)
                {
                    writer.Write(key);
                    writer.Write(statModifiers[key].Additive);
                    writer.Write(statModifiers[key].Multiplicative);
                    writer.Write(statModifiers[key].Flat);
                    writer.Write(statModifiers[key].Base);
                }
                if(UtilityMethods.debugging) ($"eStats.Count: " + eStats.Count + ", statModifiers.Count: " + statModifiers.Count).Log();
                if(UtilityMethods.debugging) ($"/\\NetSend(" + item.Name + ")").Log();
            }
        }
        public override void NetReceive(Item item, BinaryReader reader)
        {
            if (WEMod.IsEnchantable(item))
            {
                if(UtilityMethods.debugging) ($"\\/NetRecieve(" + item.Name + ")").Log();
                if(UtilityMethods.debugging) ($"eStats.Count: " + eStats.Count + ", statModifiers.Count: " + statModifiers.Count).Log();
                experience = reader.ReadInt32();
                powerBoosterInstalled = reader.ReadBoolean();
                for (int i = 0; i < EnchantingTable.maxEnchantments; i++)
                {
                    enchantments[i] = new Item(reader.ReadUInt16());
                    if(UtilityMethods.debugging) ($"e " + i + ": " + enchantments[i].Name).Log();
                }
                eStats.Clear();
                int count = reader.ReadUInt16();
                for (int i = 0; i < count; i++)
                {
                    string key = reader.ReadString();
                    float additive = reader.ReadSingle();
                    float multiplicative = reader.ReadSingle();
                    float flat = reader.ReadSingle();
                    float @base = reader.ReadSingle();
                    eStats.Add(key, new StatModifier(additive, multiplicative, flat, @base));
                }
                statModifiers.Clear();
                count = reader.ReadUInt16();
                for (int i = 0; i < count; i++)
                {
                    string key = reader.ReadString();
                    float additive = reader.ReadSingle();
                    float multiplicative = reader.ReadSingle();
                    float flat = reader.ReadSingle();
                    float @base = reader.ReadSingle();
                    statModifiers.Add(key, new StatModifier(additive, multiplicative, flat, @base));
                }
                if(UtilityMethods.debugging) ($"eStats.Count: " + eStats.Count + ", statModifiers.Count: " + statModifiers.Count).Log();
                if(UtilityMethods.debugging) ($"/\\NetRecieve(" + item.Name + ")").Log();
            }
        }
        public override void UpdateEquip(Item item, Player player)
        {
            /*WEPlayer wePlayer = Main.LocalPlayer.GetModPlayer<WEPlayer>();
            float damageModifier = 0f;
            float speedModifier = 0f;
            int defenceBonus = 0;
            float criticalBonus = 0f;
            float armorPenetrationBonus = 0f;
            for (int i = 0; i < EnchantingTable.maxEnchantments; i++)
            {
                AllForOneEnchantmentBasic enchantment = ((AllForOneEnchantmentBasic)enchantments[i].ModItem);
                if (!enchantments[i].IsAir)
                {
                    float str = enchantment.EnchantmentStrength;
                    switch ((EnchantmentTypeID)enchantment.EnchantmentType)
                    {
                        case EnchantmentTypeID.Damage:
                            damageModifier += str;
                            break;
                        case EnchantmentTypeID.Speed:
                            speedModifier += str;
                            break;
                        case EnchantmentTypeID.Defence:
                            defenceBonus += (int)Math.Round(str);
                            break;
                        case EnchantmentTypeID.CriticalStrikeChance:
                            criticalBonus += str;
                            break;
                        case EnchantmentTypeID.ArmorPenetration:
                            armorPenetrationBonus += str;
                            break;
                    }
                }
            }
            player.GetDamage(DamageClass.Generic) += damageModifier / 4;
            player.GetAttackSpeed(DamageClass.Generic) += speedModifier / 4;
            player.GetCritChance(DamageClass.Generic) += criticalBonus * 25;
            player.GetArmorPenetration(DamageClass.Generic) += armorPenetrationBonus / 4;
            item.defense += defenceBonus - lastDefenceBonus;
            lastDefenceBonus = defenceBonus;  */
        }
        public override void UpdateInventory(Item item, Player player)
        {
            WEPlayer wePlayer = Main.LocalPlayer.GetModPlayer<WEPlayer>();
            if(experience > 0 || powerBoosterInstalled)
            {
                int value = 0;
                for (int i = 0; i < EnchantingTable.maxEnchantments; i++)
                {
                    value += enchantments[i].value;
                }
                int powerBoosterValue = powerBoosterInstalled ? ModContent.GetModItem(ModContent.ItemType<PowerBooster>()).Item.value : 0;
                int npcTalking = player.talkNPC != -1 ? Main.npc[player.talkNPC].type : -1;
                int valueToAdd = npcTalking != NPCID.GoblinTinkerer ? value + (int)(EnchantmentEssenceBasic.valuePerXP * experience) + powerBoosterValue : 0;
                item.value += valueToAdd - lastValueBonus;//Update items value based on enchantments installed
                lastValueBonus = valueToAdd;
            }
            equip = false;
            if (wePlayer.stickyFavorited)
            {
                if (item.favorited)
                {
                    if (!favorited && Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) || Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightAlt))
                    {
                        favorited = true;
                    }
                }//Sticky Favorited
                else
                {
                    if (favorited)
                    {
                        if (!(Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) || Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightAlt)))
                        {
                            item.favorited = true;
                        }
                        else
                        {
                            favorited = false;
                        }
                    }
                }//Sticky Favorited
            }
        }
        public void UpdateLevel()
        {
            int l;
            for(l = 0; l < maxLevel; l++)
            {
                if(experience < WEModSystem.levelXps[l])
                {
                    level = l + 1;
                    break;
                }
            }
            if (l == maxLevel)
            {
                levelBeforeBooster = maxLevel;
                level = powerBoosterInstalled ? maxLevel + 10 : maxLevel;
            }
            else
            {
                levelBeforeBooster = l;
                level = powerBoosterInstalled ? l + 10 : l;
            }
        }
        public int GetLevelsAvailable()
        {
            UpdateLevel();
            int total = 0;
            for (int i = 0; i < enchantments.Length; i++)
            {
                if (enchantments[i] != null && !enchantments[i].IsAir)
                {
                    //if(UtilityMethods.debugging) ($"enchantments[" + i + "]: name: " + enchantments[i].Name + " type: " + enchantments[i].type).Log();
                    AllForOneEnchantmentBasic enchantment = ((AllForOneEnchantmentBasic)enchantments[i].ModItem);
                    total += enchantment.GetLevelCost();
                }
            }
            return level - total;
        }
        public override void LoadData(Item item, TagCompound tag)
        {
            if (WEMod.IsEnchantable(item))
            {
                if(UtilityMethods.debugging) ($"\\/LoadData(" + item.Name + ")").Log();
                experience = tag.Get<int>("experience");//Load experience tag
                powerBoosterInstalled = tag.Get<bool>("powerBooster");//Load status of powerBoosterInstalled
                UpdateLevel();
                for (int i = 0; i < EnchantingTable.maxEnchantments; i++)
                {
                    if (tag.Get<Item>("enchantments" + i.ToString()) != null)
                    {
                        if (!tag.Get<Item>("enchantments" + i.ToString()).IsAir)
                        {
                            enchantments[i] = tag.Get<Item>("enchantments" + i.ToString()).Clone();
                            OldItemManager.ReplaceOldItem(ref enchantments[i]);
                            if(UtilityMethods.debugging) ($"e " + i + ": " + enchantments[i].Name).Log();
                        }
                        else
                        {
                            enchantments[i] = new Item();
                        }
                    }
                }//Load enchantment item tags
                if(UtilityMethods.debugging) ($"/\\LoadData(" + item.Name + ")").Log();
            }
        }
        public override void SaveData(Item item, TagCompound tag)
        {
            if (enchantments != null)
            {
                for (int i = 0; i < EnchantingTable.maxEnchantments; i++)
                {
                    if (enchantments[i] != null)
                    {
                        if (!enchantments[i].IsAir)
                        {
                            tag["enchantments" + i.ToString()] = enchantments[i].Clone();
                        }
                    }
                }//Save enchantment item tags
            }
            tag["experience"] = experience;//Save experience tag
            tag["powerBooster"] = powerBoosterInstalled;//save status of powerBoosterInstalled
        }
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            bool enchantmentsToolTipAdded = false;
            bool enchantemntInstalled = false;
            UpdateLevel();
            for (int i = 0; i < EnchantingTable.maxEnchantments; i++)
            {
                if (!enchantments[i].IsAir)
                {
                    enchantemntInstalled = true;
                    break;
                }
            }
            if (experience > 0 || powerBoosterInstalled || inEnchantingTable || enchantemntInstalled)
            {
                if (powerBoosterInstalled)
                    tooltips.Add(new TooltipLine(Mod, "level", $"Level: {levelBeforeBooster} Points available: {GetLevelsAvailable()} (Booster Installed)") { OverrideColor = Color.LightGreen });
                else
                    tooltips.Add(new TooltipLine(Mod, "level", $"Level: {levelBeforeBooster} Points available: {GetLevelsAvailable()}") { OverrideColor = Color.LightGreen });
                string levelString = levelBeforeBooster < maxLevel ? $" ({WEModSystem.levelXps[levelBeforeBooster] - experience} to next level)" : " (Max Level)";
                tooltips.Add(new TooltipLine(Mod, "experience", $"Experience: {experience}{levelString}") { OverrideColor = Color.White });
            }
            for (int i = 0; i < EnchantingTable.maxEnchantments; i++)
            {
                if (!enchantments[i].IsAir)
                {
                    AllForOneEnchantmentBasic enchantment = ((AllForOneEnchantmentBasic)enchantments[i].ModItem);
                    if (!enchantmentsToolTipAdded)
                    {
                        tooltips.Add(new TooltipLine(Mod, "enchantmentsToolTip", "Enchantments:") { OverrideColor = Color.Violet});
                        enchantmentsToolTipAdded = true;
                    }//Enchantmenst: tooltip
                    string itemType = "";
                    if (WEMod.IsWeaponItem(item))
                        itemType = "Weapon";
                    else if (WEMod.IsArmorItem(item))
                        itemType = "Armor";
                    else if (WEMod.IsAccessoryItem(item))
                        itemType = "Accessory";
                    tooltips.Add(new TooltipLine(Mod, "enchantment" + i.ToString(), enchantment.AllowedListTooltips[itemType])
                    {
                        OverrideColor = AllForOneEnchantmentBasic.rarityColors[enchantment.EnchantmentSize]
                    });
                }
            }//Edit Tooltips
        }
        public void DamageNPC(Item item, Player player, NPC target, int damage, bool crit, bool melee = false)
        {
            int useTime = item.useTime;
            int animationSpeed = item.useAnimation;
            target.GetGlobalNPC<WEGlobalNPC>().xpCalculated = true;
            float value;
            switch (Main.netMode)
            {
                case 1:
                    value = ContentSamples.NpcsByNetId[target.type].value;
                    break;
                default:
                    value = target.value;
                    break;
            }
            if (target.type != NPCID.TargetDummy && !target.friendly && !target.townNPC && (value > 0 || !target.SpawnedFromStatue && target.lifeMax > 10))
            {
                int xpInt;
                int xpDamage;
                float multiplier;
                float effDamage;
                float effDamageDenom;
                float xp;
                multiplier = (1f + ((float)((target.noGravity ? 2f : 0f) + (target.noTileCollide ? 2f : 0f)) + 2f * (1f - target.knockBackResist)) / 10f) * (target.boss ? WEMod.config.BossExperienceMultiplier/400f : WEMod.config.BossExperienceMultiplier/100f);
                effDamage = (float)item.damage * (1f + (float)player.GetWeaponCrit(item) / 100f);
                float actualDefence = target.defense / 2f - target.checkArmorPenetration(player.GetWeaponArmorPenetration(item));
                float actualDamage = melee ? damage : damage - actualDefence;
                actualDamage = crit && !melee ? actualDamage * 2 : actualDamage;
                xpDamage = target.life < 0 ? (int)actualDamage + target.life : (int)actualDamage;
                if(xpDamage > 0)
                {
                    effDamageDenom = effDamage - actualDefence;
                    if (effDamageDenom > 1)
                        xp = (float)xpDamage * multiplier * effDamage / effDamageDenom;
                    else
                        xp = (float)xpDamage * multiplier * effDamage;
                    xp /= UtilityMethods.GetReductionFactor((int)target.lifeMax);
                    xpInt = (int)Math.Round(xp);
                    xpInt = xpInt > 1 ? xpInt : 1;
                    if (!item.consumable)
                    {
                        //ModContent.GetInstance<WEMod>().Logger.Info(wePlayer.Player.name + " recieved " + xpInt.ToString() + " xp from hitting " + target.FullName + ".");
                        //Main.NewText(wePlayer.Player.name + " recieved " + xpInt.ToString() + " xp from killing " + target.FullName + ".");
                        GainXP(item, xpInt);
                    }
                    AllArmorGainXp(xpInt);
                }
            }
        }
        public static void AllArmorGainXp(int xp)
        {
            WEPlayer wePlayer = Main.LocalPlayer.GetModPlayer<WEPlayer>();
            int xpInt;
            int i = 0;
            foreach (Item armor in wePlayer.Player.armor)
            {
                if (i < 10)
                {
                    if (!armor.vanity && !armor.IsAir)
                    {
                        if (armor.GetGlobalItem<EnchantedItem>().levelBeforeBooster < maxLevel)
                        {
                            if (WEMod.IsArmorItem(armor))
                            {
                                xpInt = (int)Math.Round(xp / 2f);
                                xpInt = xpInt > 0 ? xpInt : 1;
                                armor.GetGlobalItem<EnchantedItem>().GainXP(armor, xpInt);
                            }
                            else
                            {
                                xpInt = (int)Math.Round(xp / 4f);
                                xpInt = xpInt > 0 ? xpInt : 1;
                                armor.GetGlobalItem<EnchantedItem>().GainXP(armor, xpInt);
                            }
                            //wePlayer.equiptArmor[i].GetGlobalItem<EnchantedItem>().GainXP(wePlayer.equiptArmor[i], xpInt);
                        }
                    }
                }
                i++;
            }
        }
        public void GainXP(Item item, int xpInt)
        {
            WEPlayer wePlayer = Main.LocalPlayer.GetModPlayer<WEPlayer>();
            int currentLevel = levelBeforeBooster;
            experience += xpInt;
            if (levelBeforeBooster < maxLevel)
            {
                UpdateLevel();
                if (levelBeforeBooster > currentLevel && wePlayer.usingEnchantingTable)
                {
                    if(levelBeforeBooster == 40)
                    {
                        SoundEngine.PlaySound(SoundID.Unlock);
                        //ModContent.GetInstance<WEMod>().Logger.Info("Congratulations!  " + wePlayer.Player.name + "'s " + item.Name + " reached the maximum level, " + levelBeforeBooster.ToString() + " (" + WEModSystem.levelXps[levelBeforeBooster - 1] + " xp).");
                        Main.NewText("Congratulations!  " + wePlayer.Player.name + "'s " + item.Name + " reached the maximum level, " + levelBeforeBooster.ToString() + " (" + WEModSystem.levelXps[levelBeforeBooster - 1] + " xp).");
                    }
                    else
                    {
                        //ModContent.GetInstance<WEMod>().Logger.Info(wePlayer.Player.name + "'s " + item.Name + " reached level " + levelBeforeBooster.ToString() + " (" + WEModSystem.levelXps[levelBeforeBooster - 1] + " xp).");
                        Main.NewText(wePlayer.Player.name + "'s " + item.Name + " reached level " + levelBeforeBooster.ToString() + " (" + WEModSystem.levelXps[levelBeforeBooster - 1] + " xp).");
                    }
                }
            }
        }
        public override bool CanConsumeAmmo(Item weapon, Item ammo, Player player)
        {
            /*float ammoCostBonus = 0f;
            for (int i = 0; i < EnchantingTable.maxEnchantments; i++)
            {
                AllForOneEnchantmentBasic enchantment = ((AllForOneEnchantmentBasic)enchantments[i].ModItem);
                if (!enchantments[i].IsAir)
                {
                    float str = enchantment.EnchantmentStrength;
                    switch ((EnchantmentTypeID)enchantment.EnchantmentType)
                    {
                        case EnchantmentTypeID.AmmoCost:
                            ammoCostBonus += str;
                            break;
                    }
                }
            }*/
            return Main.rand.NextFloat() >= -1f * weapon.AEI("AmmoCost", 0f); //(eStats.ContainsKey("AmmoCost") ? eStats["AmmoCost"].ApplyTo(0f) : 0f);
        }
        public override bool? UseItem(Item item, Player player)
        {
            WEPlayer wePlayer = player.GetModPlayer<WEPlayer>();
            //if (allForOne)
            if (eStats.ContainsKey("CatastrophicRelease"))
            {
                player.statMana = 0;
            }
            if(eStats.ContainsKey("AllForOne"))
            {
                //wePlayer.allForOneCooldown = true;
                wePlayer.allForOneTimer = (int)((float)item.useTime * item.AEI("NPCHitCooldown", 0.5f));
            }
            return null;
        }
        public override bool CanUseItem(Item item, Player player)
        {
            WEPlayer wePlayer = Main.LocalPlayer.GetModPlayer<WEPlayer>();
            if (eStats.ContainsKey("CatastrophicRelease") && player.statManaMax != player.statMana)
                return false;
            if (wePlayer.usingEnchantingTable && WeaponEnchantmentUI.preventItenUse)
                return false;
            return eStats.ContainsKey("AllForOne") ? (wePlayer.allForOneTimer <= 0 ? true : false) : true;
        }
        public override bool CanRightClick(Item item)
        {
            if (item.stack > 1)
            {
                if (experience > 0 || powerBoosterInstalled)
                {
                    if (Main.mouseItem.IsAir)
                    {
                        return true;
                    }
                }
            }//Prevent splitting stack of enchantable items with maxstack > 1
            return false;
        }
        public override void RightClick(Item item, Player player)
        {
            if (item.stack > 1)
            {
                if (experience > 0 || powerBoosterInstalled)
                {
                    if (Main.mouseItem.IsAir)
                    {
                        item.stack++;
                    }
                }
            }//Prevent splitting stack of enchantable items with maxstack > 1
        }
        public override void OnHitNPC(Item item, Player player, NPC target, int damage, float knockBack, bool crit)
        {
            DamageNPC(item, player, target, damage, crit, true);
        }
        public override void PostReforge(Item item)
        {
            WEPlayer wePlayer = Main.LocalPlayer.GetModPlayer<WEPlayer>();
            wePlayer.UpdateItemStats(ref item);
        }
    }
}
