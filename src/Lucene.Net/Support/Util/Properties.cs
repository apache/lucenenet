using Microsoft.Extensions.Configuration;
using System;

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
    /// Implementation of <see cref="IProperties"/> that handles type conversion and default values 
    /// for Java-style properties.
    /// <para/>
    /// Reads properties from an <see cref="IConfigurationRoot" /> instance or
    /// a <see cref="Func{IConfigurationRoot}"/> that is supplied to the constructor.
    /// </summary>
    internal class Properties : IProperties
    {
        private readonly Func<IConfigurationRoot> createConfiguration;

        /// <summary>
        /// Initaializes a new instance of <see cref="Properties"/> with the specified <see cref="IConfigurationRoot"/>.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfigurationRoot"/>.</param>
        public Properties(IConfigurationRoot configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            this.createConfiguration = () => configuration;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Properties"/> with the specified <see cref="Func{IConfigurationRoot}"/>.
        /// The delegate method ensures the current instance of <see cref="IConfigurationRoot"/> is used.
        /// </summary>
        /// <param name="createConfiguration">The <see cref="Func{IConfigurationRoot}"/>.</param>
        public Properties(Func<IConfigurationRoot> createConfiguration)
        {
            this.createConfiguration = createConfiguration ?? throw new ArgumentNullException(nameof(createConfiguration));
        }

        /// <summary>
        /// Retrieves the value of an property from the current process.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <returns>The property value.</returns>
        public string GetProperty(string key)
        {
            return GetProperty(key, null);
        }

        /// <summary>
        /// Retrieves the value of an property from the current process, 
        /// with a default value if it doens't exist or the caller doesn't have 
        /// permission to read the value.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <param name="defaultValue">The value to use if the property does not exist 
        /// or the caller doesn't have permission to read the value.</param>
        /// <returns>The property value.</returns>
        public string GetProperty(string key, string defaultValue)
        {
            return GetProperty(key, defaultValue,
                (str) =>
                {
                    return str;
                }
            );
        }

        /// <summary>
        /// Retrieves the value of an property from the current process
        /// as <see cref="bool"/>. If the value cannot be cast to <see cref="bool"/>, returns <c>false</c>.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <returns>The property value.</returns>
        public bool GetPropertyAsBoolean(string key)
        {
            return GetPropertyAsBoolean(key, false);
        }

        /// <summary>
        /// Retrieves the value of an property from the current process as <see cref="bool"/>, 
        /// with a default value if it doens't exist, the caller doesn't have permission to read the value, 
        /// or the value cannot be cast to a <see cref="bool"/>.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <param name="defaultValue">The value to use if the property does not exist,
        /// the caller doesn't have permission to read the value, or the value cannot be cast to <see cref="bool"/>.</param>
        /// <returns>The property value.</returns>
        public bool GetPropertyAsBoolean(string key, bool defaultValue)
        {
            return GetProperty(key, defaultValue,
                (str) =>
                {
                    return bool.TryParse(str, out bool value) ? value : defaultValue;
                }
            );
        }

        /// <summary>
        /// Retrieves the value of an property from the current process
        /// as <see cref="int"/>. If the value cannot be cast to <see cref="int"/>, returns <c>0</c>.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <returns>The property value.</returns>
        public int GetPropertyAsInt32(string key)
        {
            return GetPropertyAsInt32(key, 0);
        }

        /// <summary>
        /// Retrieves the value of an property from the current process as <see cref="int"/>, 
        /// with a default value if it doens't exist, the caller doesn't have permission to read the value, 
        /// or the value cannot be cast to a <see cref="int"/>.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <param name="defaultValue">The value to use if the property does not exist,
        /// the caller doesn't have permission to read the value, or the value cannot be cast to <see cref="int"/>.</param>
        /// <returns>The property value.</returns>
        public int GetPropertyAsInt32(string key, int defaultValue)
        {
            return GetProperty(key, defaultValue,
                (str) =>
                {
                    return int.TryParse(str, out int value) ? value : defaultValue;
                }
            );
        }

        private T GetProperty<T>(string key, T defaultValue, Func<string, T> conversionFunction)
        {
            IConfigurationRoot configuration = createConfiguration();
            string setting = configuration[key];

            return string.IsNullOrEmpty(setting)
                ? defaultValue
                : conversionFunction(setting);
        }
    }
}
