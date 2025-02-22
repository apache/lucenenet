using System;

namespace Lucene.Net.Reflection
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
    /// Suppresses API analysis warnings for differences in modifiers between .NET and Java.
    /// </summary>
    [NoLuceneEquivalent]
    [AttributeUsage(
        AttributeTargets.Class
        | AttributeTargets.Enum
        | AttributeTargets.Interface
        | AttributeTargets.Struct
        | AttributeTargets.Method
        | AttributeTargets.Property
        | AttributeTargets.Field,
        Inherited = false, AllowMultiple = false)]
    public class LuceneModifierDifferenceAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LuceneModifierDifferenceAttribute"/>.
        /// </summary>
        /// <param name="justification">The justification for the difference in modifiers between .NET and Java.</param>
        public LuceneModifierDifferenceAttribute(string justification)
        {
            Justification = justification;
        }

        /// <summary>
        /// Gets the justification for the difference in modifiers between .NET and Java.
        /// </summary>
        public string Justification { get; }
    }
}
