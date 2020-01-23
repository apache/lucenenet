using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Support;
using Lucene.Net.Util.Automaton;

namespace Lucene.Net.Analysis
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
    /// A <see cref="TokenFilter"/> for testing that removes terms accepted by a DFA.
    /// <list type="bullet">
    ///     <item><description>Union a list of singletons to act like a <see cref="Analysis.Core.StopFilter"/>.</description></item>
    ///     <item><description>Use the complement to act like a <see cref="Analysis.Miscellaneous.KeepWordFilter"/>.</description></item>
    ///     <item><description>Use a regex like <c>.{12,}</c> to act like a <see cref="Analysis.Miscellaneous.LengthFilter"/>.</description></item>
    /// </list>
    /// </summary>
    public sealed class MockTokenFilter : TokenFilter
    {
        /// <summary>
        /// Empty set of stopwords </summary>
        public static readonly CharacterRunAutomaton EMPTY_STOPSET = new CharacterRunAutomaton(BasicAutomata.MakeEmpty());

        /// <summary>
        /// Set of common english stopwords </summary>
        public static readonly CharacterRunAutomaton ENGLISH_STOPSET = 
            new CharacterRunAutomaton(BasicOperations.Union(new Automaton[] {
                BasicAutomata.MakeString("a"), BasicAutomata.MakeString("an"), BasicAutomata.MakeString("and"), BasicAutomata.MakeString("are"),
                BasicAutomata.MakeString("as"), BasicAutomata.MakeString("at"), BasicAutomata.MakeString("be"), BasicAutomata.MakeString("but"),
                BasicAutomata.MakeString("by"), BasicAutomata.MakeString("for"), BasicAutomata.MakeString("if"), BasicAutomata.MakeString("in"),
                BasicAutomata.MakeString("into"), BasicAutomata.MakeString("is"), BasicAutomata.MakeString("it"), BasicAutomata.MakeString("no"),
                BasicAutomata.MakeString("not"), BasicAutomata.MakeString("of"), BasicAutomata.MakeString("on"), BasicAutomata.MakeString("or"),
                BasicAutomata.MakeString("such"), BasicAutomata.MakeString("that"), BasicAutomata.MakeString("the"), BasicAutomata.MakeString("their"),
                BasicAutomata.MakeString("then"), BasicAutomata.MakeString("there"), BasicAutomata.MakeString("these"), BasicAutomata.MakeString("they"),
                BasicAutomata.MakeString("this"), BasicAutomata.MakeString("to"), BasicAutomata.MakeString("was"), BasicAutomata.MakeString("will"),
                BasicAutomata.MakeString("with") } ));

        private readonly CharacterRunAutomaton filter;

        private readonly ICharTermAttribute termAtt;
        private readonly IPositionIncrementAttribute posIncrAtt;
        private int skippedPositions;

        /// <summary>
        /// Create a new <see cref="MockTokenFilter"/>.
        /// </summary>
        /// <param name="input"> <see cref="TokenStream"/> to filter </param>
        /// <param name="filter"> DFA representing the terms that should be removed. </param>
        public MockTokenFilter(TokenStream input, CharacterRunAutomaton filter)
            : base(input)
        {
            this.filter = filter;
            termAtt = AddAttribute<ICharTermAttribute>();
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
        }

        public override bool IncrementToken()
        {
            // TODO: fix me when posInc=false, to work like FilteringTokenFilter in that case and not return
            // initial token with posInc=0 ever

            // return the first non-stop word found
            skippedPositions = 0;
            while (m_input.IncrementToken())
            {
                if (!filter.Run(termAtt.Buffer, 0, termAtt.Length))
                {
                    posIncrAtt.PositionIncrement = posIncrAtt.PositionIncrement + skippedPositions;
                    return true;
                }
                skippedPositions += posIncrAtt.PositionIncrement;
            }
            // reached EOS -- return false
            return false;
        }

        public override void End()
        {
            base.End();
            posIncrAtt.PositionIncrement = posIncrAtt.PositionIncrement + skippedPositions;
        }

        public override void Reset()
        {
            base.Reset();
            skippedPositions = 0;
        }
    }
}