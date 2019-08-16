using System;
using System.Collections.Generic;
using System.IO;
using Console = Lucene.Net.Support.SystemConsole;

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

    using CharacterRunAutomaton = Lucene.Net.Util.Automaton.CharacterRunAutomaton;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    /// <summary>
    /// Analyzer for testing.
    /// <para/>
    /// This analyzer is a replacement for Whitespace/Simple/KeywordAnalyzers
    /// for unit tests. If you are testing a custom component such as a queryparser
    /// or analyzer-wrapper that consumes analysis streams, its a great idea to test
    /// it with this analyzer instead. MockAnalyzer has the following behavior:
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///             By default, the assertions in <see cref="MockTokenizer"/> are turned on for extra
    ///             checks that the consumer is consuming properly. These checks can be disabled
    ///             with <see cref="EnableChecks"/>.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             Payload data is randomly injected into the stream for more thorough testing
    ///             of payloads.
    ///         </description>
    ///     </item>
    /// </list>
    /// </summary>
    /// <seealso cref="MockTokenizer"/>
    public sealed class MockAnalyzer : Analyzer
    {
        private readonly CharacterRunAutomaton runAutomaton;
        private readonly bool lowerCase;
        private readonly CharacterRunAutomaton filter;
        private int positionIncrementGap;
        private int? offsetGap;
        private readonly Random random;
        private IDictionary<string, int?> previousMappings = new Dictionary<string, int?>();
        private bool enableChecks = true;
        private int maxTokenLength = MockTokenizer.DEFAULT_MAX_TOKEN_LENGTH;

        /// <summary>
        /// Creates a new <see cref="MockAnalyzer"/>.
        /// </summary>
        /// <param name="random"> Random for payloads behavior </param>
        /// <param name="runAutomaton"> DFA describing how tokenization should happen (e.g. [a-zA-Z]+) </param>
        /// <param name="lowerCase"> true if the tokenizer should lowercase terms </param>
        /// <param name="filter"> DFA describing how terms should be filtered (set of stopwords, etc) </param>
        public MockAnalyzer(Random random, CharacterRunAutomaton runAutomaton, bool lowerCase, CharacterRunAutomaton filter)
            : base(PER_FIELD_REUSE_STRATEGY)
        {
            // TODO: this should be solved in a different way; Random should not be shared (!).
            this.random = new Random(random.Next());
            this.runAutomaton = runAutomaton;
            this.lowerCase = lowerCase;
            this.filter = filter;
        }

        /// <summary>
        /// Calls <c>MockAnalyzer(random, runAutomaton, lowerCase, MockTokenFilter.EMPTY_STOPSET, false)</c>.
        /// </summary>
        public MockAnalyzer(Random random, CharacterRunAutomaton runAutomaton, bool lowerCase)
            : this(random, runAutomaton, lowerCase, MockTokenFilter.EMPTY_STOPSET)
        {
        }

        /// <summary>
        /// Create a Whitespace-lowercasing analyzer with no stopwords removal.
        /// <para/>
        /// Calls <c>MockAnalyzer(random, MockTokenizer.WHITESPACE, true, MockTokenFilter.EMPTY_STOPSET, false)</c>.
        /// </summary>
        public MockAnalyzer(Random random)
            : this(random, MockTokenizer.WHITESPACE, true)
        {
        }

        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            MockTokenizer tokenizer = new MockTokenizer(reader, runAutomaton, lowerCase, maxTokenLength);
            tokenizer.EnableChecks = enableChecks;
            MockTokenFilter filt = new MockTokenFilter(tokenizer, filter);
            return new TokenStreamComponents(tokenizer, MaybePayload(filt, fieldName));
        }

        private TokenFilter MaybePayload(TokenFilter stream, string fieldName)
        {
            lock (this)
            {
                int? val;
                previousMappings.TryGetValue(fieldName, out val);
                if (val == null)
                {
                    val = -1; // no payloads
                    if (LuceneTestCase.Rarely(random))
                    {
                        switch (random.Next(3))
                        {
                            case 0: // no payloads
                                val = -1;
                                break;

                            case 1: // variable length payload
                                val = int.MaxValue;
                                break;

                            case 2: // fixed length payload
                                val = random.Next(12);
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
                    previousMappings[fieldName] = val; // save it so we are consistent for this field
                }

                if (val == -1)
                {
                    return stream;
                }
                else if (val == int.MaxValue)
                {
                    return new MockVariableLengthPayloadFilter(random, stream);
                }
                else
                {
                    return new MockFixedLengthPayloadFilter(random, stream, (int)val);
                }
            }
        }

        public int PositionIncrementGap // LUCENENET TODO: API change to SetPositionIncrementGap()
        {
            set
            {
                this.positionIncrementGap = value;
            }
        }

        public override int GetPositionIncrementGap(string fieldName)
        {
            return positionIncrementGap;
        }

        /// <summary>
        /// Sets an offset gap which will then be added to the offset when several fields with the same name are indexed </summary>
        public int OffsetGap // LUCENENET TODO: API change to SetOffsetGap()
        {
            set
            {
                this.offsetGap = value;
            }
        }

        /// <summary>
        /// Get the offset gap between tokens in fields if several fields with the same name were added. </summary>
        /// <param name="fieldName"> Currently not used, the same offset gap is returned for each field. </param>
        public override int GetOffsetGap(string fieldName)
        {
            return offsetGap == null ? base.GetOffsetGap(fieldName) : offsetGap.Value;
        }

        /// <summary>
        /// Toggle consumer workflow checking: if your test consumes tokenstreams normally you
        /// should leave this enabled.
        /// </summary>
        public bool EnableChecks // LUCENENET TODO: API Add getter
        {
            set
            {
                this.enableChecks = value;
            }
        }

        /// <summary>
        /// Toggle maxTokenLength for <see cref="MockTokenizer"/>.
        /// </summary>
        public int MaxTokenLength // LUCENENET TODO: API Add getter
        {
            set
            {
                this.maxTokenLength = value;
            }
        }
    }
}