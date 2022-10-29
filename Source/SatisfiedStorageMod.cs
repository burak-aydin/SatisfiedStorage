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

        public SatisfiedStorageMod(ModContentPack content) : base(content)
        {
            Log.Message("SatisfiedStorage loading");
            Harmony harmonyInstance = new Harmony("SatisfiedStorageMod");

			/*
			if (ModLister.GetActiveModWithIdentifier("LWM.DeepStorage") != null)
			{
				StoreUtility_NoStorageBlockersIn.checkIHoldMultipleThings = true;
				Log.Message("SatisfiedStorage _ Activating compatibility for LWM.DeepStorage");
			}
			*/

			harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
		}

    }    

}
