using Lucene.Net.Analysis.TokenAttributes;

namespace Lucene.Net.Analysis
{
    using Lucene.Net.Support;
    using BasicAutomata = Lucene.Net.Util.Automaton.BasicAutomata;

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

    using BasicOperations = Lucene.Net.Util.Automaton.BasicOperations;
    using CharacterRunAutomaton = Lucene.Net.Util.Automaton.CharacterRunAutomaton;

    /// <summary>
    /// A tokenfilter for testing that removes terms accepted by a DFA.
    /// <ul>
    ///  <li>Union a list of singletons to act like a stopfilter.
    ///  <li>Use the complement to act like a keepwordfilter
    ///  <li>Use a regex like <code>.{12,}</code> to act like a lengthfilter
    /// </ul>
    /// </summary>
    public sealed class MockTokenFilter : TokenFilter
    {
        /// <summary>
        /// Empty set of stopwords </summary>
        public static readonly CharacterRunAutomaton EMPTY_STOPSET = new CharacterRunAutomaton(BasicAutomata.MakeEmpty());

        /// <summary>
        /// Set of common english stopwords </summary>
        public static readonly CharacterRunAutomaton ENGLISH_STOPSET = new CharacterRunAutomaton(BasicOperations.Union(Arrays.AsList(BasicAutomata.MakeString("a"), BasicAutomata.MakeString("an"), BasicAutomata.MakeString("and"), BasicAutomata.MakeString("are"), BasicAutomata.MakeString("as"), BasicAutomata.MakeString("at"), BasicAutomata.MakeString("be"), BasicAutomata.MakeString("but"), BasicAutomata.MakeString("by"), BasicAutomata.MakeString("for"), BasicAutomata.MakeString("if"), BasicAutomata.MakeString("in"), BasicAutomata.MakeString("into"), BasicAutomata.MakeString("is"), BasicAutomata.MakeString("it"), BasicAutomata.MakeString("no"), BasicAutomata.MakeString("not"), BasicAutomata.MakeString("of"), BasicAutomata.MakeString("on"), BasicAutomata.MakeString("or"), BasicAutomata.MakeString("such"), BasicAutomata.MakeString("that"), BasicAutomata.MakeString("the"), BasicAutomata.MakeString("their"), BasicAutomata.MakeString("then"), BasicAutomata.MakeString("there"), BasicAutomata.MakeString("these"), BasicAutomata.MakeString("they"), BasicAutomata.MakeString("this"), BasicAutomata.MakeString("to"), BasicAutomata.MakeString("was"), BasicAutomata.MakeString("will"), BasicAutomata.MakeString("with"))));

        private readonly CharacterRunAutomaton Filter;

        private readonly ICharTermAttribute TermAtt;
        private readonly IPositionIncrementAttribute PosIncrAtt;
        private int SkippedPositions;

        /// <summary>
        /// Create a new MockTokenFilter.
        /// </summary>
        /// <param name="input"> TokenStream to filter </param>
        /// <param name="filter"> DFA representing the terms that should be removed. </param>
        public MockTokenFilter(TokenStream input, CharacterRunAutomaton filter)
            : base(input)
        {
            this.Filter = filter;
            TermAtt = AddAttribute<ICharTermAttribute>();
            PosIncrAtt = AddAttribute<IPositionIncrementAttribute>();
        }

        public override bool IncrementToken()
        {
            // TODO: fix me when posInc=false, to work like FilteringTokenFilter in that case and not return
            // initial token with posInc=0 ever

            // return the first non-stop word found
            SkippedPositions = 0;
            while (m_input.IncrementToken())
            {
                if (!Filter.Run(TermAtt.Buffer, 0, TermAtt.Length))
                {
                    PosIncrAtt.PositionIncrement = PosIncrAtt.PositionIncrement + SkippedPositions;
                    return true;
                }
                SkippedPositions += PosIncrAtt.PositionIncrement;
            }
            // reached EOS -- return false
            return false;
        }

        public override void End()
        {
            base.End();
            PosIncrAtt.PositionIncrement = PosIncrAtt.PositionIncrement + SkippedPositions;
        }

        public override void Reset()
        {
            base.Reset();
            SkippedPositions = 0;
        }
    }
}