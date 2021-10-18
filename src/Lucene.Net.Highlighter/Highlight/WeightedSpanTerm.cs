using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.Highlight
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
    /// Lightweight class to hold term, weight, and positions used for scoring this term.
    /// </summary>
    public class WeightedSpanTerm : WeightedTerm
    {
        private bool _positionSensitive;
        private readonly JCG.List<PositionSpan> _positionSpans = new JCG.List<PositionSpan>();

        public WeightedSpanTerm(float weight, string term)
            : base(weight, term)
        {
            // LUCENENET NOTE: Duplicate instantiation
            //_positionSpans = new List<PositionSpan>();
        }

        public WeightedSpanTerm(float weight, string term, bool positionSensitive)
            : base(weight, term)
        {
            _positionSensitive = positionSensitive;
        }

        /// <summary>
        /// Checks to see if this term is valid at <paramref name="position"/>.
        /// </summary>
        /// <param name="position">to check against valid term postions</param>
        /// <returns>true iff this term is a hit at this position</returns>
        public virtual bool CheckPosition(int position)
        {
            // There would probably be a slight speed improvement if PositionSpans
            // where kept in some sort of priority queue - that way this method
            // could
            // bail early without checking each PositionSpan.

            foreach (var positionSpan in _positionSpans)
            {
                if ((position >= positionSpan.Start) && (position <= positionSpan.End))
                {
                    return true;
                }
            }

            return false;
        }

        public virtual void AddPositionSpans(IList<PositionSpan> positionSpans)
        {
            this._positionSpans.AddRange(positionSpans);
        }

        public virtual bool IsPositionSensitive
        {
            get => _positionSensitive;
            set => this._positionSensitive = value;
        }

        public virtual IList<PositionSpan> PositionSpans => _positionSpans;
    }
}