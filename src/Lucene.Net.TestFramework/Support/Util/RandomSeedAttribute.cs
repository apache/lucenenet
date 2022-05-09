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
    /// Specifies a random seed to use when running tests. This allows specific test conditions to be repeated for debugging purposes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
    public sealed class RandomSeedAttribute : System.Attribute
    {
        /// <summary>
        /// Construct a <see cref="RandomSeedAttribute"/> with a specific random seed value.
        /// </summary>
        /// <param name="randomSeed">A <see cref="string"/> value that represents the initial random seed to use to run the tests.
        /// The seed is a hexadecimal string representation of a <see cref="long"/> value.
        /// <para/>
        /// <b>NOTE:</b> For future expansion, the string may include other values as well using ":" as delimiters.</param>
        public RandomSeedAttribute(string randomSeed)
        {
            RandomSeed = randomSeed;
        }

        /// <summary>
        /// The random seed value.
        /// </summary>
        public string RandomSeed { get; private set; }
    }
}
