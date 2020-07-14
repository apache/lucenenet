using Lucene.Net.Configuration;
using Microsoft.Extensions.Configuration;
using System;
using System.Security;

namespace Lucene.Net.Util
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Reads properties from an <see cref="IProperties"/> instance. The default configuration reads
    /// the property valies from an <see cref="IConfiguration"/> instance returned by a
    /// <see cref="IConfigurationFactory"/> implementation.
    /// The <see cref="IConfigurationFactory"/> is set using
    /// <see cref="ConfigurationSettings.SetConfigurationFactory(IConfigurationFactory)"/>.
    /// This can be supplied a user implemented <see cref="IConfigurationFactory"/> to customize
    /// the property sources.
    /// </summary>
    internal static class SystemProperties
    {
        // Calls ConfigurationSettings.GetConfigurationFactory internally to
        // get the currently set instance of IConfigurationFactory
        private readonly static IProperties properties = new Properties(ConfigurationSettings.GetConfigurationFactory);

        /// <summary>
        /// Retrieves the value of a property from the current process.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <returns>The property value.</returns>
        public static string GetProperty(string key)
        {
            return properties.GetProperty(key);
        }

        /// <summary>
        /// Retrieves the value of a property from the current process, 
        /// with a default value if it doens't exist or the caller doesn't have 
        /// permission to read the value.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <param name="defaultValue">The value to use if the property does not exist 
        /// or the caller doesn't have permission to read the value.</param>
        /// <returns>The property value.</returns>
        public static string GetProperty(string key, string defaultValue)
        {
            return properties.GetProperty(key, defaultValue);
        }

        /// <summary>
        /// Retrieves the value of a property from the current process
        /// as <see cref="bool"/>. If the value cannot be cast to <see cref="bool"/>, returns <c>false</c>.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <returns>The property value.</returns>
        public static bool GetPropertyAsBoolean(string key)
        {
            return properties.GetPropertyAsBoolean(key);
        }

        /// <summary>
        /// Retrieves the value of a property from the current process as <see cref="bool"/>, 
        /// with a default value if it doens't exist, the caller doesn't have permission to read the value, 
        /// or the value cannot be cast to a <see cref="bool"/>.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <param name="defaultValue">The value to use if the property does not exist,
        /// the caller doesn't have permission to read the value, or the value cannot be cast to <see cref="bool"/>.</param>
        /// <returns>The property value.</returns>
        public static bool GetPropertyAsBoolean(string key, bool defaultValue)
        {
            return properties.GetPropertyAsBoolean(key, defaultValue);
        }

        /// <summary>
        /// Retrieves the value of a property from the current process
        /// as <see cref="int"/>. If the value cannot be cast to <see cref="int"/>, returns <c>0</c>.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <returns>The property value.</returns>
        public static int GetPropertyAsInt32(string key)
        {
            return properties.GetPropertyAsInt32(key);
        }

        /// <summary>
        /// Retrieves the value of a property from the current process as <see cref="int"/>, 
        /// with a default value if it doens't exist, the caller doesn't have permission to read the value, 
        /// or the value cannot be cast to a <see cref="int"/>.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <param name="defaultValue">The value to use if the property does not exist,
        /// the caller doesn't have permission to read the value, or the value cannot be cast to <see cref="int"/>.</param>
        /// <returns>The property value.</returns>
        public static int GetPropertyAsInt32(string key, int defaultValue)
        {
            return properties.GetPropertyAsInt32(key, defaultValue);
        }
    }
}
