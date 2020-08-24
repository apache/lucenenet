using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Configuration.Custom
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

    public class MockConfigurationFactory : IConfigurationFactory
    {
        private readonly IConfigurationFactory wrapped;

        public MockConfigurationFactory(IConfigurationFactory wrapped)
        {
            this.wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
        }

        public IConfiguration GetConfiguration()
        {
            return new MockConfiguration(wrapped.GetConfiguration(),
                new Dictionary<string, string>
                {
                    ["fruit"] = "banana",
                    ["vegetable"] = "lettuce",
                    ["tests:goo"] = "yogurt",
                    ["tests:junk"] = "pizza"
                });
        }

        private class MockConfiguration : IConfiguration
        {
            private readonly IConfiguration wrapped;
            private readonly IDictionary<string, string> settings;

            public MockConfiguration(IConfiguration wrapped, IDictionary<string, string> settings)
            {
                this.wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
                this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            }

            public string this[string key]
            {
                get
                {
                    if (settings.TryGetValue(key, out string value))
                        return value;
                    return wrapped[key];
                }
                set
                {
                    settings[key] = value;
                    wrapped[key] = value;
                }
            }

            public IEnumerable<IConfigurationSection> GetChildren()
            {
                return wrapped.GetChildren();
            }

            public IChangeToken GetReloadToken()
            {
                return wrapped.GetReloadToken();
            }

            public IConfigurationSection GetSection(string key)
            {
                return wrapped.GetSection(key);
            }

            public void Reload()
            {
                settings.Clear();
                (wrapped as IConfigurationRoot)?.Reload();
            }
        }
    }
}
