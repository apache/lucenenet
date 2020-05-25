//using Lucene.Net.Configuration;
//using Microsoft.Extensions.Configuration;
//using System;
//using System.Collections.Generic;

//namespace Lucene.Net.Configuration
//{
//    /*
//     * Licensed to the Apache Software Foundation (ASF) under one or more
//     * contributor license agreements.  See the NOTICE file distributed with
//     * this work for additional information regarding copyright ownership.
//     * The ASF licenses this file to You under the Apache License, Version 2.0
//     * (the "License"); you may not use this file except in compliance with
//     * the License.  You may obtain a copy of the License at
//     *
//     *     http://www.apache.org/licenses/LICENSE-2.0
//     *
//     * Unless required by applicable law or agreed to in writing, software
//     * distributed under the License is distributed on an "AS IS" BASIS,
//     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     * See the License for the specific language governing permissions and
//     * limitations under the License.
//     */

using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

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
    internal class ConfigurationRootFactory : IConfigurationRootFactory
    {
        public IConfigurationRoot CurrentConfiguration
        {
            get
            {
                return CreateConfiguration();
            }
        }

        /// <summary>
        /// Initializes the dependencies of this factory.
        /// </summary>
        public virtual IConfigurationRoot CreateConfiguration()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "lucene:") // Use a custom prefix to only load Lucene.NET settings
                .AddJsonFile("appsettings.json");
            return builder.Build();
        }
    }
}
