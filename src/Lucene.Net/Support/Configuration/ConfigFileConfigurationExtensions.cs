// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. 
//See https://github.com/aspnet/Entropy/blob/dev/LICENSE.txt in the project root for license information.

//Code modified to work with latest version of framework.

using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Lucene.Net.Support.Configuration
{
    public static class ConfigFileConfigurationExtensions
    {
        /// <summary>
        /// Adds configuration values for a *.config file to the ConfigurationBuilder
        /// </summary>
        /// <param name="builder">Builder to add configuration values to</param>
        /// <param name="path">Path to *.config file</param>
        /// <param name="optional">true if file is optional; false otherwise</param>
        /// <param name="parsers">Additional parsers to use to parse the config file</param>
        public static IConfigurationBuilder AddConfigFile(this IConfigurationBuilder builder, string path, bool optional, params IConfigurationParser[] parsers)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            else if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path for configuration cannot be null/empty.", nameof(path));
            }

            if (!optional && !File.Exists(path))
            {
                throw new FileNotFoundException($"Could not find configuration file. File: [{path}]", path);
            }

            return builder.Add(new ConfigFileConfigurationSource(path, true, optional, parsers));
        }

        /// <summary>
        /// Adds configuration values for an XML configuration the ConfigurationBuilder
        /// </summary>
        /// <param name="builder">Builder to add configuration values to</param>
        /// <param name="configurationContents">XML Contents of configuration</param>
        /// <param name="parsers">Additional parsers to use to parse the config file</param>
        public static IConfigurationBuilder AddConfigFile(this IConfigurationBuilder builder, string configurationContents, params IConfigurationParser[] parsers)
        {
            if (configurationContents == null)
            {
                throw new ArgumentNullException(nameof(configurationContents));
            }
            else if (string.IsNullOrEmpty(configurationContents))
            {
                throw new ArgumentException("Path for configuration cannot be null/empty.", nameof(configurationContents));
            }

            return builder.Add(new ConfigFileConfigurationSource(configurationContents, false, false, parsers));
        }
    }
}
