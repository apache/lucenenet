using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Lucene.Net.TestFramework.Support
{
    public class RandomizedTest
    {
        public static bool SystemPropertyAsBoolean(string key, bool defaultValue)
        {
            var setting = ConfigurationManager.AppSettings[key];

            if (string.IsNullOrEmpty(setting))
                return defaultValue;

            bool v;

            if (bool.TryParse(setting, out v))
                return v;

            return defaultValue;
        }



        public static int SystemPropertyAsInt(string key, int defaultValue)
        {
            var setting = ConfigurationManager.AppSettings[key];

            if (string.IsNullOrEmpty(setting))
                return defaultValue;

            int v;

            if (int.TryParse(setting, out v))
                return v;

            return defaultValue;
        }
    }
}