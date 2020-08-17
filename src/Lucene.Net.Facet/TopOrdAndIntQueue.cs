using Lucene.Net.Util;

namespace Lucene.Net.Facet
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
    /// Keeps highest results, first by largest <see cref="int"/> value,
    /// then tie break by smallest ord.
    /// <para/>
    /// NOTE: This was TopOrdAndIntQueue in Lucene
    /// </summary>
    public class TopOrdAndInt32Queue : PriorityQueue<OrdAndValue<int>>
    {
        // LUCENENET specific - de-nested OrdAndValue and made it into a generic struct
        // so it can be used with this class and TopOrdAndSingleQueue

        /// <summary>
        /// Sole constructor.
        /// </summary>
        public TopOrdAndInt32Queue(int topN)
            : base(topN, false)
        {
        }

        protected internal override bool LessThan(OrdAndValue<int> a, OrdAndValue<int> b)
        {
            if (a.Value < b.Value)
            {
                return true;
            }
            else if (a.Value > b.Value)
            {
                return false;
            }
            else
            {
                return a.Ord > b.Ord;
            }
        }
    }
}