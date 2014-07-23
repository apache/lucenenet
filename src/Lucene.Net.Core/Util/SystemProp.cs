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
    public static class SystemProps
    {
        private static Configuration s_config = new Configuration();

        static SystemProps()
        {
            s_config.Add(new EnvironmentVariablesConfigurationSource());
        }

#pragma warning disable "CS3001"
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configurationSource"></param>
        public static void Add(IConfigurationSource configurationSource)
#pragma warning restore "CS3001"
        {
           
            s_config.Add(configurationSource);
        }

        public static string Get(string key)
        {
            return s_config.Get(key);
        }

        public static T Get<T>(string key)
        {
            return Get<T>(key, default(T));
        }

        public static T Get<T>(string key, T defaultValue)
        {
            var value = s_config.Get(key);
            if (value == null)
                return defaultValue;

            return (T)Convert.ChangeType(value, typeof(T));
        }

#pragma warning disable "CS3001"
        // TODO: <Insert justification for suppressing CS3001>
        public static void UseConfiguration(Configuration configuration)
#pragma warning restore "CS3001"
        {
            s_config = configuration;
        }
    }
}
