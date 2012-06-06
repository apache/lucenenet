/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Hunspell;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;
using LuceneVersion = Lucene.Net.Util.Version;

namespace Lucene.Net.Analyzers.Hunspell {
    [TestFixture]
    public class TestHunspellStemFilter : BaseTokenStreamTestCase {
        private class DutchAnalyzer : Analyzer {
            private readonly HunspellDictionary _dictionary;

            public DutchAnalyzer() {
                _dictionary = HunspellDictionaryLoader.Dictionary("nl_NL");
            }

            public override TokenStream TokenStream(String fieldName, TextReader reader) {
                TokenStream stream = new StandardTokenizer(LuceneVersion.LUCENE_29, reader);
                stream = new LowerCaseFilter(stream);
                stream = new HunspellStemFilter(stream, _dictionary);
                return stream;
            }

            public override TokenStream ReusableTokenStream(string fieldName, TextReader reader) {
                var streams = (SavedStreams)PreviousTokenStream;
                if (streams == null) {
                    streams = new SavedStreams();
                    streams.Tokenizer = new StandardTokenizer(LuceneVersion.LUCENE_29, reader);
                    streams.Filter = new HunspellStemFilter(new LowerCaseFilter(streams.Tokenizer), _dictionary);
                    PreviousTokenStream = streams;
                } else {
                    streams.Tokenizer.Reset(reader);
                    streams.Filter.Reset();
                }

                return streams.Filter;
            }

            #region Nested type: SavedStreams

            private class SavedStreams {
                public Tokenizer Tokenizer { get; set; }

                public TokenStream Filter { get; set; }
            }

            #endregion
        };

        private readonly DutchAnalyzer _dutchAnalyzer = new DutchAnalyzer();

        [Test]
        public void TestDutch() {
            AssertAnalyzesTo(_dutchAnalyzer, "huizen",
                new[] { "huizen", "huis" },
                new[] { 1, 0 });
            AssertAnalyzesTo(_dutchAnalyzer, "huis",
                new[] { "huis", "hui" },
                new[] { 1, 0 });
            AssertAnalyzesToReuse(_dutchAnalyzer, "huizen huis",
                new[] { "huizen", "huis", "huis", "hui" },
                new[] { 1, 0, 1, 0 });
            AssertAnalyzesToReuse(_dutchAnalyzer, "huis huizen",
                new[] { "huis", "hui", "huizen", "huis" },
                new[] { 1, 0, 1, 0 });
        }
    }
}