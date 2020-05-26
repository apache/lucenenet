using System;

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
    internal static class ServiceProviderExtensions
    {
        public static T GetService<T>(this IServiceProvider provider, string name)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            ServiceCollectionExtensions.nameToTypeMap.TryGetValue(
                new NamedServiceDescriptor(name, typeof(T)), out Type implementationType);
            return (T)provider.GetService(implementationType);
        }
    }
}
