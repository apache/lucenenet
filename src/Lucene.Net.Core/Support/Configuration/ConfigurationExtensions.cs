// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. 
//See https://github.com/aspnet/Entropy/blob/dev/LICENSE.txt in the project root for license information.

//Code modified to work with latest version of framework.

using System;
using System.Collections.Immutable;
using Microsoft.Extensions.Configuration;

namespace Lucene.Net.Support.Configuration
{
    public static class ConfigurationExtensions
    {
        public const string AppSettings = "appSettings";

        public static string GetAppSetting(this IConfiguration configuration, string name)
        {
            return configuration?.GetSection(AppSettings)[name];
        }
        
         public static ImmutableDictionary<string, IConfigurationSection> GetSection(this IConfiguration configuration, params string[] sectionNames)
        {
            if (sectionNames.Length == 0)
                return ImmutableDictionary<string, IConfigurationSection>.Empty;
            
            var fullKey = string.Join(ConfigurationPath.KeyDelimiter, sectionNames);

            return configuration?.GetSection(fullKey).GetChildren()?.ToImmutableDictionary(x => x.Key, x => x);
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
