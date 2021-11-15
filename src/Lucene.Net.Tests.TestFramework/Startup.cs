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

using J2N.Threading.Atomic;
using Lucene.Net.Configuration;
using Lucene.Net.Configuration.Custom;
using Lucene.Net.Util;

/// <summary>
/// An example of a custom initializer + test counter to ensure it is only loaded once.
/// The initializer is launched by NUnit. A custom initializer must exist outside of
/// all namespaces, and there can only be one per test assembly.
/// <para/>
/// This loads an instance of <see cref="MockConfigurationFactory"/>, which simply
/// decorates the default <see cref="TestConfigurationFactory"/> instance with an implementation
/// that adds some fake configuration values, so they don't interfere with actual testing.
/// </summary>
public class Startup : LuceneTestFrameworkInitializer
{
    internal static AtomicInt32 initializationCount = new AtomicInt32(0);
    internal static bool initilizationReset = false;

    public Startup()
    {
        initilizationReset = initializationCount.GetAndSet(0) != 0;
    }

    protected override void Initialize()
    {
        // Decorate the existing configuration factory with mock settings
        // so we don't interfere with the operation of the test framework.
        ConfigurationFactory = new MockConfigurationFactory(ConfigurationFactory);
    }

    internal override void AfterInitialization()
    {
        initializationCount.IncrementAndGet();
    }
}
