
using Microsoft.Extensions.Configuration;

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
    /// Represents environment variables as an <see cref="IConfigurationSource"/>.
    /// </summary>
    internal class LuceneDefaultConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// A prefix used to filter environment variables.
        /// </summary>
        public string Prefix { get; set; }
        /// <summary>
        /// Set to true by default - used to prevent any security exceptions thrown when reading environment variables
        /// </summary>
        public bool IgnoreSecurityExceptionsOnRead { get; set; }

        /// <summary>
        /// Builds the <see cref="LuceneDefaultConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>A <see cref="LuceneDefaultConfigurationProvider"/></returns>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new LuceneDefaultConfigurationProvider(Prefix, IgnoreSecurityExceptionsOnRead);
        }
    }
}
