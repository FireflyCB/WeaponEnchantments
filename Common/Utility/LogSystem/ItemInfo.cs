﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ModLoader;
using WeaponEnchantments.Common.Utility.LogSystem.WebpageComponenets;
using WeaponEnchantments.Items;
using static WeaponEnchantments.Common.Utility.LogSystem.Wiki;
using WeaponEnchantments.Common.Utility;
using WeaponEnchantments.Common.Globals;
using Terraria.GameContent.UI;
using Microsoft.Xna.Framework;
using Terraria.ID;
using static WeaponEnchantments.Common.Utility.LogModSystem;

namespace WeaponEnchantments.Common.Utility.LogSystem
{
    public class ItemInfo
    {
        public ModItem ModItem { private set; get; }
        public Item Item { private set; get; }
        public ItemInfo(ModItem modItem) {
            ModItem = modItem;
            Item = new(modItem.Type);
        }
        public ItemInfo(Item item) {
            Item = item;
        }
        private int ShopPrice;
        public void AddStatistics(WebPage webpage) {
            List<List<string>> info = new();
            string artistString = ((IItemWikiInfo)ModItem).Artist;
            string artModifiedBy = ((IItemWikiInfo)ModItem).ArtModifiedBy;
            if (artistString == "andro951")
                artistString = null;

            if (artistString != null) {
                if (contributorLinks.ContainsKey(artistString))
                    artistString = contributorLinks[artistString].ToExternalLink(artistString);

                artistString = $"  (art by {artistString}{(artModifiedBy == null ? ")" : "")}";
            }
            
            if (artModifiedBy != null) {
                if (contributorLinks.ContainsKey(artModifiedBy))
                    artModifiedBy = contributorLinks[artModifiedBy].ToExternalLink(artModifiedBy);

                artModifiedBy = $"{(artistString == null ? "  (" : " ")}modified by {artModifiedBy})";
            }

            List<string> labels = new() {
                $"{Item.Name}",
                $"{Item.ToItemPNG(displayName: false)}",
                $"{artistString}{artModifiedBy}",
                "Statistics"
            };

            //Type
            string typeText = "";
            bool first = true;
			foreach (WikiItemTypeID id in ((IItemWikiInfo)ModItem).WikiItemTypes) {
                if (first) {
                    first = false;
				}
				else {
                    typeText += ", ";
				}
                string temp = id.GetLinkText(out bool external);
                typeText += external ? temp.ToExternalLink(id.ToString().AddSpaces()) : temp.ToLink(id.ToString().AddSpaces());
			}

            info.Add(new() { "Type", typeText });

            //Tooltip
            string tooltip = "";
            for (int i = 0; i < Item.ToolTip.Lines; i++) {
                tooltip += Item.ToolTip.GetLine(i);
            }

            if (tooltip == "") {
                List<TooltipLine> tooltipLines = new();
                Item item = new(Item.type);
                if (item.ModItem is Enchantment enchantment) {
                    float temp = enchantment.EnchantmentStrength;
                    enchantment.ModifyTooltips(tooltipLines);
                    tooltip += tooltipLines.Select(t => t.Text).JoinList();
                }
			}

            info.Add(new() { "Tooltip", $"<i>'{tooltip}'</i>" });//italics?

            //Rarity
            int rare = Item.rare;
            Color color = ItemRarity.GetColor(rare);
            string rareString;
            if (EnchantingRarity.TierColors.Contains(color)) {
                rareString = EnchantingRarity.GetTierNameFromColor(color);
            }
			else {
                if (!ItemRarityID.Search.TryGetName(Item.rare, out rareString))
                    rareString = "Failed to find rarity name";
            }

            info.Add(new() { "https://terraria.fandom.com/wiki/Rarity".ToExternalLink("Rarity"), $"<b><span style=\"color:rgb({color.R}, {color.G}, {color.B});\">{rareString}</span></b>" });

            //Buy
            if (TryGetShopPrice())
                info.Add(new() { "Buy", $"{ShopPrice.GetCoinsPNG()}" });

            //Sell
            info.Add(new() { "Sell", $"{(Item.value / 5).GetCoinsPNG()}" });

            //Research
            info.Add(new() { "https://terraria.fandom.com/wiki/Journey_Mode".ToExternalLink("Research"), $"{CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Item.type]} required" });

            webpage.AddTable(info, headers: labels, maxWidth: 400, alignID: AlignID.right, collapsible: true);
        }
        public void AddDrops(WebPage webpage) {
            int type = Item.type;
            List<(string, List<List<string>>)> dropTables = new();

            if (enemyDrops.ContainsKey(type)) {
                List<List<string>> allDropInfo = new() { new() { "Entity", "Qty.", "Rate" } };
                foreach (int npcNetID in enemyDrops[type].Keys) {
                    int minDropped = enemyDrops[type][npcNetID].stackMin;
                    int maxDropped = enemyDrops[type][npcNetID].stackMax;
                    float chance = enemyDrops[type][npcNetID].dropRate;
                    bool configDrop = ((IItemWikiInfo)ModItem).ConfigOnlyDrop && WEGlobalNPC.preHardModeBossTypes.Contains(npcNetID);
                    string chanceString = $"{(configDrop ? "(" : "")}{chance.PercentString()}{(configDrop ? ")<br/>if config enabled" : "")}";
                    List<string> dropInfo = new() { $"{npcNetID.ToNpcPNG(link: true)}", (minDropped != maxDropped ? $"{minDropped}-{maxDropped}" : $"{minDropped}"), chanceString };//Make vanilla link option to vanilla wiki
                    allDropInfo.Add(dropInfo);
                }

                dropTables.Add(($"{"Bestiary_Boss_Enemy".ToPNG()} Obtained From", allDropInfo));
            }

            if (chestDrops.ContainsKey(type)) {
                List<List<string>> allChestDropInfo = new() { new() { "Entity", "Rate" } };
                foreach (KeyValuePair<ChestID, float> pair in chestDrops[type]) {
                    List<string> dropInfo = new() { $"{pair.Key.GetItemType().ToItemPNG(link: true)}", pair.Value.PercentString() };
                    allChestDropInfo.Add(dropInfo);
                }

                dropTables.Add(($"{ItemID.Chest.ToItemPNG(displayName: false)} Obtained From", allChestDropInfo));
            }

            if (crateDrops.ContainsKey(type)) {
                List<List<string>> allCrateDropInfo = new() { new() { "Entity", "Rate" } };
                foreach (KeyValuePair<CrateID, float> pair in crateDrops[type]) {
                    List<string> dropInfo = new() { $"{((int)pair.Key).ToItemPNG(link: true)}", pair.Value.PercentString() };
                    allCrateDropInfo.Add(dropInfo);
                }

                dropTables.Add(($"{ItemID.WoodenCrate.ToItemPNG(displayName: false)} Obtained From", allCrateDropInfo));
            }

            foreach ((string, List<List<string>>) dropTable in dropTables) {
                webpage.AddTable(dropTable.Item2, firstRowHeaders: true, collapsible: true, maxWidth: 400, alignID: AlignID.right, label: dropTable.Item1);
            } 
        }
        public void AddInfo(WebPage webpage) {
            if (TryGetShopPrice() && ModItem is ISoldByWitch soldByWitch) {
                string sellPriceString = ShopPrice.GetCoinsPNG();
                string sellText = $"Can appear in the Witch's shop for {sellPriceString}. ({soldByWitch.SellCondition.ToString().AddSpaces()})";
                webpage.AddParagraph(sellText);
            }
        }
        private bool TryGetShopPrice() {
            if (ShopPrice != 0)
                return true;

            if (ModItem is ISoldByWitch soldByWitch && soldByWitch.SellCondition != SellCondition.Never) {
                ShopPrice = (int)((float)Item.value * soldByWitch.SellPriceModifier);
                return true;
            }

            return false;
        }
        public IEnumerable<IEnumerable<string>> RecipesCreateItem => GetRecipes(createItem: true);
        public IEnumerable<IEnumerable<string>> RecipesUsedIn => GetRecipes(usedIn: true);
        public IEnumerable<IEnumerable<string>> RecipesReverseRecipes => GetRecipes(createItem: true, usedIn: true, reverseRecipes: true);
        public void AddRecipes(WebPage webpage) {
            webpage.AddTable(RecipesCreateItem, label: "Recipes", firstRowHeaders: true, rowspanColumns: true, collapsible: true);
            webpage.AddTable(RecipesUsedIn, label: "Used in", firstRowHeaders: true, rowspanColumns: true, collapsible: true);
            webpage.AddTable(RecipesReverseRecipes, label: "Used in (Reverse Crafting Recipes)", firstRowHeaders: true, rowspanColumns: true, automaticCollapse: true);
        }
        private IEnumerable<IEnumerable<string>> GetRecipes(bool createItem = false, bool usedIn = false, bool reverseRecipes = false) {
            bool recipesAdded = false;

            List<List<string>> myRecipes = new() { new() { "Result", "Ingredients", "Crafting station" } };
            if (createItem && TryAddRecipeData(myRecipes, createItemRecipes, reverseRecipes))
                recipesAdded = true;

            if (usedIn && TryAddRecipeData(myRecipes, recipesUsedIn, reverseRecipes))
                recipesAdded = true;

            if (!recipesAdded)
                $"No recipe found for {Item.S()}".Log();

            if (myRecipes.Count == 1)
                myRecipes = new();

            return myRecipes;
        }
        private bool TryAddRecipeData(List<List<string>> myRecipes, Dictionary<int, List<RecipeData>> allRecipeData, bool includeReverse = false) {
            if (!allRecipeData.ContainsKey(Item.type))
                return false;

            List<RecipeData> myRecipeData = new();

            if (allRecipeData[Item.type].Count >= 20) {
                foreach (RecipeData recipeData in allRecipeData[Item.type]) {
                    bool added = false;
                    foreach (RecipeData myRecipe in myRecipeData) {
                        if (myRecipe.TryCondenseRecipe(recipeData)) {
                            added = true;
                            break;
                        }
                    }

                    if (!added)
                        myRecipeData.Add(recipeData);
                }
            }
            else {
                myRecipeData = allRecipeData[Item.type];
            }

            List<RecipeData> myRecipeData2 = myRecipeData.Where(rd => rd.requiredTile.ToString().Contains("Enchanting Table")).OrderBy(rd => -rd.requiredTile.ToString().Length).ToList();
            List<RecipeData> myRecipeData3 = myRecipeData.Where(rd => !rd.requiredTile.ToString().Contains("Enchanting Table")).OrderBy(rd => rd.requiredTile.ToString().Length).ToList();

            foreach (RecipeData recipeData in myRecipeData3.Concat(myRecipeData2)) {
                int createItemCount = recipeData.createItem.Count;
                bool reverse = recipeData.requiredItem.Count > 1 && createItemCount > recipeData.requiredItem.Count && recipeData.requiredItem.Contains(Item);
                bool reverseRecipe = recipeData.IsReverseRecipe(Item.type) || reverse;
                if (includeReverse && !reverseRecipe || !includeReverse && reverseRecipe)
                    continue;

                ConvertRecipeDataListToStringList(myRecipes, recipeData);
            }

            return true;
        }
        public static void ConvertRecipeDataListToStringList(List<List<string>> recipes, RecipeData recipeData) {
            List<string> list = new();

            int createItemNum = recipeData.createItem.CommonList.Count;
            if (createItemNum > 0)
                createItemNum++;

            int requiredItemNum = recipeData.requiredItem.CommonList.Count;
            if (requiredItemNum > 0)
                requiredItemNum++;

            bool breaksNeeded = recipeData.createItem.UniqueList != null && recipeData.requiredItem.UniqueList != null && recipeData.createItem.UniqueList.Count > 0 && recipeData.requiredItem.UniqueList.Count > 0;
            int diff = Math.Abs(createItemNum - requiredItemNum);
            string breaks = "<br/>".FillString(diff);

            string createItems = recipeData.createItem.ToString();
            if (breaksNeeded && createItemNum < requiredItemNum)
                createItems = breaks + createItems;

            list.Add(createItems);

            string requiredItems = recipeData.requiredItem.ToString();
            if (breaksNeeded && requiredItemNum < createItemNum)
                requiredItems = breaks + requiredItems;

            list.Add(requiredItems);

            if (recipeData.requiredTile.Count > 0) {
                string requiredTiles = recipeData.requiredTile.ToString();
                list.Add(requiredTiles);
            }
            else {
                list.Add($"By Hand\n");
            }

            recipes.Add(list);
        }
    }
}