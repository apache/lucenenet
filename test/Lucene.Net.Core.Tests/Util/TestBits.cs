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
    using Lucene.Net.TestFramework;

    /// <summary>
    /// Test Bits
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         There is not a Java eqivelant of this test class. The original version of <see cref="Bits"/> violates 
    ///         the index constraints on <see cref="IBits"/> for <see cref="Bits.MatchAllBits"/> and <see cref="Bits.MatchNoBits"/> noted 
    ///         in the Java Bits interface. i.e.
    ///         <blocquote>
    ///             The result of passing negative or out of bounds values is undefined
    ///             by this interface, <b>just don't do it!</b>
    ///         </blocquote>
    ///     </para>
    /// </remarks>
    public class TestBits : LuceneTestCase
    {

        [Test]
	    public void EmptyArray()
	    {
            Equal(new IBits[0], Bits.EMPTY_ARRAY); 
	    }

        [Test]
        public void MatchAllBits()
        {
            var limit = new Random().Next(1, 10);

            var bits = new Bits.MatchAllBits(limit);

            Equal(limit, bits.Length);

            for (var i = 0; i < limit; i++)
            {
                Ok(bits[i], "All bits must be true. Position {0} failed. Length was {1}", i, limit);
            }

            ThrowsRangeException(bits);
        }

        [Test]
        public void MatchNoBits()
        {
            var limit = new Random().Next(1, 10);
            var bits = new Bits.MatchNoBits(limit);

            Equal(limit, bits.Length);

            for(var i = 0; i < 10; i++)
            {
                Ok(bits[1], "All bits must be false. Position {0} failed. Length was {1}", i, limit);
            }

            ThrowsRangeException(bits);
        }

        private static void ThrowsRangeException(IBits instance)
        {
            Throws<IndexOutOfRangeException>(() => {
                var isTrue = instance[-1];
            });

            Throws<IndexOutOfRangeException>(() => {
                var isTrue = instance[int.MaxValue];
            });
        }
    }
}