﻿using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using WeaponEnchantments.Items;

namespace WeaponEnchantments.Common.Utility
{
	public static class StringManipulation
    {
        private static readonly char[] upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private static readonly char[] lowerCase = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
        private static readonly char[] numbers = "0123456789".ToCharArray();
        private static readonly string[] apla = { "abcdefghijklmnopqrstuvwxyz", "ABCDEFGHIJKLMNOPQRSTUVWKYZ" };

        #region S() Methods

        public static string S(this StatModifier statModifier) => "<A: " + statModifier.Additive + ", M: " + statModifier.Multiplicative + ", B: " + statModifier.Base + ", F: " + statModifier.Flat + ">";
        public static string S(this EStat eStat) => "<N: " + eStat.StatName + " A: " + eStat.Additive + ", M: " + eStat.Multiplicative + ", B: " + eStat.Base + ", F: " + eStat.Flat + ">";
        public static string S(this EnchantmentStaticStat staticStat) => "<N: " + staticStat.Name + " A: " + staticStat.Additive + ", M: " + staticStat.Multiplicative + ", B: " + staticStat.Base + ", F: " + staticStat.Flat + ">";
        public static string S(this Item item) => item != null ? !item.IsAir ? item.Name : "<Air>" : "null";
        public static string S(this Projectile projectile) => projectile != null ? projectile.Name : "null";
        public static string S(this Player player) => player != null ? player.name : "null";
        public static string S(this NPC npc, bool stats = false) => npc != null ? $"name: {npc.FullName} whoAmI: {npc.whoAmI}{(stats ? $"defense: {npc.defense}, defDefense: {npc.defDefense}, lifeMax: {npc.lifeMax}, life: {npc.life}" : "")}" : "null";
        public static string S(this Enchantment enchantment) => enchantment != null ? enchantment.Name : "null";
        public static string S(this Dictionary<int, int> dictionary, int key) => "contains " + key + ": " + dictionary.ContainsKey(key) + " count: " + dictionary.Count + (dictionary.ContainsKey(key) ? " value: " + dictionary[key] : "");
        public static string S(this Dictionary<string, StatModifier> dictionary, string key) => "contains " + key + ": " + dictionary.ContainsKey(key) + " count: " + dictionary.Count + (dictionary.ContainsKey(key) ? " value: " + dictionary[key].S() : "");
        public static string S(this Dictionary<string, EStat> dictionary, string key) => "contains " + key + ": " + dictionary.ContainsKey(key) + " count: " + dictionary.Count + (dictionary.ContainsKey(key) ? " value: " + dictionary[key].S() : "");
        public static string S(this bool b) => b ? "True" : "False";

        #endregion

        public static bool IsUpper(this char c) {
            foreach (char upper in upperCase) {
                if (upper == c)
                    return true;
            }
            return false;
        }
        public static bool IsLower(this char c) {
            foreach (char lower in lowerCase) {
                if (lower == c)
                    return true;
            }
            return false;
        }
        public static bool IsNumber(this char c) {
            foreach (char number in numbers) {
                if (number == c)
                    return true;
            }
            return false;
        }
        public static List<string> SplitString(this string s) {
            List<string> list = new List<string>();
            int start = 0;
            int end = 0;
            for (int i = 1; i < s.Length; i++) {
                if (s[i].IsUpper()) {
                    end = i - 1;
                    list.Add(s.Substring(start, end - start + 1));
                    start = end + 1;
                }
                else if (i == s.Length - 1) {
                    end = i;
                    list.Add(s.Substring(start, end - start + 1));
                }
            }
            return list;
        }
        public static string GetNameFolderName(this string s, int numberOfFolders = 1) {
            int i = s.Length - 1;
            for (int j = 0; j < numberOfFolders; j++) {
                i = s.FindChar('.', false);

                //Not last time loop will run
                if (j != numberOfFolders - 1) {
                    //Remove last folder from the string and continue the loop
                    s = s.Substring(0, i);
                }
            }

            return s.Substring(i + 1);
        }
        public static string GetFileName(this string s, char searchChar = '.') {
            int i = s.FindChar(searchChar, false);

            return s.Substring(i + 1);
        }
        public static int FindChar(this string s, char searchChar, bool startLeft = true) {
            int length = s.Length;
            int i = startLeft ? 0 : length - 1;
            if (startLeft) {
                for (; i < length; i++) {
                    char c = s[i];
                    if (c == searchChar) {
                        return i;
                    }
                }
            }
            else {
                for (; i >= 0; i--) {
                    char c = s[i];
                    if (c == searchChar) {
                        return i;
                    }
                }
            }
            return -1;
        }
        public static string RemoveNameSpace(this string s, char searchChar = '.', bool removeSearchChar = true) {
            int i = s.FindChar(searchChar);

            if (removeSearchChar)
                i++;

            return s.Substring(i);
        }
        public static string AddSpaces(this string s) {
            int start = 0;
            int end = 0;
            string finalString = "";
            for (int i = 1; i < s.Length; i++) {
                if (s[i].IsUpper() || s[i].IsNumber()) {
                    if (s[i - 1].IsUpper()) {
                        int j = 0;
                        while (i + j < s.Length - 1 && s[i + j].IsUpper()) {
                            j++;
                        }
                        i += j - 1;
                    }
                    else if (s[i - 1].IsNumber()) {
                        int j = 0;
                        while (i + j < s.Length - 1 && s[i + j].IsNumber()) {
                            j++;
                        }
                        i += j - 1;
                    }
                    end = i - 1;
                    finalString += s.Substring(start, end - start + 1) + " ";
                    start = end + 1;
                }
                else if (i == s.Length - 1) {
                    end = i;
                    finalString += s.Substring(start, end - start + 1);
                    start = -1;
                }
            }
            if (start != -1)
                finalString += s.Substring(start);
            return finalString;
        }
        public static string RemoveSpaces(this string s) {
            bool started = false;
            int start = 0;
            int end = 0;
            string finalString = "";
            for (int i = 0; i < s.Length; i++) {
                if (started) {
                    if (s[i] == ' ') {
                        started = false;
                        end = i;
                        finalString += s.Substring(start, end - start);
                    }
                }
                else {
                    if (s[i] != ' ') {
                        started = true;
                        start = i;
                    }
                }
            }
            if (started)
                finalString += s.Substring(start, s.Length - start);
            return finalString;
        }
        public static string CapitalizeFirst(this string s) {
            if (s.Length > 0) {
                if (s[0].IsLower())
                    for (int i = 0; i < apla[0].Length; i++) {
                        if (s[0] == apla[0][i]) {
                            char c = apla[1][i];
                            return c + s.Substring(1);
                        }
                    }
            }
            return s;
        }
        public static string ToFieldName(this string s) {
            if (s.Length > 0) {
                if (s[0].IsUpper())
                    for (int i = 0; i < apla[0].Length; i++) {
                        if (s[0] == apla[1][i]) {
                            char c = apla[0][i];
                            return c + s.Substring(1);
                        }
                    }
            }
            return s;
        }
        public static string RemoveProjectileName(this string s) {
            int i = s.IndexOf("ProjectileName.");
            return i == 0 ? s.Substring(15) : s;
        }
        public static int CheckMatches(this List<string> l1, List<string> l2) {
            int matches = 0;
            foreach (string s in l1) {
                foreach (string s2 in l2) {
                    if (s2.IndexOf(s) > -1) {
                        matches++;
                    }
                }
            }
            return matches;
        }
    }
}