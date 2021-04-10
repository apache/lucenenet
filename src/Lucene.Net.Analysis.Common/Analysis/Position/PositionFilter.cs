// Lucene version compatibility level 4.8.1
using System;
using Lucene.Net.Analysis.TokenAttributes;

namespace Lucene.Net.Analysis.Position
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
    /// Set the positionIncrement of all tokens to the "positionIncrement",
    /// except the first return token which retains its original positionIncrement value.
    /// The default positionIncrement value is zero. </summary>
    /// @deprecated (4.4) <see cref="PositionFilter"/> makes <see cref="TokenStream"/> graphs inconsistent
    ///             which can cause highlighting bugs. Its main use-case being to make
    ///             QueryParser
    ///             generate boolean queries instead of phrase queries, it is now advised to use
    ///             <c>QueryParser.AutoGeneratePhraseQueries = true</c>
    ///             (for simple cases) or to override <c>QueryParser.NewFieldQuery</c>. 
    [Obsolete("(4.4) PositionFilter makes TokenStream graphs inconsistent")]
    public sealed class PositionFilter : TokenFilter
    {
        /// <summary>
        /// Position increment to assign to all but the first token - default = 0 </summary>
        private readonly int positionIncrement;

        /// <summary>
        /// The first token must have non-zero positionIncrement * </summary>
        private bool firstTokenPositioned = false;

        private readonly IPositionIncrementAttribute posIncrAtt;

        /// <summary>
        /// Constructs a <see cref="PositionFilter"/> that assigns a position increment of zero to
        /// all but the first token from the given input stream.
        /// </summary>
        /// <param name="input"> the input stream </param>
        public PositionFilter(TokenStream input)
            : this(input, 0)
        {
        }

        /// <summary>
        /// Constructs a <see cref="PositionFilter"/> that assigns the given position increment to
        /// all but the first token from the given input stream.
        /// </summary>
        /// <param name="input"> the input stream </param>
        /// <param name="positionIncrement"> position increment to assign to all but the first
        ///  token from the input stream </param>
        public PositionFilter(TokenStream input, int positionIncrement)
            : base(input)
        {
            if (positionIncrement < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(positionIncrement), "positionIncrement may not be negative"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.positionIncrement = positionIncrement;
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                if (firstTokenPositioned)
                {
                    posIncrAtt.PositionIncrement = positionIncrement;
                }
                else
                {
                    firstTokenPositioned = true;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void Reset()
        {
            base.Reset();
            firstTokenPositioned = false;
        }
    }
}