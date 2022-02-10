using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace Lucene.Net.Configuration
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
    /// An environment variable based <see cref="IConfigurationProvider"/>.
    /// </summary>
    internal class EnvironmentVariablesConfigurationProvider : IConfigurationProvider
    {
        private readonly bool ignoreSecurityExceptionsOnRead;
        private readonly string _prefix;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public EnvironmentVariablesConfigurationProvider(bool ignoreSecurityExceptionsOnRead = true) : this(string.Empty, ignoreSecurityExceptionsOnRead)
        { }

        /// <summary>
        /// Initializes a new instance with the specified prefix.
        /// </summary>
        /// <param name="prefix">A prefix used to filter the environment variables.</param>
        public EnvironmentVariablesConfigurationProvider(string prefix, bool ignoreSecurityExceptionsOnRead = true)
        {
            _prefix = prefix ?? string.Empty;
            this.ignoreSecurityExceptionsOnRead = ignoreSecurityExceptionsOnRead;
        }

        /// <summary>
        /// Loads the environment variables.
        /// </summary>
        public void Load()
        {
            Data = new ConcurrentDictionary<string, string>();
        }

        /// <summary>
        /// The configuration key value pairs for this provider.
        /// </summary>
        protected ConcurrentDictionary<string, string> Data { get; set; }

        public virtual bool TryGet(string key, out string value)
        {
            value = Data.GetOrAdd(key, (name) =>
            {
                // LUCENENET: There is a slight chance that two threads could load the
                // same environment variable at the same time, but it shouldn't be too expensive. See #417.
                if (ignoreSecurityExceptionsOnRead)
                {
                    try
                    {
                        return Environment.GetEnvironmentVariable(_prefix + name);
                    }
                    catch (SecurityException)
                    {
                        return null;
                    }
                }
                else
                {
                    return Environment.GetEnvironmentVariable(_prefix + name);
                }
            });
            return (!string.IsNullOrEmpty(value));
        }


        /// <summary>
        /// Sets a value for a given key.
        /// </summary>
        /// <param name="key">The configuration key to set.</param>
        /// <param name="value">The value to set.</param>
        public virtual void Set(string key, string value)
            => Data[key] = value;
        /// <summary>
        /// Returns the list of keys that this provider has.
        /// </summary>
        /// <param name="earlierKeys">The earlier keys that other providers contain.</param>
        /// <param name="parentPath">The path for the parent IConfiguration.</param>
        /// <returns>The list of keys for this provider.</returns>
        public virtual IEnumerable<string> GetChildKeys(
            IEnumerable<string> earlierKeys,
            string parentPath)
        {
            var prefix = parentPath is null ? string.Empty : parentPath + ConfigurationPath.KeyDelimiter;

            return Data
                .Where(kv => kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(kv => Segment(kv.Key, prefix.Length))
                .Concat(earlierKeys)
                .OrderBy(k => k);
        }

        private static string Segment(string key, int prefixLength)
        {
            var indexOf = key.IndexOf(ConfigurationPath.KeyDelimiter, prefixLength, StringComparison.OrdinalIgnoreCase);
            return indexOf < 0 ? key.Substring(prefixLength) : key.Substring(prefixLength, indexOf - prefixLength);
        }

        private readonly IChangeToken _reloadToken = new ConfigurationReloadToken();

        /// <summary>
        /// Returns a <see cref="IChangeToken"/> that can be used to listen when this provider is reloaded.
        /// </summary>
        /// <returns></returns>
        public IChangeToken GetReloadToken()
        {
            return _reloadToken;
        }
    }
}
