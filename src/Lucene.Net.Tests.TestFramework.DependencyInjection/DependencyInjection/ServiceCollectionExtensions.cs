using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Lucene.Net.DependencyInjection
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
    /// Extensions to Microsoft.Extensions.DependencyInjection to add support for named service types
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        internal static readonly IDictionary<NamedServiceDescriptor, Type> nameToTypeMap
            = new ConcurrentDictionary<NamedServiceDescriptor, Type>();

        public static IServiceCollection AddSingleton<TService, TImplementation>(
            this IServiceCollection serviceCollection,
            string name)
            where TService : class where TImplementation : class, TService
        {
            nameToTypeMap[new NamedServiceDescriptor(name, typeof(TService))]
                = typeof(TImplementation);
            return serviceCollection.AddSingleton<TImplementation>();
        }
    }
}
