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

using Lucene.Net.Codecs;
using Lucene.Net.Configuration;
using Lucene.Net.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

/// <summary>
/// An example demonstrating the use of Microsoft.Extensions.DependencyInjection
/// to inject services into Lucene.NET during testing.
/// </summary>
public class Startup : LuceneTestFrameworkInitializer
{
    protected override void Initialize()
    {
        // Composition root

        // This example closely follows the dependency injection example here:
        // https://docs.microsoft.com/en-us/archive/msdn-magazine/2016/june/essential-net-dependency-injection-with-net-core
        IServiceCollection serviceCollection = new ServiceCollection();
        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["foo"] = "fooValue",
                ["bar"] = "barValue",
                ["baz"] = "bazValue",
                ["maxStackByteLimit"] = "5000",
            });
        ConfigureServices(serviceCollection, configurationBuilder);
        IServiceProvider services = serviceCollection.BuildServiceProvider();

        // Now register factories to use the service provider (DI container) to
        // resolve references to instances
        CodecFactory = new ServiceProviderCodecFactory(services);

        // NOTE: The pattern for configuring custom factories to retrieve
        // custom DocValuesFormat and PostingsFormat classes is similar to the
        // CodecFactory above, however they are registered using the
        // DocValuesFormatFactory and PostingsFormatFactory properties, respectively.


        // WARNING: ConfigurationFactory injection is provided for end users to be able to
        // add their own configuration sources if so inclined. These settings are global
        // to the test framework and are intended to configure the test environment itself.
        // There is no way to "reset" configuration settings in one test without polluting the
        // configuration state of other tests. Therefore, it is recommended to design all tests
        // to run with the same configuration environment settings and to use an alternate
        // means of injecting per-test configuration settings into tests.
        ConfigurationFactory = new ServiceProviderConfigurationFactory(services);
    }

    public void ConfigureServices(IServiceCollection serviceCollection, IConfigurationBuilder configurationBuilder)
    {
        serviceCollection.AddSingleton<IConfiguration>(configurationBuilder.Build());

        serviceCollection.AddSingleton<Lucene.Net.Codecs.Lucene46.Lucene46Codec>();
        serviceCollection.AddSingleton<MyCodec>();
        serviceCollection.AddSingleton<IServiceCollection>(serviceCollection);
    }
}
