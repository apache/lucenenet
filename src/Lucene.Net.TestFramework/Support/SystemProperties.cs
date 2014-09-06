using System.Configuration;

namespace Lucene.Net.TestFramework.Support
{
    public static class SystemProperties
    {
        public static string GetProperty(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        public static string GetProperty(string key, string defaultValue)
        {
            var setting = ConfigurationManager.AppSettings[key];

            if (string.IsNullOrEmpty(setting))
                return defaultValue;

            return setting;
        }
    }
}