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
    using Lucene.Net.Support;

    public class StringHelper
    {
        // WHY is this in string helper?
        /// <summary>
        /// Poached from Guava: set a different salt/seed
        /// for each JVM instance, to frustrate hash key collision
        /// denial of service attacks, and to catch any places that
        /// somehow rely on hash function/order across JVM
        /// instances:
        /// </summary>
        public static readonly int GOOD_FAST_HASH_SEED;

        static StringHelper()
        {
            var prop = SystemProps.Get("tests:seed");

            if(prop != null)
            {
                if(prop.Length > 8)
                {
                    prop = prop.Substring(prop.Length - 8);
                }

                GOOD_FAST_HASH_SEED = (int)int.Parse(prop);
            } else{
                GOOD_FAST_HASH_SEED = (int) System.DateTime.Now.Ticks;
            }
        }

        public static int MurmurHash3_x86_32(byte[] data, int offset, int length, int seed)
        {
            int c1 = unchecked((int)0xcc9e2d51),
                c2 = 0x1b873593;
            uint n = 3864292196;

            int h1 = seed;
            int roundedEnd = offset + (int)(length & 0xfffffffc);  // round down to 4 byte block

            for (int i=offset; i<roundedEnd; i+=4) {
              // little endian load order
              int k = (data[i] & 0xff) | ((data[i+1] & 0xff) << 8) | ((data[i+2] & 0xff) << 16) | (data[i+3] << 24);
              k *= c1;
              k = k.RotateLeft(15);
              k *= c2;
                
              h1 ^= k;
              h1 = h1.RotateLeft(13);
              h1 = h1 * 5+(int)n;
            }

    // tail
            int k1 = 0;

            switch(length & 0x03) {
              case 3:
                k1 = (data[roundedEnd + 2] & 0xff) << 16;
                goto case 2;
              case 2:
                k1 |= (data[roundedEnd + 1] & 0xff) << 8;
                goto case 1;
              case 1:
                k1 |= (data[roundedEnd] & 0xff);
                k1 *= c1;
                k1 = k1.RotateLeft(15);
                k1 *= c2;
                h1 ^= k1;
                break;
            }

            h1 ^= length;

            return h1.ComputeMurmurHash3();
        }
    }
   
}
