using RimWorld;
using System.Collections.Generic;

namespace SatisfiedStorage
{

    public class Mapping
    {
        private static Dictionary<StorageSettings, Hysteresis> mapping = new Dictionary<StorageSettings, Hysteresis>();

        public static Hysteresis Get(StorageSettings storage)
        {
            bool flag = mapping.ContainsKey(storage);
            Hysteresis result;
            if (flag)
            {
                result = mapping[storage];
            }
            else
            {
                result = new Hysteresis();
            }

            return result;
        }

        public static void Set(StorageSettings storage, Hysteresis value)
        {
            bool flag = mapping.ContainsKey(storage);
            if (flag)
            {
                if (mapping[storage] != value)
                {
                    sethelper(storage, value);
                }
            }
            else
            {
                sethelper(storage, value);
            }
        }


        internal static void sethelper(StorageSettings storage, Hysteresis val)
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
