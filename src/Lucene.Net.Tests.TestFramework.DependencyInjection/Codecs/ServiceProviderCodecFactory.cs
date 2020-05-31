using Lucene.Net.Util;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Codecs
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
    /// An example of a basic implementation of <see cref="ICodecFactory"/>
    /// to retrieve a codec from a <see cref="IServiceProvider"/> lazily.
    /// </summary>
    internal class ServiceProviderCodecFactory : ICodecFactory, IServiceListable
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IDictionary<string, Type> codecTypes;

        public ServiceProviderCodecFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Get the registered service collection, which can be used to get a list of registered types
            var serviceCollection = serviceProvider.GetService<IServiceCollection>();

            // Retrieve a list of registered types that subclass Codec. Codecs must be registered by
            // their concrete type so we can differentiate between them later when calling GetService().
            this.codecTypes = serviceCollection
                .Where(t => typeof(Codec).IsAssignableFrom(t.ServiceType))
                .ToDictionary(
                    t => NamedServiceFactory<Codec>.GetServiceName(t.ImplementationType),
                    t => t.ImplementationType
                );
        }

        public ICollection<string> AvailableServices => codecTypes.Keys;

        public Codec GetCodec(string name)
        {
            if (codecTypes.TryGetValue(name, out Type implementationType))
                return (Codec)serviceProvider.GetService(implementationType);

            throw new ArgumentException($"The codec {name} is not registered.", nameof(name));
        }
    }
}
