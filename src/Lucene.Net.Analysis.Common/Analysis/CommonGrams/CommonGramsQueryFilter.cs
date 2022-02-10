// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using System;

namespace Lucene.Net.Analysis.CommonGrams
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
    /// Wrap a <see cref="CommonGramsFilter"/> optimizing phrase queries by only returning single
    /// words when they are not a member of a bigram.
    /// <para/>
    /// Example:
    /// <list type="bullet">
    ///     <item><description>query input to CommonGramsFilter: "the rain in spain falls mainly"</description></item>
    ///     <item><description>output of CommomGramsFilter/input to CommonGramsQueryFilter:
    ///     |"the, "the-rain"|"rain" "rain-in"|"in, "in-spain"|"spain"|"falls"|"mainly"</description></item>
    ///     <item><description>output of CommonGramsQueryFilter:"the-rain", "rain-in" ,"in-spain",
    ///     "falls", "mainly"</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// See:http://hudson.zones.apache.org/hudson/job/Lucene-trunk/javadoc//all/org/apache/lucene/analysis/TokenStream.html and
    /// http://svn.apache.org/viewvc/lucene/dev/trunk/lucene/src/java/org/apache/lucene/analysis/package.html?revision=718798
    /// </remarks>
    public sealed class CommonGramsQueryFilter : TokenFilter
    {
        private readonly ITypeAttribute typeAttribute;
        private readonly IPositionIncrementAttribute posIncAttribute;

        private State previous;
        private string previousType;
        private bool exhausted;

        /// <summary>
        /// Constructs a new CommonGramsQueryFilter based on the provided CommomGramsFilter 
        /// </summary>
        /// <param name="input"> CommonGramsFilter the QueryFilter will use </param>
        public CommonGramsQueryFilter(CommonGramsFilter input)
            : base(input)
        {
            typeAttribute = AddAttribute<ITypeAttribute>();
            posIncAttribute = AddAttribute<IPositionIncrementAttribute>();
        }

        /// <summary>
        /// This method is called by a consumer before it begins consumption using
        /// <see cref="IncrementToken()"/>.
        /// <para/>
        /// Resets this stream to a clean state. Stateful implementations must implement
        /// this method so that they can be reused, just as if they had been created fresh.
        /// <para/>
        /// If you override this method, always call <c>base.Reset()</c>, otherwise
        /// some internal state will not be correctly reset (e.g., <see cref="Tokenizer"/> will
        /// throw <see cref="InvalidOperationException"/> on further usage).
        /// </summary>
        /// <remarks>
        /// <b>NOTE:</b>
        /// The default implementation chains the call to the input <see cref="TokenStream"/>, so
        /// be sure to call <c>base.Reset()</c> when overriding this method.
        /// </remarks>
        public override void Reset()
        {
            base.Reset();
            previous = null;
            previousType = null;
            exhausted = false;
        }

        /// <summary>
        /// Output bigrams whenever possible to optimize queries. Only output unigrams
        /// when they are not a member of a bigram. Example:
        /// <list type="bullet">
        ///     <item><description>input: "the rain in spain falls mainly"</description></item>
        ///     <item><description>output:"the-rain", "rain-in" ,"in-spain", "falls", "mainly"</description></item>
        /// </list>
        /// </summary>
        public override bool IncrementToken()
        {
            while (!exhausted && m_input.IncrementToken())
            {
                State current = CaptureState();

                if (previous != null && !IsGramType)
                {
                    RestoreState(previous);
                    previous = current;
                    previousType = typeAttribute.Type;

                    if (IsGramType)
                    {
                        posIncAttribute.PositionIncrement = 1;
                    }
                    return true;
                }

                previous = current;
            }

            exhausted = true;

            if (previous is null || CommonGramsFilter.GRAM_TYPE.Equals(previousType, StringComparison.Ordinal))
            {
                return false;
            }

            RestoreState(previous);
            previous = null;

            if (IsGramType)
            {
                posIncAttribute.PositionIncrement = 1;
            }
            return true;
        }

        // ================================================= Helper Methods ================================================

        /// <summary>
        /// Convenience method to check if the current type is a gram type
        /// </summary>
        /// <returns> <c>true</c> if the current type is a gram type, <c>false</c> otherwise </returns>
        public bool IsGramType => CommonGramsFilter.GRAM_TYPE.Equals(typeAttribute.Type, StringComparison.Ordinal);
    }
}