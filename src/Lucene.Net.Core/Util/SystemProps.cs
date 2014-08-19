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

namespace Lucene.Net.Util
{
    using Microsoft.Framework.ConfigurationModel;
    using System;


    /// <summary>
    /// SystemProps gets configuration properties for Lucene.Net. By default, it loads 
    /// <see cref="EnvironmentVariablesConfigurationSource"/> for the <see cref="Constants"/> class.
    /// Other configuration sources may be added at runtime. 
    /// </summary>

    public class SystemProps
    {
        private static Configuration s_config = new Configuration();

        static SystemProps()
        {
            s_config.Add(new EnvironmentVariablesConfigurationSource());
        }

        /// <summary>
        /// Adds the <see cref="IConfigurationSource"/> to what is available in <see cref="SystemProps"/>.
        /// </summary>
        /// <param name="configurationSource"></param>
        [CLSCompliant(false)]
        public static void Add(IConfigurationSource configurationSource)
        {
            s_config.Add(configurationSource);
        }

        /// <summary>
        /// Gets the string value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The identifier associated with a value.</param>
        /// <returns>The <see cref="string"/> value.</returns>
        public static string Get(string key)
        {
            return s_config.Get(key);
        }

        /// <summary>
        /// Gets the <typeparamref name="T"/> value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The expected value type.</typeparam>
        /// <param name="key">The identifier associated with a value.</param>
        /// <returns>The <typeparamref name="T"/> value.</returns>
        public static T Get<T>(string key)
        {
            return Get(key, default(T));
        }

        /// <summary>
        ///  Gets the <typeparamref name="T"/> value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The expected value type</typeparam>
        /// <param name="key">The identifier associated with a value.</param>
        /// <param name="defaultValue">The default value that will be supplied if the key returns null.</param>
        /// <returns></returns>
        public static T Get<T>(string key, T defaultValue)
        {
            var value = s_config.Get(key);
            if (value == null)
                return defaultValue;

            return (T)Convert.ChangeType(value, typeof(T));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        [CLSCompliant(false)]
        public static void UseConfiguration(Configuration configuration)
        {
            s_config = configuration;
        }
    }

}
