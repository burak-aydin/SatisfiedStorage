﻿using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Verse;


namespace SatisfiedStorage
{


    [HarmonyPatch(typeof(ThingFilterUI), nameof(ThingFilterUI.DoThingFilterConfigWindow))]
    class HaulingHysteresis_InjectControls
    {

        private const float HysteresisHeight = 30f;
        private const float HysteresisBlockHeight = 35f;

        internal static volatile int showHysteresisCount;

        private static Queue<StorageSettings> _settingsQueue = new Queue<StorageSettings>();

        internal static Queue<StorageSettings> SettingsQueue => _settingsQueue;

        [HarmonyPrefix]
        public static void Before_DoThingFilterConfigWindow(ref object __state, ref Rect rect)
        {
            bool showHysteresis = (showHysteresisCount-- > 0) && _settingsQueue.Count != 0;
            showHysteresisCount = Math.Max(0, showHysteresisCount);

            if (showHysteresis)
            {
                DoHysteresisBlock(new Rect(0f, rect.yMax - HysteresisHeight, rect.width, HysteresisHeight), _settingsQueue.Dequeue());
                rect = new Rect(rect.x, rect.y, rect.width, rect.height - HysteresisBlockHeight);
            }
        }
        private static void DoHysteresisBlock(Rect rect, StorageSettings settings)
        {

            Hysteresis storageSettings_Hysteresis = Mapping.Get(settings) ?? new Hysteresis();

            storageSettings_Hysteresis.FillPercent = Widgets.HorizontalSlider(rect.LeftPart(0.82f), storageSettings_Hysteresis.FillPercent, 0f, 100f, true, "Refill cells less than");

            rect = new Rect(rect.x, rect.y, rect.width, rect.height);
            Widgets.Label(rect.RightPart(0.15f).BottomPart(0.72f), storageSettings_Hysteresis.FillPercent.ToString("N0") + "%");

            Mapping.Set(settings, storageSettings_Hysteresis);
        }
    }



    [HarmonyPatch(typeof(Listing_TreeThingFilter), "DoCategoryChildren")]
    static class ThingFilter_InjectFilter
    {
        private static readonly Queue<Func<TreeNode_ThingCategory, TreeNode_ThingCategory>> projections = new Queue<Func<TreeNode_ThingCategory, TreeNode_ThingCategory>>();

        internal static Queue<Func<TreeNode_ThingCategory, TreeNode_ThingCategory>> Projections => projections;

        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Harmony patch method")]
        public static void Before_DoCategoryChildren(ref TreeNode_ThingCategory node, int indentLevel, int openMask, Map map, bool subtreeMatchedSearch)
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
            Hysteresis storageSettings_Hysteresis = Mapping.Get(__instance);
            Scribe_Deep.Look<Hysteresis>(ref storageSettings_Hysteresis, "hysteresis", new object[0]);
            bool flag = storageSettings_Hysteresis != null;
            if (flag)
            {
                Mapping.Set(__instance, storageSettings_Hysteresis);
            }
        }
    }

    [HarmonyPatch(typeof(StorageSettings), nameof(StorageSettings.CopyFrom))]
    public class StorageSettings_CopyFrom
    {

        [HarmonyPostfix]
        public static void CopyFrom(StorageSettings __instance, StorageSettings other)
        {
            Hysteresis storageSettings_Hysteresis = Mapping.Get(other);
            bool flag = storageSettings_Hysteresis != null;
            if (flag)
            {
                Mapping.Set(__instance, storageSettings_Hysteresis);
            }
        }
    }

    [HarmonyPatch(typeof(StoreUtility), "NoStorageBlockersIn")]
    internal class StoreUtility_NoStorageBlockersIn
    {
        //Some storage mods allow more than one thing in a cell.  If they do, we need to do
        //more of a check to see if the threshold has been met; we only check if we need to:
        //public static bool checkIHoldMultipleThings = false;

        [HarmonyPrefix]
        public static bool NoStorageBlockersPrefix(ref bool __result, IntVec3 c, Map map, Thing thing)
        {

            List<Thing> list = map.thingGrid.ThingsListAt(c);
            bool flag = false;
            int items = 0;
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing2 = list[i];
                bool canStackWith = thing2.CanStackWith(thing);
                if (canStackWith)
                {
                    items += thing2.stackCount;
                }

                if (!flag && thing2.def.EverStorable(false) && canStackWith && thing2.stackCount < thing2.def.stackLimit)
                {
                    flag = true;
                }
                if (thing2.def.entityDefToBuild != null && thing2.def.entityDefToBuild.passability != Traversability.Standable)
                {
                    __result = false;
                    return false;
                }
                if (thing2.def.surfaceType == SurfaceType.None && thing2.def.passability != Traversability.Standable && (c.GetMaxItemsAllowedInCell(map) <= 1 || thing2.def.category != ThingCategory.Item))
                {
                    __result = false;
                    return false;
                }
            }

            int maxItemsAllowedInCell = c.GetMaxItemsAllowedInCell(map);
            bool rv = flag || c.GetItemCount(map) < maxItemsAllowedInCell;

            if (rv)
            {
                float num = 100f;
                SlotGroup slotGroup = c.GetSlotGroup(map);

                flag = slotGroup != null && slotGroup.Settings != null;
                if (flag)
                {
                    num = Mapping.Get(slotGroup.Settings).FillPercent;
                }

                //TODO: LWM.DeepStorage should not need anything here anymore, but check

                //Log.Message(thing.def.defName + ":" + items + "<" + (maxItemsAllowedInCell * thing.def.stackLimit * (num / 100f)));
                rv &= items < maxItemsAllowedInCell * thing.def.stackLimit * (num / 100f);
            }

            __result = rv;
            return false;
        }

    }

    [HarmonyPatch(typeof(ITab_Storage), "FillTab")]
    public class ITab_Storage_FillTab
    {

        private static Func<RimWorld.ITab_Storage, IStoreSettingsParent> GetSelStoreSettingsParent;


        static ITab_Storage_FillTab()
        {
            GetSelStoreSettingsParent = AccessManager.GetPropertyGetter<RimWorld.ITab_Storage, IStoreSettingsParent>("SelStoreSettingsParent");
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


/*
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
                    // NOTE: Other storage mods may not be comp-based.  If one ever starts causing
                    //   problems with this mod, the logic here can be updated to include checking
                    //   whether the storage building itself is IHoldMultipleThings
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

				Building edifice = c.GetEdifice(map);
				if (edifice != null)
				{


					__result &= !map.thingGrid.ThingsListAt(c).Any(t => {
						Log.Message("scount:" + t.stackCount + "	mitems:" + edifice.MaxItemsInCell * t.def.stackLimit * (num / 100f));
						return t.def.EverStorable(false) && t.stackCount >= edifice.MaxItemsInCell * t.def.stackLimit * (num / 100f);
						});

				}


			}
        }
*/
