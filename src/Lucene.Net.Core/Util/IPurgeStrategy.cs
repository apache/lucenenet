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

namespace Lucene.Net.Util
{
    using System;
    using System.Threading.Tasks;



    /// <summary>
    ///  Contract for a strategy that removes dead object references created by 
    /// <see cref="PurgeableThreadLocal{T}"/>
    /// </summary>
    public interface IPurgeStrategy : IDisposable
    {
        /// <summary>
        /// Adds the value asynchronously.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><see cref="Task"/></returns>
        Task AddAsync(object value);

        /// <summary>
        /// Purges the object references asynchronously.
        /// </summary>
        /// <returns><see cref="Task"/></returns>
        Task PurgeAsync();
    }
}
