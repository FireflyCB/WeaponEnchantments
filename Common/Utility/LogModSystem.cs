﻿using Hjson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using WeaponEnchantments.Common.Globals;
using WeaponEnchantments.Items;
using WeaponEnchantments.Localization;
using static Terraria.Localization.GameCulture;
using static WeaponEnchantments.Common.Utility.LogModSystem.GetItemDictModeID;

namespace WeaponEnchantments.Common.Utility
{
    public class LogModSystem : ModSystem {
        public static bool printListOfContributors = false;
        public static bool printListOfEnchantmentTooltips => WEMod.clientConfig.PrintEnchantmentTooltips;
        public static bool printLocalization = WEMod.clientConfig.PrintLocalizationLists;
        public static bool printListForDocumentConversion = false;
        public static bool printEnchantmentDrops => WEMod.clientConfig.PrintEnchantmentDrops;

        public static class GetItemDictModeID {
            public static byte Weapon = 0;
            public static byte Armor = 1;
            public static byte Accessory = 2;
        }
        public static Dictionary<int, bool> PrintListOfItems = new Dictionary<int, bool>() {
            { Weapon, false },
            { Armor, false },
            { Accessory, false }
        };


        //Only used to print the full list of contributors.
        private static Dictionary<string, string> contributorLinks = new Dictionary<string, string>() {
            { "Zorutan", "https://twitter.com/ZorutanMesuta" }
		};

        public struct Contributors
        {
            public Contributors(string artist, string designer) {
                Artist = artist;
                Designer = designer;
            }
            public string Artist;
            public string Designer;
        }
        public static SortedDictionary<string, Contributors> contributorsData = new SortedDictionary<string, Contributors>();
        public static List<string> namesAddedToContributorDictionary = new List<string>();
        public static List<string> enchantmentsLocalization = new List<string>();
        public static SortedDictionary<int, List<(float, List<WeightedPair>)>> npcEnchantmentDrops = new();
	    private static string localization = "";
	    private static int tabs = 0;
	    private static List<string> labels;
        private static Dictionary<string, ModTranslation> translations;
        private static int culture;

        //Only used to print the full list of enchantment tooltips in WEPlayer OnEnterWorld()  (Normally commented out there)
        //public static string listOfAllEnchantmentTooltips = "";

        //Requires an input type to have properties: Texture
        public static void UpdateContributorsList<T>(T modTypeWithTexture, string sharedName = null) {
            if (!printListOfContributors)
                return;

            //Already added
            if (sharedName != null && namesAddedToContributorDictionary.Contains(sharedName))
                return;

            Type thisObjectsType = modTypeWithTexture.GetType();
            string texture = (string)thisObjectsType.GetProperty("Texture").GetValue(modTypeWithTexture);
            string artist = (string)thisObjectsType.GetProperty("Artist").GetValue(modTypeWithTexture);
            string designer = (string)thisObjectsType.GetProperty("Designer").GetValue(modTypeWithTexture);

            if (!contributorsData.ContainsKey(texture))
                contributorsData.Add(texture, new Contributors(artist, designer));

            if (sharedName != null)
                namesAddedToContributorDictionary.Add(sharedName);
        }
        private static void PrintAllLocalization() {
            if (!printLocalization)
                return;

            Mod mod = ModContent.GetInstance<WEMod>();
            TmodFile file = (TmodFile)typeof(Mod).GetProperty("File", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(mod);
            translations = new();
            Autoload(file);
            /*foreach (int i in Enum.GetValues(typeof(CultureName)).Cast<CultureName>().Where(n => n != CultureName.Unknown).Select(n => (int)n)) {
                foreach(string key in translations.Keys) {
                    $"{key}: {translations[key].GetTranslation(i)}".Log();
				}
            }*/
            foreach (int i in Enum.GetValues(typeof(CultureName)).Cast<CultureName>().Where(n => n != CultureName.Unknown).Select(n => (int)n)) {
                PrintLocalization((CultureName)i);
            }
        }
        private static void Autoload(TmodFile file) {
            var modTranslationDictionary = new Dictionary<string, ModTranslation>();

            AutoloadTranslations(file, modTranslationDictionary);

            foreach (var value in modTranslationDictionary.Values) {
                AddTranslation(value);
            }
        }
        public static void AddTranslation(ModTranslation translation) {
            translations[translation.Key] = translation;
        }
        private static void AutoloadTranslations(TmodFile file, Dictionary<string, ModTranslation> modTranslationDictionary) {
            
            foreach (var translationFile in file.Where(entry => Path.GetExtension(entry.Name) == ".hjson")) {
                using var stream = file.GetStream(translationFile);
                using var streamReader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

                string translationFileContents = streamReader.ReadToEnd();

                var culture = GameCulture.FromPath(translationFile.Name);

                // Parse HJSON and convert to standard JSON
                string jsonString = HjsonValue.Parse(translationFileContents).ToString();

                // Parse JSON
                var jsonObject = JObject.Parse(jsonString);
                // Flatten JSON into dot seperated key and value
                var flattened = new Dictionary<string, string>();

                foreach (JToken t in jsonObject.SelectTokens("$..*")) {
                    if (t.HasValues) {
                        continue;
                    }

                    // Custom implementation of Path to allow "x.y" keys
                    string path = "";
                    JToken current = t;

                    for (JToken parent = t.Parent; parent != null; parent = parent.Parent) {
                        path = parent switch {
                            JProperty property => property.Name + (path == string.Empty ? string.Empty : "." + path),
                            JArray array => array.IndexOf(current) + (path == string.Empty ? string.Empty : "." + path),
                            _ => path
                        };
                        current = parent;
                    }

                    flattened.Add(path, t.ToString());
                }

                foreach (var (key, value) in flattened) {
                    string effectiveKey = key.Replace(".$parentVal", "");
                    if (!modTranslationDictionary.TryGetValue(effectiveKey, out ModTranslation mt)) {
                        // removing instances of .$parentVal is an easy way to make this special key assign its value
                        //  to the parent key instead (needed for some cases of .lang -> .hjson auto-conversion)
                        modTranslationDictionary[effectiveKey] = mt = LocalizationLoader.CreateTranslation(effectiveKey);
                    }

                    mt.AddTranslation(culture, value);
                }
            }
        }
        public static void PrintLocalization(CultureName cultureName) {
	        Start(cultureName);
	    
	        AddLabel("ItemName");
            IEnumerable<ModItem> modItems = ModContent.GetInstance<WEMod>().GetContent<ModItem>();
	        List<string> enchantmentNames = new();
	        foreach (Enchantment enchantment in modItems.OfType<Enchantment>()) {
	    	    enchantmentNames.Add(enchantment.Name);
		    if (enchantment.EnchantmentTier >= 3)
			    enchantmentNames.Add(enchantment.EnchantmentTypeName + "Enchantment" + EnchantingRarity.displayTierNames[enchantment.EnchantmentTier]);
	        }
	    
	        enchantmentNames.Sort();
	        GetLocalizationFromList(null, enchantmentNames);
	    
                var modItemLists = modItems
	    	    .Where(mi => mi is not Enchantment)
	    	    .GroupBy(mi => mi is Enchantment ? mi.GetType().BaseType.BaseType.Name : mi.GetType().BaseType.Name)
		    .Select(mi => new { Key = mi.GetType().BaseType.Name, ModItemList = mi})
		    .OrderBy(group => group.Key);
            
                foreach (var list in modItemLists) {
                    GetLocalizationFromList(null, list.ModItemList);
                }
	        Close();
	    
	        FromLocalizationData();
	    
	        End();
        }
	    private static void FromLocalizationData() {
		    SortedDictionary<string, SData> all = LocalizationData.All;
		    GetFromSDataDict(all);
	    }
	    private static void GetFromSDataDict(SortedDictionary<string, SData> dict) {
            foreach (KeyValuePair<string, SData> pair in dict) {
                AddLabel(pair.Key);
                if (LocalizationData.autoFill.Contains(pair.Key))
                    AutoFill(pair);

                GetFromSData(pair.Value);
                Close();
            }
	    }
	    private static void GetFromSData(SData d) {
		    if (d.Values != null)
			    GetLocalizationFromList(null, d.Values);
		
		    if (d.Dict != null)
			    GetLocalizationFromDict(null, d.Dict);
		
		    if (d.Children != null)
			    GetFromSDataDict(d.Children);
	    }
	    private static void AutoFill(KeyValuePair<string, SData> pair) {
            IEnumerable<Type> types = null;
            try {
                types = AssemblyManager.GetLoadableTypes(Assembly.GetExecutingAssembly());
            }
            catch (ReflectionTypeLoadException e) {
                types = e.Types.Where(t => t != null);
            }

            List<string> list = types.Where(t => t.GetType() == Type.GetType(pair.Key))
			    .Where(t => !t.IsAbstract)
			    .Select(t => t.Name)
			    .ToList();
            SortedDictionary<string, string> dict = pair.Value.Dict;
		    foreach(string s in list) {
			    if(!dict.ContainsKey(s))
				    dict.Add(s, s.AddSpaces());
		    }
	    }
	    private static void AddLabel(string label) {
		    localization += Tabs(tabs) + label + ": {\n";
		    tabs++;
		    labels.Add(label);
	    }
	    private static void Start(CultureName cultureName) {
            culture = (int)cultureName;
            localization += "\n\n" + cultureName.ToString() + "\n";
		    labels = new();
		    AddLabel("Mods");
		    AddLabel("WeaponEnchantments");
	    }
	    private static void Close() {
		    tabs--;
            if (tabs < 0)
                return;

		    localization += Tabs(tabs) + "}\n";
			
		    labels.RemoveAt(labels.Count - 1);
	    }
	    private static void End() {
		    while(tabs >= 0) {
			    Close();
		    }
		
		    tabs = 0;
		    localization.Log();
		    localization = "";
	    }
        private static void GetLocalizationFromList(string label, IEnumerable<ModType> list, bool ignoreLabel = false, bool printMaster = false) {
            IEnumerable<string> listNames = list.Select(l => l.Name);
            GetLocalizationFromList(label, listNames, ignoreLabel, printMaster);
        }
        private static void GetLocalizationFromList(string label, IEnumerable<string> list, bool ignoreLabel = false, bool printMaster = false) {
            SortedDictionary<string, string> dict = new();
            foreach (string s in list) {
                //$"{s}: {s.AddSpaces()}".Log();
				dict.Add($"{s}", $"{s.AddSpaces()}");
			}

            GetLocalizationFromDict(label, dict, ignoreLabel, printMaster);
		}
	    private static void GetLocalizationFromDict(string label, SortedDictionary<string, string> dict, bool ignoreLabel = false, bool printMaster = false) {
            ignoreLabel = ignoreLabel || label == null || label == "";
	        if (!ignoreLabel)
	    	    AddLabel(label);

            string tabString = Tabs(tabs);
		    string allLabels = string.Join(".", labels.ToArray());
            foreach (KeyValuePair<string, string> p in dict) {
                string key = allLabels + "." + p.Key;
                string s = translations.ContainsKey(key) ? translations[key].GetTranslation(culture) : key;
                //$"{key}: {s}".Log();
                if (s == key) {
                    s = p.Value;
                }

                s = CheckTabOutLocalization(s);

                localization += tabString + p.Key + ": " + (printMaster ? "" : s) + "\n";
            }

            if (!ignoreLabel)
	    	Close();
        }
        private static void GetLocalizationFromListAddToEnd(string label, IEnumerable<string> list, string addString, int tabsNum) {
            List<string> newList = ListAddToEnd(list, addString);
            GetLocalizationFromList(label, newList);
		}
        private static List<string> ListAddToEnd(IEnumerable<string> iEnumerable, string addString) {
            List<string> list = iEnumerable.ToList();
            for(int i = 0; i < list.Count; i++) {
                list[i] += addString;
			}

            return list;
		}
        private static string Tabs(int num) => num > 0 ? new string('\t', num) : "";
        private static string CheckTabOutLocalization(string s) {
            if (s.Contains("'''"))
                return s;

            if (!s.Contains('\n'))
                return s;

            tabs++;
            string newString = $"\n{Tabs(tabs)}'''\n";
            int start = 0;
            int i = 0;
            for (; i < s.Length; i++) {
                if (s[i] == '\n') {
                    newString += Tabs(tabs) + s.Substring(start, i - start + 1);
                    start = i + 1;
                }
            }

            if (s[s.Length - 1] != '\n') {
                newString += Tabs(tabs) + s.Substring(start, i - start);
            }

            newString += $"\n{Tabs(tabs)}'''";
            tabs--;

            return newString;
		}
        public override void OnWorldLoad() {
            PrintListOfEnchantmentTooltips();

            //Contributors  change to give exact file location when added to contributor.
            PrintContributorsList();

            PrintAllLocalization();

	        PrintEnchantmentDrops();
        }
        private static void PrintListOfEnchantmentTooltips() {
		if (!printListOfEnchantmentTooltips)
			return;
	
            string tooltipList = "";
            foreach(Enchantment e in ModContent.GetContent<ModItem>().OfType<Enchantment>()) {
                tooltipList += $"\n\n{e.Name}";
                IEnumerable<string> tooltips = e.GenerateFullTooltip().Select(t => t.Item1);
                foreach(string tooltip in tooltips) {
                    tooltipList += $"\n{tooltip}";
                }
			}

            tooltipList.Log();
        }
        private static void PrintContributorsList() {
            if (!printListOfContributors)
                return;

            if (contributorsData.Count <= 0)
                return;
            
            //New dictionary with artist names as the key
            SortedDictionary<string, List<string>> artistCredits = new SortedDictionary<string, List<string>>();
            foreach (string key in contributorsData.Keys) {
                string artistName = contributorsData[key].Artist;
                if (artistName != null) {
                    if (artistCredits.ContainsKey(artistName)) {
                        artistCredits[artistName].Add(key);
                    }
                    else {
                        artistCredits.Add(artistName, new List<string>() { key });
                    }
                }
            }

            //Create and print the GitHub Artist credits.
            string artistsMessage = "";
            foreach (string artistName in artistCredits.Keys) {
                artistsMessage += $"\n{artistName}: ";
                if (contributorLinks.ContainsKey(artistName))
                    artistsMessage += contributorLinks[artistName];

                artistsMessage += "\n\n";
                foreach (string texture in artistCredits[artistName]) {
                    artistsMessage += $"![{texture.GetFileName('/')}]({texture.RemoveFirstFolder('/', false)}.png)\n";
                }
            }
            artistsMessage.Log();

            namesAddedToContributorDictionary.Clear();
            contributorsData.Clear();
        }
        private static void PrintEnchantmentDrops() {
		if (!printEnchantmentDrops)
			return;
            string log = "\n";
            foreach(KeyValuePair<int, List<(float, List<WeightedPair>)>> npc in npcEnchantmentDrops) {
                string name = ContentSamples.NpcsByNetId[npc.Key].TypeName;
                foreach((float, List<WeightedPair>) enchantmentGroup in npc.Value) {
                    float total = 0f;
                    foreach(WeightedPair pair in enchantmentGroup.Item2) {
                        total += pair.Weight;
                    }

                    foreach(WeightedPair pair in enchantmentGroup.Item2) {
                        Item sampleItem = ContentSamples.ItemsByType[pair.ID];
                        if (sampleItem.ModItem is not Enchantment enchantment)
                            continue;

                        log += $"\n{name} ({npc.Key}),";
                        float chance = enchantmentGroup.Item1 * pair.Weight / total;

                        log += $"{enchantment.EnchantmentTypeName.AddSpaces()},{chance.PercentString()}";
                    }
                }
			}

            foreach(ChestID chestID in Enum.GetValues(typeof(ChestID)).Cast<ChestID>().ToList().Where(c => c != ChestID.None)) {
                WEModSystem.GetChestLoot(chestID, out List<WeightedPair> pairs, out float baseChance);
                if (pairs == null)
                    continue;

                string name = chestID.ToString() + " Chest";
                float total = 0f;
                foreach(WeightedPair pair in pairs) {
                    total += pair.Weight;
				}

                foreach(WeightedPair pair in pairs) {
                    Item sampleItem = ContentSamples.ItemsByType[pair.ID];
                    if (sampleItem.ModItem is not Enchantment enchantment)
                        continue;

                    log += $"\n{name},";
                    float chance = baseChance * pair.Weight / total;

                    log += $"{enchantment.EnchantmentTypeName.AddSpaces()},{chance.PercentString()}";
                }
			}

            foreach(KeyValuePair<int, List<WeightedPair>> crate in GlobalCrates.crateDrops) {
                string name = ((CrateID)crate.Key).ToString() + " Crate";
                float total = 0f;
                foreach(WeightedPair pair in crate.Value) {
                    total += pair.Weight;
				}

                foreach(WeightedPair pair in crate.Value) {
                    Item sampleItem = ContentSamples.ItemsByType[pair.ID];
                    if (sampleItem.ModItem is not Enchantment enchantment)
                        continue;

                    log += $"\n{name} ({crate.Key}),";
                    float baseChance = GlobalCrates.GetCrateEnchantmentDropChance(crate.Key);
                    float chance = baseChance * pair.Weight / total;

                    log += $"{enchantment.EnchantmentTypeName.AddSpaces()},{chance.PercentString()}";
                }
			}

            log.Log();
		}
    }
}
