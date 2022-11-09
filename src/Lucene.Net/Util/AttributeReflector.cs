using Lucene.Net.Analysis.TokenAttributes;
using System;

namespace Lucene.Net.Util
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
    /// This interface is used to reflect contents of <see cref="AttributeSource"/> or <see cref="Attribute"/>.
    /// </summary>
    public interface IAttributeReflector
    {
        /// <summary>
        /// LUCENENET specific overload to support generics.
        /// </summary>
        void Reflect<T>(string key, object value)
            where T : IAttribute;

        /// <summary>
        /// This method gets called for every property in an <see cref="Attribute"/>/<see cref="AttributeSource"/>
        /// passing the <see cref="Type"/> of the <see cref="IAttribute"/>, a <paramref name="key"/> and the actual <paramref name="value"/>.
        /// E.g., an invocation of <see cref="CharTermAttribute.ReflectWith(IAttributeReflector)"/>
        /// would call this method once using <c>typeof(ICharTermAttribute)</c>
        /// as attribute type, <c>"term"</c> as <paramref name="key"/> and the actual <paramref name="value"/> as a <see cref="string"/>.
        /// </summary>
        void Reflect(Type type, string key, object value);
    }
}