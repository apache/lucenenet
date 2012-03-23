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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Highlight;

namespace Lucene.Net.Search.Highlight
{
    /**
     * Lightweight class to hold term, weight, and positions used for scoring this
     * term.
     */

    public class WeightedSpanTerm : WeightedTerm
    {
        private bool positionSensitive;
        private List<PositionSpan> positionSpans = new List<PositionSpan>();

        /**
         * @param weight
         * @param term
         */

        public WeightedSpanTerm(float weight, String term)
            : base(weight, term)
        {

            this.positionSpans = new List<PositionSpan>();
        }

        /**
         * @param weight
         * @param term
         * @param positionSensitive
         */

        public WeightedSpanTerm(float weight, String term, bool positionSensitive)
            : base(weight, term)
        {

            this.positionSensitive = positionSensitive;
        }

        /**
         * Checks to see if this term is valid at <code>position</code>.
         *
         * @param position
         *            to check against valid term postions
         * @return true iff this term is a hit at this position
         */

        public bool checkPosition(int position)
        {
            // There would probably be a slight speed improvement if PositionSpans
            // where kept in some sort of priority queue - that way this method
            // could
            // bail early without checking each PositionSpan.

            foreach (var positionSpan in positionSpans)
            {
                if (((position >= positionSpan.Start) && (position <= positionSpan.End)))
                {
                    return true;
                }
            }

            return false;
        }

        public void addPositionSpans(List<PositionSpan> positionSpans)
        {
            this.positionSpans.AddRange(positionSpans);
        }

        public bool isPositionSensitive()
        {
            return positionSensitive;
        }

        public void setPositionSensitive(bool positionSensitive)
        {
            this.positionSensitive = positionSensitive;
        }

        public List<PositionSpan> getPositionSpans()
        {
            return positionSpans;
        }
    }


    // Utility class to store a Span
    public class PositionSpan
    {
        public int Start { get; private set; }
        public int End { get; private set; }

        public PositionSpan(int start, int end)
        {
            this.Start = start;
            this.End = end;
        }
    }
}
