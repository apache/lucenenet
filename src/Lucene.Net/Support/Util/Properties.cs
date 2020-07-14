using Lucene.Net.Configuration;
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
    /// Reads properties from a <see cref="Func{IConfiguration}"/> that is supplied to the constructor.
    /// </summary>
    internal class Properties : IProperties
    {
        private readonly Func<IConfigurationFactory> getConfigurationFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="Properties"/> with the specified <see cref="Func{IConfigurationFactory}"/>.
        /// The delegate method ensures the current instance of <see cref="IConfiguration"/> is used.
        /// </summary>
        /// <param name="getConfigurationFactory">The <see cref="Func{IConfigurationFactory}"/>.</param>
        // NOTE: We are decoupling the configurationFactory here to create a seam that we can use to inject
        // a custom one for testing purposes. We don't want to hold onto a reference to it because the user
        // may change it to a different instance after the first time it is called.
        public Properties(Func<IConfigurationFactory> getConfigurationFactory)
        {
            this.getConfigurationFactory = getConfigurationFactory ?? throw new ArgumentNullException(nameof(getConfigurationFactory));
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
            IConfigurationFactory configurationFactory = getConfigurationFactory();
            IConfiguration configuration = configurationFactory.GetConfiguration();
            string setting = configuration[key];

            return string.IsNullOrEmpty(setting)
                ? defaultValue
                : conversionFunction(setting);
        }
    }
}
