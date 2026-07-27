using System;
using System.Linq;
using HarmonyLib;
using Il2Cpp;
using Il2CppSystem.Collections.Generic;

namespace QoLFireStart;

internal class Panel_FireStart_Patches
{
    delegate int CompareFunc(GearItem a, GearItem b);

    [HarmonyPatch(typeof(Panel_FireStart), "Enable")]
    internal class Panel_FireStart_Update
    {
        private static Panel_FireStart _instance;

        // Prefer 'free' starter. Torch and flare only counts when lit
        private static string[] BestStarters = new string[]
        {
            "GEAR_Torch",
            "GEAR_FlareA",
            "GEAR_BlueFlare",
            "GEAR_MagnifyingLens",
        };

        // Exclude items that can be used another way
        private static string[] WorstTinders = new string[]
        {
            "GEAR_BarkTinder",
        };

        // Exclude items that can be used another way
        private static string[] WorstFuels = new string[]
        {
            "GEAR_Torch",
        };
        
        private static void Postfix(
            Panel_FireStart __instance, bool enable)
        {
            _instance = __instance;
            if (enable)
            {
                _instance.m_SelectedStarterIndex = SelectBest(_instance.m_StarterList, _instance.m_SelectedStarterIndex, ComparisonFirestarter);
                _instance.m_SelectedTinderIndex = SelectBest(_instance.m_TinderList, _instance.m_SelectedTinderIndex, ComparisonTinder);
                _instance.m_SelectedFuelIndex = SelectBest(_instance.m_FuelList, _instance.m_SelectedFuelIndex, ComparisonFuel);
#if DEBUG
                MelonLoader.MelonLogger.Msg($"Starters:");
                LogOrder(_instance.m_StarterList, ComparisonFirestarter);
                MelonLoader.MelonLogger.Msg($"Tinders:");
                LogOrder(_instance.m_TinderList, ComparisonTinder);
                MelonLoader.MelonLogger.Msg($"Fuels:");
                LogOrder(_instance.m_FuelList, ComparisonFuel);
#endif
            }
        }
        
        private static void LogOrder(List<GearItem> gearList, Comparison<GearItem> comparison)
        {
#if DEBUG
            var list = new System.Collections.Generic.List<GearItem>();
            for (var i = 0; i < gearList.Count; i++)
            {
                list.Add(gearList[i]);
            }
            
            list.Sort(comparison);

            for (var i = 0; i < list.Count; i++)
            {
                var gear = list[i];
                if (gear == null)
                {
                    MelonLoader.MelonLogger.Msg($"null");
                    continue;
                }
                
                var starterChance = gear?.m_FireStarterItem?.m_FireStartSkillModifier.ToString("N0") ?? "null";
                var starterTime = gear?.m_FireStarterItem?.m_FireStartDurationModifier.ToString("N0") ?? "null";
                
                var fuelChance = gear?.m_FuelSourceItem?.m_FireStartSkillModifier.ToString("N0") ?? "null";
                var fuelTime = gear?.m_FuelSourceItem?.m_FireStartDurationModifier.ToString("N0") ?? "null";
                
                var weight = (gear.WeightKG.m_Units / 1000000f).ToString("N0");
                var fuelDuration = gear.m_FuelSourceItem?.m_BurnDurationHours.ToString("N2");
                var fuelQuality = (gear.m_FuelSourceItem?.m_BurnDurationHours ?? 0) / (gear.WeightKG.m_Units/ 1000000000f);
                
                var condition = gear.CurrentHP;
                
                MelonLoader.MelonLogger.Msg(
                    $"{gear.name}/{gear.DisplayName}: {starterTime}, {starterChance}% / {fuelTime}, {fuelChance}% / {weight}g, {condition}, {fuelDuration}h, {fuelQuality}");
            }
            
            MelonLoader.MelonLogger.Msg($"");
#endif
        }

        private static int SelectBest(List<GearItem> list, int startIndex,
            Comparison<GearItem> comparison)
        {
            var index = startIndex;
            if (index >= list.Count)
            {
                index = 0;
            }
            
            var selectedGear = list[index];
            for (var i = 0; i < list.Count; i++)
            {
                var gear = list[i];
                if (comparison(selectedGear, gear) > 0)
                {
                    index = i;
                    selectedGear = gear;
                }
            }

            return index;
        }

        private static int ComparisonFirestarter(GearItem a, GearItem b)
        {
            var aFireChance = (int)a.m_FireStarterItem.m_FireStartSkillModifier;
            var aFireTime = -(int)a.m_FireStarterItem.m_FireStartDurationModifier;
            var aAvailable = (a.m_FireStarterItem == null || !a.m_FireStarterItem.m_RequiresSunLight ||
                              _instance.HasDirectSunlight()) ? 1 : 0;
            var aFavor = BestStarters.Contains(a.name) ? 1 : 0;
            var bFireChance = (int)b.m_FireStarterItem.m_FireStartSkillModifier;
            var bFireTime = -(int)b.m_FireStarterItem.m_FireStartDurationModifier;
            var bAvailable = (b.m_FireStarterItem == null || !b.m_FireStarterItem.m_RequiresSunLight ||
                              _instance.HasDirectSunlight()) ? 1 : 0;
            var bFavor = BestStarters.Contains(b.name) ? 1 : 0;
            return (bAvailable - aAvailable) * 100000   // Sort out Magnifying lens when it is unavailable
                   + (bFavor - aFavor) * 10000          // Prefer 'free' starters
                   + (bFireChance - aFireChance) * 100  // Then prefer highest chance
                   + (bFireTime - aFireTime);           // Then prefer shortest start (torch better than magnifying lens)
        }

        private static int ComparisonTinder(GearItem a, GearItem b)
        {
            if (a == null)
            {
                return -1;
            }
            
            if (b == null)
            {
                return 1;
            }
            
            var nameDiff = 0;
            var i = 0;
            while (i < a.name.Length && i < b.name.Length && Math.Abs(nameDiff) < 100000)
            {
                nameDiff = nameDiff * 100 + b.name[i] - a.name[i];
                ++i;
            }
            
            var aWorst = WorstTinders.Contains(a.name) ? 1 : 0;
            var bWorst = WorstTinders.Contains(b.name) ? 1 : 0;

            var aWeigth = (int)a.WeightKG.m_Units;
            var bWeigth = (int)b.WeightKG.m_Units;
            
            return (aWorst - bWorst) * 1000000000       // Sort out birchbark
                   + (aWeigth - bWeigth) + nameDiff;    // Then prefer lowest weight (tinder over paper)
        }

        private static int ComparisonFuel(GearItem a, GearItem b)
        {
            if (a == null)
            {
                return -1;
            }
            
            if (b == null)
            {
                return 1;
            }
            
            var aIncomplete = (a.m_ResearchItem != null && !a.m_ResearchItem.IsResearchComplete());
            var bIncomplete = (b.m_ResearchItem != null && !b.m_ResearchItem.IsResearchComplete());
            if (aIncomplete != bIncomplete)
            {
                return aIncomplete ? 1 : -1;
            }

            var aChance = (int)a.m_FuelSourceItem.m_FireStartSkillModifier;
            var aTime = -(int)a.m_FuelSourceItem.m_FireStartDurationModifier;
            var aRatio = (int)(10 * a.m_FuelSourceItem.m_BurnDurationHours / (a.WeightKG.m_Units/ 1000000000f));
            var aWorst = WorstFuels.Contains(a.name) ? 1 : 0;
            var aCondition = (int)a.CurrentHP;
            
            var bChance = (int)b.m_FuelSourceItem.m_FireStartSkillModifier;
            var bTime = -(int)b.m_FuelSourceItem.m_FireStartDurationModifier;
            var bRatio = (int)(10 * b.m_FuelSourceItem.m_BurnDurationHours / (b.WeightKG.m_Units/ 1000000000f));
            var bWorst = WorstFuels.Contains(b.name) ? 1 : 0;
            var bCondition = (int)b.CurrentHP;
            return (bChance - aChance) * 10000000           // Prefer best chance
                   + (bTime - aTime) * 100000               // Then best start duration
                   + (aWorst - bWorst) * 10000              // Then sort out torch
                   + (aCondition - bCondition) * 100        // Then by condition (torch)
                   + (aRatio - bRatio);                     // Then lowest quality (stick over cedar wood)
        }
    }
}