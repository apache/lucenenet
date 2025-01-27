// Based on: https://github.com/randomizedtesting/randomizedtesting/blob/release/2.7.8/randomized-runner/src/main/java/com/carrotsearch/randomizedtesting/SeedUtils.java

using J2N;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Util
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one
     * or more contributor license agreements.  See the NOTICE file
     * distributed with this work for additional information
     * regarding copyright ownership.  The ASF licenses this file
     * to you under the Apache License, Version 2.0 (the
     * "License"); you may not use this file except in compliance
     * with the License.  You may obtain a copy of the License at
     * 
     *   http://www.apache.org/licenses/LICENSE-2.0
     * 
     * Unless required by applicable law or agreed to in writing,
     * software distributed under the License is distributed on an
     * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
     * KIND, either express or implied.  See the License for the
     * specific language governing permissions and limitations
     * under the License.
     */

    /// <summary>
    /// Utilities for parsing and formatting random seeds.
    /// </summary>
    internal static class SeedUtils
    {
        /// <summary>
        /// Format a single <paramref name="seed"/>.
        /// </summary>
        // LUCENENET: Our format deviates from the Java randomizedtesting implementation
        public static string FormatSeed(long seed)
            => $"0x{seed:x16}";

        /// <summary>
        /// Format a <paramref name="seed"/> and <paramref name="testSeed"/>.
        /// </summary>
        // LUCENENET: Our format deviates from the Java randomizedtesting implementation
        public static string FormatSeed(long seed, long testSeed)
            => $"0x{seed:x16}:0x{testSeed:x16}";
    }
}
