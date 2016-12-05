namespace Lucene.Net.QueryParsers.Flexible.Core.Config
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
    /// An instance of this class represents a key that is used to retrieve a value
    /// from <see cref="AbstractQueryConfig"/>. It also holds the value's type, which is
    /// defined in the generic argument.
    /// </summary>
    /// <seealso cref="AbstractQueryConfig"/>
    /// <typeparam name="T">the value's type</typeparam>
    public sealed class ConfigurationKey<T> : ConfigurationKey
    {
        internal ConfigurationKey() { }
    }

    /// <summary>
    /// LUCENENET specific class used to access the NewInstance
    /// static method without referring to the ConfigurationKey{T}'s
    /// generic closing type.
    /// </summary>
    public abstract class ConfigurationKey
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <typeparam name="T">the value's type</typeparam>
        /// <returns>a new instance</returns>
        public static ConfigurationKey<T> NewInstance<T>()
        {
            return new ConfigurationKey<T>();
        }
    }
}
