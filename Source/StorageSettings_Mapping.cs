using System.Collections.Generic;
using RimWorld;

namespace SatisfiedStorage
{

    public class StorageSettings_Mapping
    {
        private static Dictionary<StorageSettings, StorageSettings_Hysteresis> mapping = new Dictionary<StorageSettings, StorageSettings_Hysteresis>();

        public static StorageSettings_Hysteresis Get(StorageSettings storage)
        {
            bool flag = mapping.ContainsKey(storage);
            StorageSettings_Hysteresis result;
            if (flag)
            {
                result = mapping[storage];
            }
            else
            {
                result = new StorageSettings_Hysteresis();
            }

            return result;
        }

        public static void Set(StorageSettings storage, StorageSettings_Hysteresis value)
        {
            bool flag = mapping.ContainsKey(storage);
            if (flag)
            {
                if(mapping[storage] != value)
                {
                    sethelper(storage,value);                    
                }               
            }
            else
            {
                sethelper(storage,value);                
            }
        }


        internal static void sethelper(StorageSettings storage,StorageSettings_Hysteresis val)
        {
            bool flag = mapping.ContainsKey(storage);
            if (flag)
            {
                mapping[storage] = val;
            }
            else
            {
                mapping.Add(storage, val);
            }


            
            
        }
        
    }
}
