// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. 
//See https://github.com/aspnet/Entropy/blob/dev/LICENSE.txt in the project root for license information.

//Code modified to work with latest version of framework.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Support.Configuration
{
    public static class ConfigurationExtensions
    {
        public const string AppSettings = "appSettings";

        public static string GetAppSetting(this IConfiguration configuration, string name)
        {
            return configuration?.GetSection(AppSettings)[name];
        }
        
         public static IDictionary<string, IConfigurationSection> GetSection(this IConfiguration configuration, params string[] sectionNames)
        {
            if (sectionNames.Length == 0)
                return Collections.UnmodifiableMap(new Dictionary<string, IConfigurationSection>());
            
            var fullKey = string.Join(ConfigurationPath.KeyDelimiter, sectionNames);

            return Collections.UnmodifiableMap(configuration?.GetSection(fullKey).GetChildren()?.ToDictionary(k => k.Key, v => v));
        }

        public static string GetValue(this IConfiguration configuration, params string[] keys)
        {
            if (keys.Length == 0)
            {
                throw new ArgumentException("Need to provide keys", nameof(keys));
            }
           
            var fullKey = string.Join(ConfigurationPath.KeyDelimiter, keys);

            return configuration?[fullKey];
        }
    }
}
