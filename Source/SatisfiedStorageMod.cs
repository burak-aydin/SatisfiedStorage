using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using RimWorld.Planet;

namespace SatisfiedStorage
{
    
    
    public class SatisfiedStorageMod : Mod
    {
        public static bool DeepStorageCOMP = true;
        public static MethodInfo methodcapacityat = null;
        public static MethodInfo methodcapacitytostorethingat = null;
        public static Type _comptype = null;

        public SatisfiedStorageMod(ModContentPack content) : base(content)
        {
            Log.Message("SatisfiedStorage loading");
            Harmony harmonyInstance = new Harmony("SatisfiedStorageMod");
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());              // just use all [HarmonyPatch] decorated classes       



            //check if we LWM IS active
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().ToList<Assembly>().Find((Assembly x) => x.FullName.Split(new char[] { ',' }).First<string>() == "LWM.DeepStorage");

            _comptype = assembly.GetType("LWM.DeepStorage.CompDeepStorage");

            if (_comptype != null)
            {
                Log.Message("SatisfiedStorage :: LWM DETECTED, TRYING TO BE COMPATIBLE");

                SatisfiedStorageMod.DeepStorageCOMP = true;
                methodcapacityat = AccessTools.Method(_comptype, "CapacityAt", null, null);
                methodcapacitytostorethingat = AccessTools.Method(_comptype, "CapacityToStoreThingAt", null, null);
            }

        }
    }    

}
