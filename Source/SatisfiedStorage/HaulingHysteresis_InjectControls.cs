using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace HaulingHysterisis
{

    [HarmonyPatch(typeof(ThingFilterUI), nameof(ThingFilterUI.DoThingFilterConfigWindow))]
    class HaulingHysteresis_InjectControls {

        private const float HysteresisHeight = 30f;
        private const float HysteresisBlockHeight = 35f;

        internal static volatile int showHysteresisCount;

        private static Queue<StorageSettings> _settingsQueue = new Queue<StorageSettings>();

        internal static Queue<StorageSettings> SettingsQueue => _settingsQueue;

        [HarmonyPrefix]
        public static void Before_DoThingFilterConfigWindow(ref object __state, ref Rect rect) {
            Log.Message("hh patch");
            bool showHysteresis = (showHysteresisCount-- > 0) && _settingsQueue.Count != 0;
            showHysteresisCount = Math.Max(0, showHysteresisCount);

            if (showHysteresis)
            {                
                DoHysteresisBlock(new Rect(0f, rect.yMax - HysteresisHeight, rect.width, HysteresisHeight), _settingsQueue.Dequeue());
                rect= new Rect(rect.x, rect.y, rect.width, rect.height - HysteresisBlockHeight);            
            }
        }        

        private static void DoHysteresisBlock(Rect rect, StorageSettings settings) {

            StorageSettings_Hysteresis storageSettings_Hysteresis = StorageSettings_Mapping.Get(settings) ?? new StorageSettings_Hysteresis();

            storageSettings_Hysteresis.FillPercent = Widgets.HorizontalSlider(rect.LeftPart(0.8f), storageSettings_Hysteresis.FillPercent, 0f, 100f, false, "Refill cells less than");
            Widgets.Label(rect.RightPart(0.2f), storageSettings_Hysteresis.FillPercent.ToString("N0") + "%");

            StorageSettings_Mapping.Set(settings, storageSettings_Hysteresis);
        }        
    }


    [HarmonyPatch(typeof(Listing_TreeThingFilter), nameof(Listing_TreeThingFilter.DoCategoryChildren))]
    static class ThingFilter_InjectFilter
    {
        private static readonly Queue<Func<TreeNode_ThingCategory, TreeNode_ThingCategory>> projections = new Queue<Func<TreeNode_ThingCategory, TreeNode_ThingCategory>>();

        internal static Queue<Func<TreeNode_ThingCategory, TreeNode_ThingCategory>> Projections => projections;

        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Harmony patch method")]
        public static void Before_DoCategoryChildren(ref TreeNode_ThingCategory node)
        {
            if (projections.Count == 0)
                return;

            node = projections.Dequeue()(node);
        }
    }

    [HarmonyPatch(typeof(StorageSettings), nameof(StorageSettings.ExposeData))]
    public class StorageSettings_ExposeData
    {

        [HarmonyPostfix]
        public static void ExposeData(StorageSettings __instance)
        {
            StorageSettings_Hysteresis storageSettings_Hysteresis = StorageSettings_Mapping.Get(__instance);
            Scribe_Deep.Look<StorageSettings_Hysteresis>(ref storageSettings_Hysteresis, "hysteresis", new object[0]);
            bool flag = storageSettings_Hysteresis != null;
            if (flag)
            {
                StorageSettings_Mapping.Set(__instance, storageSettings_Hysteresis);
            }
        }
    }

    [HarmonyPatch(typeof(StoreUtility), "NoStorageBlockersIn")]
    internal class StoreUtility_NoStorageBlockersIn
    {

        [HarmonyPostfix]
        public static void FilledEnough(ref bool __result, IntVec3 c, Map map, Thing thing)
        {
            // if base implementation waves of, then don't need to care
            if (__result)
            {
                float num = 100f;
                bool flag = c.GetSlotGroup(map) != null && c.GetSlotGroup(map).Settings != null;
                if (flag)
                {
                    num = StorageSettings_Mapping.Get(c.GetSlotGroup(map).Settings).FillPercent;
                }

                __result &= !map.thingGrid.ThingsListAt(c).Any(t => t.def.EverStorable(false) && t.stackCount >= thing.def.stackLimit * (num / 100f));
            }
        }
    }

    [HarmonyPatch(typeof(ITab_Storage), "FillTab")]
    public class ITab_Storage_FillTab
    {

        private static Func<RimWorld.ITab_Storage, IStoreSettingsParent> GetSelStoreSettingsParent;


        static ITab_Storage_FillTab()
        {
            GetSelStoreSettingsParent = Access.GetPropertyGetter<RimWorld.ITab_Storage, IStoreSettingsParent>("SelStoreSettingsParent");
        }


        [HarmonyPrefix]
        public static void Before_ITab_Storage_FillTab(ITab_Storage __instance)
        {
            if (ReferenceEquals(__instance.GetType().Assembly, typeof(ITab_Storage).Assembly))
            {
                // only show hysteresis option for non derived (non-custom) storage(s)
                HaulingHysteresis_InjectControls.showHysteresisCount++;

                IStoreSettingsParent selStoreSettingsParent = GetSelStoreSettingsParent(__instance);
                HaulingHysteresis_InjectControls.SettingsQueue.Enqueue(selStoreSettingsParent.GetStoreSettings());
            }
        }
    }

}
