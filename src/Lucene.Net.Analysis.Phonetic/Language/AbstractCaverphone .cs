// commons-codec version compatibility level: 1.9
using System;

namespace Lucene.Net.Analysis.Phonetic.Language
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
    /// Encodes a string into a Caverphone value.
    /// <para/>
    /// This is an algorithm created by the Caversham Project at the University of Otago. It implements the Caverphone 2.0
    /// algorithm:
    /// <para/>
    /// This class is immutable and thread-safe.
    /// <para/>
    /// See <a href="http://en.wikipedia.org/wiki/Caverphone">Wikipedia - Caverphone</a>
    /// </summary>
    public abstract class AbstractCaverphone : IStringEncoder
    {
        /// <summary>
        /// Creates an instance of the Caverphone encoder
        /// </summary>
        protected AbstractCaverphone() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : base()
        {
        }

        // LUCENENET specific - in .NET we don't need an object overload of Encode(), since strings are sealed anyway.

        // LUCENENET specific - must provide implementation for IStringEncoder
        public abstract string Encode(string source);

        /// <summary>
        /// Tests if the encodings of two strings are equal.
        /// <para/>
        /// This method might be promoted to a new AbstractStringEncoder superclass.
        /// </summary>
        /// <param name="str1">First of two strings to compare.</param>
        /// <param name="str2">Second of two strings to compare.</param>
        /// <returns><c>true</c> if the encodings of these strings are identical, <c>false</c> otherwise.</returns>
        public virtual bool IsEncodeEqual(string str1, string str2) 
        {
            return this.Encode(str1).Equals(this.Encode(str2), StringComparison.Ordinal);
        }
    }
}
