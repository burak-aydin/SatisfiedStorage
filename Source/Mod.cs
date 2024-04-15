using HarmonyLib;
using System.Reflection;
using Verse;


namespace SatisfiedStorage
{


    public class Mod : Verse.Mod
    {

        public Mod(ModContentPack content) : base(content)
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
