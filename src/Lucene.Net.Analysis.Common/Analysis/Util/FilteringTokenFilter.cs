using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Analysis.Util
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
    /// Abstract base class for TokenFilters that may remove tokens.
    /// You have to implement <seealso cref="#accept"/> and return a boolean if the current
    /// token should be preserved. <seealso cref="#incrementToken"/> uses this method
    /// to decide if a token should be passed to the caller.
    /// <para><a name="lucene_match_version" />As of Lucene 4.4, an
    /// <seealso cref="IllegalArgumentException"/> is thrown when trying to disable position
    /// increments when filtering terms.
    /// </para>
    /// </summary>
    public abstract class FilteringTokenFilter : TokenFilter
    {

        private static void CheckPositionIncrement(LuceneVersion version, bool enablePositionIncrements)
        {
            if (!enablePositionIncrements &&
#pragma warning disable 612, 618
                version.OnOrAfter(LuceneVersion.LUCENE_44))
#pragma warning restore 612, 618
            {
                throw new System.ArgumentException("enablePositionIncrements=false is not supported anymore as of Lucene 4.4 as it can create broken token streams");
            }
        }

        protected internal readonly LuceneVersion version;
        private readonly IPositionIncrementAttribute posIncrAtt;
        private bool enablePositionIncrements; // no init needed, as ctor enforces setting value!
        private bool first = true;
        private int skippedPositions;

        /// <summary>
        /// Create a new <seealso cref="FilteringTokenFilter"/>. </summary>
        /// <param name="version">                  the <a href="#lucene_match_version">Lucene match version</a> </param>
        /// <param name="enablePositionIncrements"> whether to increment position increments when filtering out terms </param>
        /// <param name="input">                    the input to consume </param>
        /// @deprecated enablePositionIncrements=false is not supported anymore as of Lucene 4.4 
        [Obsolete("enablePositionIncrements=false is not supported anymore as of Lucene 4.4")]
        public FilteringTokenFilter(Lucene.Net.Util.LuceneVersion version, bool enablePositionIncrements, TokenStream input)
            : this(version, input)
        {
            CheckPositionIncrement(version, enablePositionIncrements);
            this.enablePositionIncrements = enablePositionIncrements;
        }

        /// <summary>
        /// Create a new <seealso cref="FilteringTokenFilter"/>. </summary>
        /// <param name="version"> the Lucene match version </param>
        /// <param name="in">      the <seealso cref="TokenStream"/> to consume </param>
        public FilteringTokenFilter(LuceneVersion version, TokenStream @in)
            : base(@in)
        {
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            this.version = version;
            this.enablePositionIncrements = true;
        }

        /// <summary>
        /// Override this method and return if the current input token should be returned by <seealso cref="#incrementToken"/>. </summary>
        protected internal abstract bool Accept();

        public override sealed bool IncrementToken()
        {
            if (enablePositionIncrements)
            {
                skippedPositions = 0;
                while (input.IncrementToken())
                {
                    if (Accept())
                    {
                        if (skippedPositions != 0)
                        {
                            posIncrAtt.PositionIncrement = posIncrAtt.PositionIncrement + skippedPositions;
                        }
                        return true;
                    }
                    skippedPositions += posIncrAtt.PositionIncrement;
                }
            }
            else
            {
                while (input.IncrementToken())
                {
                    if (Accept())
                    {
                        if (first)
                        {
                            // first token having posinc=0 is illegal.
                            if (posIncrAtt.PositionIncrement == 0)
                            {
                                posIncrAtt.PositionIncrement = 1;
                            }
                            first = false;
                        }
                        return true;
                    }
                }
            }
            // reached EOS -- return false
            return false;
        }

        public override void Reset()
        {
            base.Reset();
            first = true;
            skippedPositions = 0;
        }

        /// <seealso cref= #setEnablePositionIncrements(boolean) </seealso>
        public virtual bool EnablePositionIncrements
        {
            get
            {
                return enablePositionIncrements;
            }
            // LUCENENET TODO:
            // deprecated enablePositionIncrements=false is not supported anymore as of Lucene 4.4
            // There doesn't appear to be a way to apply [Obsolete] on a property setter only. The only way
            // to make it show the obsolete warning is to change this back to separate Get and Set methods.
            set
            {
                CheckPositionIncrement(version, value);
                this.enablePositionIncrements = value;
            }
        }

        public override void End()
        {
            base.End();
            if (enablePositionIncrements)
            {
                posIncrAtt.PositionIncrement = posIncrAtt.PositionIncrement + skippedPositions;
            }
        }
    }
}