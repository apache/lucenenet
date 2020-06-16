using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

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
    /// An example of the most basic implementation of <see cref="IConfigurationFactory"/>
    /// to retrieve the configuration from a <see cref="IServiceProvider"/>.
    /// </summary>
    public class ServiceProviderConfigurationFactory : IConfigurationFactory
    {
        private readonly IServiceProvider serviceProvider;

        public ServiceProviderConfigurationFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IConfiguration GetConfiguration()
        {
            return serviceProvider.GetService<IConfiguration>();
        }
    }
}
