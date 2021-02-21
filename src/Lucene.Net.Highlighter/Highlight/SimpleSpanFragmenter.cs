using J2N.Numerics;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using System.Collections.Generic;

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
    /// <see cref="IFragmenter"/> implementation which breaks text up into same-size
    /// fragments but does not split up <see cref="Search.Spans.Spans"/>. This is a simple sample class.
    /// </summary>
    public class SimpleSpanFragmenter : IFragmenter
    {
        private const int DEFAULT_FRAGMENT_SIZE = 100;
        private readonly int fragmentSize; // LUCENENET: marked readonly
        private int currentNumFrags;
        private int position = -1;
        private readonly QueryScorer queryScorer; // LUCENENET: marked readonly
        private int waitForPos = -1;
        private int textSize;
        private ICharTermAttribute termAtt;
        private IPositionIncrementAttribute posIncAtt;
        private IOffsetAttribute offsetAtt;

        /// <param name="queryScorer"><see cref="QueryScorer"/> that was used to score hits</param>
        public SimpleSpanFragmenter(QueryScorer queryScorer)
            : this(queryScorer, DEFAULT_FRAGMENT_SIZE)
        {
        }

        /// <param name="queryScorer"><see cref="QueryScorer"/> that was used to score hits</param>
        /// <param name="fragmentSize">size in bytes of each fragment</param>
        public SimpleSpanFragmenter(QueryScorer queryScorer, int fragmentSize)
        {
            this.fragmentSize = fragmentSize;
            this.queryScorer = queryScorer;
        }

        /// <seealso cref="IFragmenter.IsNewFragment()"/>
        public virtual bool IsNewFragment()
        {
            position += posIncAtt.PositionIncrement;

            if (waitForPos == position)
            {
                waitForPos = -1;
            }
            else if (waitForPos != -1)
            {
                return false;
            }

            WeightedSpanTerm wSpanTerm = queryScorer.GetWeightedSpanTerm(termAtt.ToString());

            if (wSpanTerm != null)
            {
                IList<PositionSpan> positionSpans = wSpanTerm.PositionSpans;

                for (int i = 0; i < positionSpans.Count; i++)
                {
                    if (positionSpans[i].Start == position)
                    {
                        waitForPos = positionSpans[i].End + 1;
                        break;
                    }
                }
            }

            bool isNewFrag = offsetAtt.EndOffset >= (fragmentSize * currentNumFrags)
                && (textSize - offsetAtt.EndOffset) >= fragmentSize.TripleShift(1);


            if (isNewFrag)
            {
                currentNumFrags++;
            }

            return isNewFrag;
        }

        /// <seealso cref="IFragmenter.Start(string, TokenStream)"/>
        public virtual void Start(string originalText, TokenStream tokenStream)
        {
            position = -1;
            currentNumFrags = 1;
            textSize = originalText.Length;
            termAtt = tokenStream.AddAttribute<ICharTermAttribute>();
            posIncAtt = tokenStream.AddAttribute<IPositionIncrementAttribute>();
            offsetAtt = tokenStream.AddAttribute<IOffsetAttribute>();
        }
    }
}