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
        /// <param name="randomSeed">A <see cref="long"/> value that represents the initial random seed to use to run the tests.</param>
        public RandomSeedAttribute(long randomSeed)
        {
            RandomSeed = randomSeed;
        }

        /// <summary>
        /// Construct a <see cref="RandomSeedAttribute"/> with a specific random seed value.
        /// </summary>
        /// <param name="randomSeed">A <see cref="ulong"/> value that represents the initial random seed to use to run the tests.</param>
        [CLSCompliant(false)]
        public RandomSeedAttribute(ulong randomSeed)
        {
            RandomSeed = unchecked((long)randomSeed);
        }

        /// <summary>
        /// The random seed value.
        /// </summary>
        public long RandomSeed { get; private set; }
    }
}
