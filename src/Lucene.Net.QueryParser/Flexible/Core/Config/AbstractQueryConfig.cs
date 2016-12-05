using System;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Core.Config
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
    /// This class is the base of <see cref="QueryConfigHandler"/> and <see cref="FieldConfig"/>.
    /// It has operations to set, unset and get configuration values.
    /// <para>
    /// Each configuration is is a key->value pair. The key should be an unique
    /// <see cref="ConfigurationKey{T}"/> instance and it also holds the value's type.
    /// </para>
    /// <seealso cref="ConfigurationKey{T}"/>
    /// </summary>
    public abstract class AbstractQueryConfig
    {
        private readonly IDictionary<ConfigurationKey, object> configMap = new Dictionary<ConfigurationKey, object>();

        internal AbstractQueryConfig()
        {
            // although this class is public, it can only be constructed from package
        }

        /// <summary>
        /// Returns the value held by the given key.
        /// </summary>
        /// <typeparam name="T">the value's type</typeparam>
        /// <param name="key">the key, cannot be <c>null</c></param>
        /// <returns>the value held by the given key</returns>
        public virtual T Get<T>(ConfigurationKey<T> key)
        {
            if (key == null)
            {
                throw new ArgumentException("key cannot be null!");
            }
            object result;
            this.configMap.TryGetValue(key, out result);
            return result == null ? default(T) : (T)result;
        }

        /// <summary>
        /// Returns <c>true</c> if there is a value set with the given key, otherwise <c>false</c>.
        /// </summary>
        /// <typeparam name="T">the value's type</typeparam>
        /// <param name="key">the key, cannot be <c>null</c></param>
        /// <returns><c>true</c> if there is a value set with the given key, otherwise <c>false</c></returns>
        public virtual bool Has<T>(ConfigurationKey<T> key)
        {
            if (key == null)
            {
                throw new ArgumentException("key cannot be null!");
            }

            return this.configMap.ContainsKey(key);
        }

        /// <summary>
        /// Sets a key and its value.
        /// </summary>
        /// <typeparam name="T">the value's type</typeparam>
        /// <param name="key">the key, cannot be <c>null</c></param>
        /// <param name="value">value to set</param>
        public virtual void Set<T>(ConfigurationKey<T> key, T value)
        {
            if (key == null)
            {
                throw new ArgumentException("key cannot be null!");
            }

            if (value == null)
            {
                Unset(key);
            }
            else
            {
                this.configMap[key] = value;
            }
        }

        /// <summary>
        /// Unsets the given key and its value.
        /// </summary>
        /// <typeparam name="T">the value's type</typeparam>
        /// <param name="key">the key</param>
        /// <returns><c>true</c> if the key and value was set and removed, otherwise <c>false</c></returns>
        public virtual bool Unset<T>(ConfigurationKey<T> key)
        {
            if (key == null)
            {
                throw new ArgumentException("key cannot be null!");
            }

            return this.configMap.Remove(key);
        }
    }
}
