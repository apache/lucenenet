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

namespace Lucene.Net.Support
{
    using Util;

    public class TestNumberExtensionMethods : LuceneTestCase
    {

        [Test]
        public void NumberOfLeadingZerosForInt()
        {
            Equal(32, 0x0.NumberOfLeadingZeros());
            Equal(24, 0xff.NumberOfLeadingZeros());
        }

        [Test]
        public void NumberOfLeadingZerosForLong()
        {
            Equal(64, ((long)0x0).NumberOfLeadingZeros());
            Equal(56, ((long)0xff).NumberOfLeadingZeros());
        }

        [Test]
        public void NumberOfTrailingZerosForInt()
        {
            Equal(4, 10000.NumberOfTrailingZeros());
            Equal(6, 1000000.NumberOfTrailingZeros());
        }


        [Test]
        public void NumberOfTrailingZerosForLong()
        {
            Equal(3, 1000L.NumberOfTrailingZeros());
            Equal(5, 100000L.NumberOfTrailingZeros());
        }
    }
}
