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

using System.Runtime.Serialization;

namespace Lucene.Net.Util
{
    using Lucene.Net.Random;
#if PORTABLE || K10
    using Lucene.Net.Support;
#endif
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using Xunit;

    public class LuceneTestCase : TestClass
    {
        /// <summary>
        ///  A random multiplier which you should use when writing random tests:
        ///  multiply it by the number of iterations to scale your tests (for nightly builds).
        /// </summary>
        public static int RANDOM_MULTIPLIER = SystemProps.Get<int>("tests:multiplier", 1);

        /// <summary>
        /// Whether or not <see cref="NightlyAttribute" /> tests should run.
        /// </summary>
        public static bool TEST_NIGHTLY = SystemProps.Get<Boolean>("tests:nightly", false);


        public static int AtLeast(int minimumValue)
        {
            int min = (LuceneTestCase.TEST_NIGHTLY ? 2 * minimumValue : minimumValue) * LuceneTestCase.RANDOM_MULTIPLIER;
            int max = min + (min / 2);
            return Random.NextBetween(min, max);
        }

       
    }
}