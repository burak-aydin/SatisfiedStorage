using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using IHoldMultipleThings;

namespace SatisfiedStorage
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

    [HarmonyPatch(typeof(StorageSettings), nameof(StorageSettings.CopyFrom))]
    public class StorageSettings_CopyFrom
    {

        [HarmonyPostfix]
        public static void CopyFrom(StorageSettings __instance, StorageSettings other)
        {
            StorageSettings_Hysteresis storageSettings_Hysteresis = StorageSettings_Mapping.Get(other);
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
        // Some storage mods allow more than one thing in a cell.  If they do, we need to do
        //   more of a check to see if the threshold has been met; we only check if we need to:
        public static bool checkIHoldMultipleThings=false;
        public static bool Prepare() {
            if (ModLister.GetActiveModWithIdentifier("LWM.DeepStorage")!=null) {
                checkIHoldMultipleThings=true;
                Log.Message("SatisfiedStorage _ Activating compatibility for LWM.DeepStorage");
            }
            //  If other storage mods don't work, add the test here:
            return true;
        }
        [HarmonyPostfix]
        public static void NoStorageBlockersInPost(ref bool __result, IntVec3 c, Map map, Thing thing)
        {
            //FALSE IF ITS TOO FULL
            //TRUE IF THERE IS EMPTY SPACE

            //we dont make empty space so if its full then we dont care
            if (__result)
            {
                float num = 100f;
                SlotGroup slotGroup=c.GetSlotGroup(map);
                
                bool flag = slotGroup != null && slotGroup.Settings != null;
                if (flag)
                {
                    num = StorageSettings_Mapping.Get(slotGroup.Settings).FillPercent;
                }

                //LWM.DeepStorage
                if (checkIHoldMultipleThings) {

                    foreach(Thing thisthing in map.thingGrid.ThingsListAt(c))
                    {
                        ThingWithComps th = thisthing as ThingWithComps;
                        if (th == null) continue;
                        var allComps = th.AllComps;

                        if (allComps != null)
                        {
                            foreach (var comp in allComps)
                            {
                                if (comp is IHoldMultipleThings.IHoldMultipleThings)
                                {
                                    int capacity = 0;
                                    IHoldMultipleThings.IHoldMultipleThings thiscomp = (IHoldMultipleThings.IHoldMultipleThings)comp;

                                    thiscomp.CapacityAt(thing, c, map, out capacity);
                                    // if total capacity is larger than the stackLimit (full stack available)
                                    //    Allow hauling (other choices are valid)
                                    // if (capacity > thing.def.stackLimit) return true;
                                    // only haul if count is below threshold
                                    //   which is equivalent to availability being above threshold:
                                    //            Log.Message("capacity = " + capacity);
                                    //            Log.Message("thing.def.stackLimit = " +thing.def.stackLimit);
                                    float var = (100f * (float)capacity / thing.def.stackLimit);

                                    //100 - num is necessary because capacity gives empty space not full space
                                    __result = var > (100 - num);
                                    //      if (__result == false){
                                    //          Log.Message("ITS TOO FULL stop yey");
                                    //      }
                                    return;
                                }
                            }
                        }

                    }
                    
                }

                // mod check:
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
