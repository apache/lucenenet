// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Attributes;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.Ga
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
    /// Test the Irish lowercase filter.
    /// </summary>
    public class TestIrishLowerCaseFilter : BaseTokenStreamTestCase
    {

        /// <summary>
        /// Test lowercase
        /// </summary>
        [Test]
        public virtual void TestIrishLowerCaseFilter_()
        {
            TokenStream stream = new MockTokenizer(new StringReader("nAthair tUISCE hARD"), MockTokenizer.WHITESPACE, false);
            IrishLowerCaseFilter filter = new IrishLowerCaseFilter(stream);
            AssertTokenStreamContents(filter, new string[] { "n-athair", "t-uisce", "hard" });
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new IrishLowerCaseFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }

        /// <summary>
        /// Test that prothesis output is correct across all upper vowels and fadas,
        /// and for both n- and t- prefixes.
        /// </summary>
        [Test, LuceneNetSpecific] // Issue #1150
        public virtual void TestProthesisCorrectness()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new IrishLowerCaseFilter(tokenizer));
            });

            // Plain upper vowels
            CheckOneTerm(a, "nAthair", "n-athair");
            CheckOneTerm(a, "nEan", "n-ean");
            CheckOneTerm(a, "nIasc", "n-iasc");
            CheckOneTerm(a, "nOiche", "n-oiche");
            CheckOneTerm(a, "nUll", "n-ull");
            CheckOneTerm(a, "tAthair", "t-athair");
            CheckOneTerm(a, "tEan", "t-ean");
            CheckOneTerm(a, "tIasc", "t-iasc");
            CheckOneTerm(a, "tOiche", "t-oiche");
            CheckOneTerm(a, "tUll", "t-ull");

            // Accented vowels (fadas)
            CheckOneTerm(a, "nÁit", "n-áit");
            CheckOneTerm(a, "nÉan", "n-éan");
            CheckOneTerm(a, "nÍoc", "n-íoc");
            CheckOneTerm(a, "nÓg", "n-óg");
            CheckOneTerm(a, "nÚll", "n-úll");
            CheckOneTerm(a, "tÁit", "t-áit");
            CheckOneTerm(a, "tÉan", "t-éan");
            CheckOneTerm(a, "tÍoc", "t-íoc");
            CheckOneTerm(a, "tÓg", "t-óg");
            CheckOneTerm(a, "tÚll", "t-úll");
        }

        /// <summary>
        /// Regression test for issue #1150: ArgumentOutOfRangeException in IrishLowerCaseFilter.
        ///
        /// The bug: when the n-/t-prothesis path runs, idx is set to 2 but the span length was
        /// left as chLen (full term length) instead of chLen - idx. This creates a span that reads
        /// chArray[idx .. idx+chLen], which exceeds the buffer when the buffer is sized just large
        /// enough for chLen chars.
        ///
        /// We reproduce by using a custom TokenStream that writes directly into the internal
        /// CharTermAttributeImpl.termBuffer field, bypassing the Oversize over-allocation that
        /// all public paths go through, so the buffer is sized to exactly the post-prothesis
        /// term length. The buggy code then asks for chArray[2..2+chLen] which is 2 chars past
        /// the end.
        /// </summary>
        [Test, LuceneNetSpecific] // Issue #1150
        public virtual void TestProthesis_SpanBoundaryThrows()
        {
            // "nAthair" has length 7; after prothesis it becomes "n-athair" (length 8).
            // We pre-size the buffer to exactly 8 so that chArray[2..2+8] = chArray[2..10]
            // exceeds the buffer and throws ArgumentOutOfRangeException on the buggy code.
            const string input = "nAthair";
            const string expected = "n-athair";

            var source = new TightBufferTokenStream(input);
            var filter = new IrishLowerCaseFilter(source);
            AssertTokenStreamContents(filter, new[] { expected });
        }

        /// <summary>
        /// A TokenStream that emits a single token with the backing buffer sized to exactly
        /// the post-prothesis term length (term.Length + 1), bypassing Oversize over-allocation.
        /// This forces the tight-buffer condition that triggers the span length bug in
        /// IrishLowerCaseFilter when idx=2 and spanLen is incorrectly computed as chLen.
        /// </summary>
        private sealed class TightBufferTokenStream : TokenStream
        {
            private readonly string _term;
            private bool _done;
            private readonly CharTermAttribute _termAtt;

            public TightBufferTokenStream(string term) : base()
            {
                _term = term;
                _termAtt = (CharTermAttribute)AddAttribute<ICharTermAttribute>();
            }

            public override bool IncrementToken()
            {
                if (_done) return false;
                _done = true;
                ClearAttributes();
                // Set the backing buffer to exactly postProthesisLen chars — no Oversize slack.
                // IrishLowerCaseFilter.IncrementToken calls ResizeBuffer(chLen+1) which is a no-op
                // when the buffer is already that size, leaving the span with no room for idx=2.
                int postProthesisLen = _term.Length + 1;
                _termAtt.termBuffer = new char[postProthesisLen];
                _term.CopyTo(0, _termAtt.termBuffer, 0, _term.Length);
                _termAtt.Length = _term.Length;
                return true;
            }

            public override void Reset()
            {
                base.Reset();
                _done = false;
            }
        }
    }
}
