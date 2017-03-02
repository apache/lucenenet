using System;

namespace Lucene.Net.Support
{
    public static class SystemProperties
    {
        public static string GetProperty(string key)
        {
            return GetProperty(key, null);
        }

        public static string GetProperty(string key, string defaultValue)
        {
            return GetProperty<string>(key, defaultValue,
                (str) =>
                {
                    return str;
                });
        }

        public static bool GetPropertyAsBoolean(string key)
        {
            return GetPropertyAsBoolean(key, false);
        }

        public static bool GetPropertyAsBoolean(string key, bool defaultValue)
        {
            return GetProperty<bool>(key, defaultValue,
                (str) =>
                {
                    bool value;
                    return bool.TryParse(str, out value) ? value : defaultValue;
                });
        }

        public static int GetPropertyAsInt(string key)
        {
            return GetPropertyAsInt(key, 0);
        }

        public static int GetPropertyAsInt(string key, int defaultValue)
        {
            return GetProperty<int>(key, defaultValue,
              (str) =>
              {
                  int value;
                  return int.TryParse(str, out value) ? value : defaultValue;
              }
            );
        }

        private static T GetProperty<T>(string key, T defaultValue, System.Func<string, T> conversionFunction)
        {
            string setting = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrEmpty(setting) ? defaultValue : conversionFunction(setting);
        }

        public static void SetProperty(string key, string value)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}