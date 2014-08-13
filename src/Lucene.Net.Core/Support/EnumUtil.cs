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

namespace Lucene.Net
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class EnumUtil
    {
        /// <summary>
        /// Returns all the values of the Enum as an IEnumerable&lt;T&gt; where T : enum
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <exception cref="System.ArgumentException">Throws when T is not an enum.</exception>
        /// <returns>Returns an IEnumerable&lt;T&gt; of enum values for type T.</returns>
        public static IList<T> ValuesOf<T>()
        { 
            // TODO: 5.0 resource: put exception message in resource.
            if (!typeof(T).GetTypeInfo().IsEnum)
                throw new ArgumentException("Type of T must be an enum");

            return Enum.GetValues(typeof(T)).Cast<T>().ToList();
        }
    }
}