using System;
using System.Configuration;

namespace Lucene.Net.Support
{
    /// <summary>
    /// 
    /// </summary>
    public class AppSettings
    {
        static System.Collections.Specialized.ListDictionary settings = new System.Collections.Specialized.ListDictionary();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defValue"></param>
        public static void Set(System.String key, int defValue)
        {
            settings[key] = defValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defValue"></param>
        public static void Set(System.String key, long defValue)
        {
            settings[key] = defValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defValue"></param>
        public static void Set(System.String key, System.String defValue)
        {
            settings[key] = defValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defValue"></param>
        public static void Set(System.String key, bool defValue)
        {
            settings[key] = defValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        public static int Get(System.String key, int defValue)
        {
            if (settings[key] != null)
            {
                return (int)settings[key];
            }

            System.String theValue = ConfigurationManager.AppSettings.Get(key);
            if (theValue == null)
            {
                return defValue;
            }
            int retValue = Convert.ToInt32(theValue.Trim());
            settings[key] = retValue;
            return retValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        public static long Get(System.String key, long defValue)
        {
            if (settings[key] != null)
            {
                return (long)settings[key];
            }

            System.String theValue = ConfigurationManager.AppSettings.Get(key);
            if (theValue == null)
            {
                return defValue;
            }
            long retValue = Convert.ToInt64(theValue.Trim());
            settings[key] = retValue;
            return retValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        public static System.String Get(System.String key, System.String defValue)
        {
            if (settings[key] != null)
            {
                return (System.String)settings[key];
            }

            System.String theValue = ConfigurationManager.AppSettings.Get(key);
            if (theValue == null)
            {
                return defValue;
            }
            settings[key] = theValue;
            return theValue;
        }

        public static bool Get(System.String key, bool defValue)
        {
            if (settings[key] != null)
            {
                return (bool)settings[key];
            }

            System.String theValue = ConfigurationManager.AppSettings.Get(key);
            if (theValue == null)
            {
                return defValue;
            }
            bool retValue = Convert.ToBoolean(theValue.Trim());
            settings[key] = retValue;
            return retValue;
        }
    }
}