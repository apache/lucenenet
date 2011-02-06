/**
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

using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

namespace Lucene.Net.Util
{
    public class OpenBitSetDISI : OpenBitSet
    {
        /** Construct an OpenBitSetDISI with its bits set
         * from the doc ids of the given DocIdSetIterator.
         * Also give a maximum size one larger than the largest doc id for which a
         * bit may ever be set on this OpenBitSetDISI.
         */
        public OpenBitSetDISI(DocIdSetIterator disi, int maxSize)
            : base(maxSize)
        {
            InPlaceOr(disi);
        }

        /** Construct an OpenBitSetDISI with no bits set, and a given maximum size
         * one larger than the largest doc id for which a bit may ever be set
         * on this OpenBitSetDISI.
         */
        public OpenBitSetDISI(int maxSize)
            : base(maxSize)
        {
        }

        /**
         * Perform an inplace OR with the doc ids from a given DocIdSetIterator,
         * setting the bit for each such doc id.
         * These doc ids should be smaller than the maximum size passed to the
         * constructor.
         */
        public void InPlaceOr(DocIdSetIterator disi)
        {
            while (disi.Next() && (disi.Doc() < Size()))
            {
                FastSet(disi.Doc());
            }
        }

        /**
         * Perform an inplace AND with the doc ids from a given DocIdSetIterator,
         * leaving only the bits set for which the doc ids are in common.
         * These doc ids should be smaller than the maximum size passed to the
         * constructor.
         */
        public void InPlaceAnd(DocIdSetIterator disi)
        {
            int index = NextSetBit(0);
            int lastNotCleared = -1;
            while ((index != -1) && disi.SkipTo(index))
            {
                while ((index != -1) && (index < disi.Doc()))
                {
                    FastClear(index);
                    index = NextSetBit(index + 1);
                }
                if (index == disi.Doc())
                {
                    lastNotCleared = index;
                    index++;
                }
                System.Diagnostics.Debug.Assert((index == -1) || (index > disi.Doc()));
            }
            Clear(lastNotCleared + 1, Size());
        }

        /**
         * Perform an inplace NOT with the doc ids from a given DocIdSetIterator,
         * clearing all the bits for each such doc id.
         * These doc ids should be smaller than the maximum size passed to the
         * constructor.
         */
        public void InPlaceNot(DocIdSetIterator disi)
        {
            while (disi.Next() && (disi.Doc() < Size()))
            {
                FastClear(disi.Doc());
            }
        }

        /**
         * Perform an inplace XOR with the doc ids from a given DocIdSetIterator,
         * flipping all the bits for each such doc id.
         * These doc ids should be smaller than the maximum size passed to the
         * constructor.
         */
        public void InPlaceXor(DocIdSetIterator disi)
        {
            while (disi.Next() && (disi.Doc() < Size()))
            {
                FastFlip(disi.Doc());
            }
        }
    }
}
