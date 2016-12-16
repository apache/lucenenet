using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis
{
    using System.IO;
    using CharacterRunAutomaton = Lucene.Net.Util.Automaton.CharacterRunAutomaton;

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

    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    /// <summary>
    /// Analyzer for testing
    /// <p>
    /// this analyzer is a replacement for Whitespace/Simple/KeywordAnalyzers
    /// for unit tests. If you are testing a custom component such as a queryparser
    /// or analyzer-wrapper that consumes analysis streams, its a great idea to test
    /// it with this analyzer instead. MockAnalyzer has the following behavior:
    /// <ul>
    ///   <li>By default, the assertions in <seealso cref="MockTokenizer"/> are turned on for extra
    ///       checks that the consumer is consuming properly. These checks can be disabled
    ///       with <seealso cref="#setEnableChecks(boolean)"/>.
    ///   <li>Payload data is randomly injected into the stream for more thorough testing
    ///       of payloads.
    /// </ul> </summary>
    /// <seealso cref= MockTokenizer </seealso>
    public sealed class MockAnalyzer : Analyzer
    {
        private readonly CharacterRunAutomaton RunAutomaton;
        private readonly bool LowerCase;
        private readonly CharacterRunAutomaton Filter;
        private int PositionIncrementGap_Renamed;
        private int? OffsetGap_Renamed;
        private readonly Random Random;
        private IDictionary<string, int?> PreviousMappings = new Dictionary<string, int?>();
        private bool EnableChecks_Renamed = true;
        private int MaxTokenLength_Renamed = MockTokenizer.DEFAULT_MAX_TOKEN_LENGTH;

        /// <summary>
        /// Creates a new MockAnalyzer.
        /// </summary>
        /// <param name="random"> Random for payloads behavior </param>
        /// <param name="runAutomaton"> DFA describing how tokenization should happen (e.g. [a-zA-Z]+) </param>
        /// <param name="lowerCase"> true if the tokenizer should lowercase terms </param>
        /// <param name="filter"> DFA describing how terms should be filtered (set of stopwords, etc) </param>
        public MockAnalyzer(Random random, CharacterRunAutomaton runAutomaton, bool lowerCase, CharacterRunAutomaton filter)
            : base(PER_FIELD_REUSE_STRATEGY)
        {
            // TODO: this should be solved in a different way; Random should not be shared (!).
            this.Random = new Random(random.Next());
            this.RunAutomaton = runAutomaton;
            this.LowerCase = lowerCase;
            this.Filter = filter;
        }

        /// <summary>
        /// Calls {@link #MockAnalyzer(Random, CharacterRunAutomaton, boolean, CharacterRunAutomaton)
        /// MockAnalyzer(random, runAutomaton, lowerCase, MockTokenFilter.EMPTY_STOPSET, false}).
        /// </summary>
        public MockAnalyzer(Random random, CharacterRunAutomaton runAutomaton, bool lowerCase)
            : this(random, runAutomaton, lowerCase, MockTokenFilter.EMPTY_STOPSET)
        {
        }

        /// <summary>
        /// Create a Whitespace-lowercasing analyzer with no stopwords removal.
        /// <p>
        /// Calls {@link #MockAnalyzer(Random, CharacterRunAutomaton, boolean, CharacterRunAutomaton)
        /// MockAnalyzer(random, MockTokenizer.WHITESPACE, true, MockTokenFilter.EMPTY_STOPSET, false}).
        /// </summary>
        public MockAnalyzer(Random random)
            : this(random, MockTokenizer.WHITESPACE, true)
        {
        }

        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            MockTokenizer tokenizer = new MockTokenizer(reader, RunAutomaton, LowerCase, MaxTokenLength_Renamed);
            tokenizer.EnableChecks = EnableChecks_Renamed;
            MockTokenFilter filt = new MockTokenFilter(tokenizer, Filter);
            return new TokenStreamComponents(tokenizer, MaybePayload(filt, fieldName));
        }

        private TokenFilter MaybePayload(TokenFilter stream, string fieldName)
        {
            lock (this)
            {
                int? val;
                PreviousMappings.TryGetValue(fieldName, out val);
                if (val == null)
                {
                    val = -1; // no payloads
                    if (LuceneTestCase.Rarely(Random))
                    {
                        switch (Random.Next(3))
                        {
                            case 0: // no payloads
                                val = -1;
                                break;

                            case 1: // variable length payload
                                val = int.MaxValue;
                                break;

                            case 2: // fixed length payload
                                val = Random.Next(12);
                                break;
                        }
                    }
                    if (LuceneTestCase.VERBOSE)
                    {
                        if (val == int.MaxValue)
                        {
                            Console.WriteLine("MockAnalyzer: field=" + fieldName + " gets variable length payloads");
                        }
                        else if (val != -1)
                        {
                            Console.WriteLine("MockAnalyzer: field=" + fieldName + " gets fixed length=" + val + " payloads");
                        }
                    }
                    PreviousMappings[fieldName] = val; // save it so we are consistent for this field
                }

                if (val == -1)
                {
                    return stream;
                }
                else if (val == int.MaxValue)
                {
                    return new MockVariableLengthPayloadFilter(Random, stream);
                }
                else
                {
                    return new MockFixedLengthPayloadFilter(Random, stream, (int)val);
                }
            }
        }

        public int PositionIncrementGap
        {
            set
            {
                this.PositionIncrementGap_Renamed = value;
            }
        }

        public override int GetPositionIncrementGap(string fieldName)
        {
            return PositionIncrementGap_Renamed;
        }

        /// <summary>
        /// Set a new offset gap which will then be added to the offset when several fields with the same name are indexed </summary>
        /// <param name="offsetGap"> The offset gap that should be used. </param>
        public int OffsetGap
        {
            set
            {
                this.OffsetGap_Renamed = value;
            }
        }

        /// <summary>
        /// Get the offset gap between tokens in fields if several fields with the same name were added. </summary>
        /// <param name="fieldName"> Currently not used, the same offset gap is returned for each field. </param>
        public override int GetOffsetGap(string fieldName)
        {
            return OffsetGap_Renamed == null ? base.GetOffsetGap(fieldName) : OffsetGap_Renamed.Value;
        }

        /// <summary>
        /// Toggle consumer workflow checking: if your test consumes tokenstreams normally you
        /// should leave this enabled.
        /// </summary>
        public bool EnableChecks
        {
            set
            {
                this.EnableChecks_Renamed = value;
            }
        }

        /// <summary>
        /// Toggle maxTokenLength for MockTokenizer
        /// </summary>
        public int MaxTokenLength
        {
            set
            {
                this.MaxTokenLength_Renamed = value;
            }
        }
    }
}