using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;

#nullable enable

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
    /// Tests for <see cref="BaseTokenStreamTestCase"/>.
    /// </summary>
    [TestFixture]
    public class TestBaseTokenStreamTestCase : BaseTokenStreamTestCase
    {
        [Test]
        [LuceneNetSpecific] // lucenenet#271
        public void TestTokenStreamNotUsedAfterDispose()
        {
            DisposeTrackingLowerCaseFilter? leakedReference = null;

            using (var a = Analyzer.NewAnonymous((_, reader) =>
                   {
                       // copied from StandardAnalyzer, but purposefully leaking our dispose tracking reference
                       var src = new StandardTokenizer(LuceneVersion.LUCENE_48, reader);
                       src.MaxTokenLength = 255;
                       TokenStream tok = new StandardFilter(LuceneVersion.LUCENE_48, src);
                       tok = leakedReference = new DisposeTrackingLowerCaseFilter(LuceneVersion.LUCENE_48, tok);
                       return new TokenStreamComponents(src, tok);
                   }))
            {
                CheckAnalysisConsistency(Random, a, false, "This is a test to make sure dispose is called only once");
            }

            Assert.IsNotNull(leakedReference);
            Assert.IsTrue(leakedReference!.IsDisposed, "Dispose was not called on the token stream");
        }

        /// <summary>
        /// LUCENENET specific class for <see cref="TestBaseTokenStreamTestCase.TestTokenStreamNotUsedAfterDispose"/>
        /// that tracks whether <see cref="TokenStream.Dispose()"/> was called, and throws an exception
        /// if it is called more than once, or if other operations are called after it is disposed.
        /// Code copied from <see cref="LowerCaseFilter"/>.
        /// </summary>
        private class DisposeTrackingLowerCaseFilter : TokenFilter
        {
            private readonly CharacterUtils charUtils;
            private readonly ICharTermAttribute termAtt;

            public DisposeTrackingLowerCaseFilter(LuceneVersion matchVersion, TokenStream @in)
                : base(@in)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                charUtils = CharacterUtils.GetInstance(matchVersion);
            }

            protected override void Dispose(bool disposing)
            {
                if (!IsDisposed)
                {
                    IsDisposed = true;
                    base.Dispose(disposing);
                }
                else
                {
                    throw new AssertionException("Dispose called more than once on TokenStream instance");
                }
            }

            public override bool IncrementToken()
            {
                if (IsDisposed)
                {
                    throw new AssertionException("IncrementToken called after Dispose");
                }

                if (m_input.IncrementToken())
                {
                    charUtils.ToLower(termAtt.Buffer, 0, termAtt.Length);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public override void End()
            {
                if (IsDisposed)
                {
                    throw new AssertionException("End called after Dispose");
                }

                base.End();
            }

            public override void Reset()
            {
                if (IsDisposed)
                {
                    throw new AssertionException("Reset called after Dispose");
                }

                base.Reset();
            }

            public override void Close()
            {
                if (IsDisposed)
                {
                    throw new AssertionException("Close called after Dispose");
                }

                base.Close();
            }

            public bool IsDisposed { get; private set; }
        }
    }
}
