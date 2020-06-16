using Microsoft.Extensions.Configuration;
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
    /// Contract for extending the functionality of system properties by providing an application-defined
    /// <see cref="IConfiguration"/> instance.
    /// <para/>
    /// Usage: Implement this interface and set the implementation at application startup using 
    /// <see cref="ConfigurationSettings.SetConfigurationFactory(IConfigurationFactory)"/>.
    /// </summary>
    [CLSCompliant(false)]
    public interface IConfigurationFactory
    {
        /// <summary>
        /// Gets or creates an instance of <see cref="IConfiguration"/> that Lucene.NET can use
        /// to read application-defined settings.
        /// <para/>
        /// The implementation is responsible for the lifetime of the <see cref="IConfiguration"/> instance.
        /// A typical implementation will either get the instance from a dependency injection container or
        /// provide its own caching mechanism to ensure the settings are not reloaded each time the method
        /// is called.
        /// </summary>
        /// <returns>The current <see cref="IConfiguration"/> instance.</returns>
        IConfiguration GetConfiguration();
    }
}
