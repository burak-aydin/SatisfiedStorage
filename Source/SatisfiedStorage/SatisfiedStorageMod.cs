using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SatisfiedStorage
{
    public class SatisfiedStorageMod : Mod
    {

        public SatisfiedStorageMod(ModContentPack content) : base(content)
        {
            Harmony harmonyInstance = new Harmony("SatisfiedStorageMod");
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());              // just use all [HarmonyPatch] decorated classes       
                         
        }
    }
}
