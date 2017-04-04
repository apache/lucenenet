#if NETSTANDARD
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;
#else
using System.Configuration;
#endif

namespace Lucene.Net.Support.Configuration
{
    public static class Configuration
    {
#if NETSTANDARD
        private static IConfigurationRoot _configuration;

        static Configuration()
        {
            var builder = new ConfigurationBuilder();
            var entryAssembly = Assembly.GetEntryAssembly();
            var configurationFiles = new string[0];

            if (entryAssembly != null)
            {
                var directory = Path.GetDirectoryName(entryAssembly.Location);
                configurationFiles = Directory.GetFiles(directory, "*.config");
            }

            foreach (var config in configurationFiles)
            {
                builder.AddConfigFile(config, false, new KeyValueParser());
            }

            _configuration = builder.Build();
        }
#endif

        public static string GetAppSetting(string key)
        {
#if NETSTANDARD

            return _configuration.GetAppSetting(key);
#else
            return ConfigurationManager.AppSettings[key];
#endif
        }

        public static string GetAppSetting(string key, string defaultValue)
        {
            string setting = GetAppSetting(key);
            return string.IsNullOrEmpty(setting) ? defaultValue : setting;
        }

        /// <summary>
        /// Gets the value for the AppSetting with specified key.  
        /// If key is not present, default value is returned.
        /// If key is present, value is converted to specified type based on the conversionFunction specified.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="conversionFunction"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetProperty<T>(string key, T defaultValue, System.Func<string, T> conversionFunction)
        {
            string setting = GetAppSetting(key);
            return string.IsNullOrEmpty(setting) ? defaultValue : conversionFunction(setting);
        }
    }
}