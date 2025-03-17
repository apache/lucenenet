using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.TokenAttributes.Extensions;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;

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
        private sealed class DisposeTrackingLowerCaseFilter : TokenFilter, IDisposable
        {
            private readonly CharacterUtils charUtils;
            private readonly ICharTermAttribute termAtt;

            public DisposeTrackingLowerCaseFilter(LuceneVersion matchVersion, TokenStream @in)
                : base(@in)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                charUtils = CharacterUtils.GetInstance(matchVersion);
            }

            public void Dispose()
            {
                if (!IsDisposed)
                {
                    IsDisposed = true;
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


        [Test]
        [LuceneNetSpecific]
        public virtual void TestTokenStream_Dispose_AllDisposable()
        {
            DisposableKeywordTokenizer? tokenizer = null;
            DisposableTokenFilter? filter = null;
            DisposableTokenFilter? filter2 = null;
            DisposableTokenFilter? filter3 = null;

            using (Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                tokenizer = new DisposableKeywordTokenizer(reader);
                filter = new DisposableTokenFilter(tokenizer);
                filter2 = new DisposableTokenFilter(filter);
                filter3 = new DisposableTokenFilter(filter2);
                return new TokenStreamComponents(tokenizer, filter3);
            }))
            {
                CheckOneTerm(analyzer, "foo", "foo");
                CheckOneTerm(analyzer, "bar", "bar");
            }

            Assert.AreEqual(1, tokenizer!.disposeCount);
            Assert.AreEqual(1, filter!.disposeCount);
            Assert.AreEqual(1, filter2!.disposeCount);
            Assert.AreEqual(1, filter3!.disposeCount);
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestTokenStream_Dispose_DisposableTokenizer_OneDisposableFilter()
        {
            DisposableKeywordTokenizer? tokenizer = null;
            NonDisposableTokenFilter? filter = null;
            DisposableTokenFilter? filter2 = null;
            NonDisposableTokenFilter? filter3 = null;

            using (Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                tokenizer = new DisposableKeywordTokenizer(reader);
                filter = new NonDisposableTokenFilter(tokenizer);
                filter2 = new DisposableTokenFilter(filter);
                filter3 = new NonDisposableTokenFilter(filter2);
                return new TokenStreamComponents(tokenizer, filter3);
            }))
            {
                CheckOneTerm(analyzer, "foo", "foo");
                CheckOneTerm(analyzer, "bar", "bar");
            }

            Assert.AreEqual(1, tokenizer!.disposeCount);
            Assert.AreEqual(1, filter2!.disposeCount);
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestTokenStream_Dispose_DisposableTokenizer_TwoDisposableFilters()
        {
            DisposableKeywordTokenizer? tokenizer = null;
            NonDisposableTokenFilter? filter = null;
            NonDisposableTokenFilter? filter2 = null;
            DisposableTokenFilter? filter3 = null;
            NonDisposableTokenFilter? filter4 = null;
            DisposableTokenFilter? filter5 = null;

            using (Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                tokenizer = new DisposableKeywordTokenizer(reader);
                filter = new NonDisposableTokenFilter(tokenizer);
                filter2 = new NonDisposableTokenFilter(filter);
                filter3 = new DisposableTokenFilter(filter2);
                filter4 = new NonDisposableTokenFilter(filter3);
                filter5 = new DisposableTokenFilter(filter4);
                return new TokenStreamComponents(tokenizer, filter5);
            }))
            {
                CheckOneTerm(analyzer, "foo", "foo");
                CheckOneTerm(analyzer, "bar", "bar");
            }

            Assert.AreEqual(1, tokenizer!.disposeCount);
            Assert.AreEqual(1, filter3!.disposeCount);
            Assert.AreEqual(1, filter5!.disposeCount);
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestTokenStream_Dispose_NonDisposableTokenizer_OneDisposableFilter()
        {
            NonDisposableKeywordTokenizer? tokenizer = null;
            NonDisposableTokenFilter? filter = null;
            NonDisposableTokenFilter? filter2 = null;
            DisposableTokenFilter? filter3 = null;

            using (Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                tokenizer = new NonDisposableKeywordTokenizer(reader);
                filter = new NonDisposableTokenFilter(tokenizer);
                filter2 = new NonDisposableTokenFilter(filter);
                filter3 = new DisposableTokenFilter(filter2);
                return new TokenStreamComponents(tokenizer, filter3);
            }))
            {
                CheckOneTerm(analyzer, "foo", "foo");
                CheckOneTerm(analyzer, "bar", "bar");
            }

            Assert.AreEqual(1, filter3!.disposeCount);
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestTokenStream_Dispose_NonDisposableTokenizer_TwoDisposableFilters()
        {
            NonDisposableKeywordTokenizer? tokenizer = null;
            NonDisposableTokenFilter? filter = null;
            NonDisposableTokenFilter? filter2 = null;
            DisposableTokenFilter? filter3 = null;
            NonDisposableTokenFilter? filter4 = null;
            DisposableTokenFilter? filter5 = null;

            using (Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                tokenizer = new NonDisposableKeywordTokenizer(reader);
                filter = new NonDisposableTokenFilter(tokenizer);
                filter2 = new NonDisposableTokenFilter(filter);
                filter3 = new DisposableTokenFilter(filter2);
                filter4 = new NonDisposableTokenFilter(filter3);
                filter5 = new DisposableTokenFilter(filter4);
                return new TokenStreamComponents(tokenizer, filter5);
            }))
            {
                CheckOneTerm(analyzer, "foo", "foo");
                CheckOneTerm(analyzer, "bar", "bar");
            }

            Assert.AreEqual(1, filter3!.disposeCount);
            Assert.AreEqual(1, filter5!.disposeCount);
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestTokenStream_DoubleDispose_NonDisposableTokenizer_TwoDisposableFilters()
        {
            NonDisposableKeywordTokenizer? tokenizer = null;
            NonDisposableTokenFilter? filter = null;
            NonDisposableTokenFilter? filter2 = null;
            DisposableTokenFilter? filter3 = null;
            NonDisposableTokenFilter? filter4 = null;
            DisposableTokenFilter? filter5 = null;

            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                tokenizer = new NonDisposableKeywordTokenizer(reader);
                filter = new NonDisposableTokenFilter(tokenizer);
                filter2 = new NonDisposableTokenFilter(filter);
                filter3 = new DisposableTokenFilter(filter2);
                filter4 = new NonDisposableTokenFilter(filter3);
                filter5 = new DisposableTokenFilter(filter4);
                return new TokenStreamComponents(tokenizer, filter5);
            });
            CheckOneTerm(analyzer, "foo", "foo");
            CheckOneTerm(analyzer, "bar", "bar");

            analyzer.Dispose();
            analyzer.Dispose();

            Assert.AreEqual(1, filter3!.disposeCount);
            Assert.AreEqual(1, filter5!.disposeCount);
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestTokenStream_ThrowOnDispose_NonDisposableTokenizer_TwoDisposableFilters()
        {
            NonDisposableKeywordTokenizer? tokenizer = null;
            NonDisposableTokenFilter? filter = null;
            NonDisposableTokenFilter? filter2 = null;
            DisposableTokenFilter? filter3 = null;
            NonDisposableTokenFilter? filter4 = null;
            DisposableTokenFilter? filter5 = null;

            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                tokenizer = new NonDisposableKeywordTokenizer(reader);
                filter = new NonDisposableTokenFilter(tokenizer);
                filter2 = new NonDisposableTokenFilter(filter);
                filter3 = new DisposableTokenFilter(filter2, throwOnDispose: true);
                filter4 = new NonDisposableTokenFilter(filter3);
                filter5 = new DisposableTokenFilter(filter4, throwOnDispose: true);
                return new TokenStreamComponents(tokenizer, filter5);
            });
            CheckOneTerm(analyzer, "foo", "foo");
            CheckOneTerm(analyzer, "bar", "bar");

            LuceneSystemException ex = Assert.Throws<LuceneSystemException>(analyzer.Dispose)!;
            Assert.AreEqual(1, ex.GetSuppressed().Length);
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestTokenStream_ThrowOnDispose_DisposableTokenizer_TwoDisposableFilters()
        {
            DisposableKeywordTokenizer? tokenizer = null;
            NonDisposableTokenFilter? filter = null;
            NonDisposableTokenFilter? filter2 = null;
            DisposableTokenFilter? filter3 = null;
            NonDisposableTokenFilter? filter4 = null;
            DisposableTokenFilter? filter5 = null;

            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                tokenizer = new DisposableKeywordTokenizer(reader, throwOnDispose: true);
                filter = new NonDisposableTokenFilter(tokenizer);
                filter2 = new NonDisposableTokenFilter(filter);
                filter3 = new DisposableTokenFilter(filter2, throwOnDispose: true);
                filter4 = new NonDisposableTokenFilter(filter3);
                filter5 = new DisposableTokenFilter(filter4, throwOnDispose: true);
                return new TokenStreamComponents(tokenizer, filter5);
            });
            CheckOneTerm(analyzer, "foo", "foo");
            CheckOneTerm(analyzer, "bar", "bar");

            LuceneSystemException ex = Assert.Throws<LuceneSystemException>(analyzer.Dispose)!;
            Assert.AreEqual(2, ex.GetSuppressed().Length);
        }

        private class PassThroughTokenFilter : TokenFilter
        {
            private readonly ICharTermAttribute termAtt;

            public PassThroughTokenFilter(TokenStream input)
                : base(input)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
            }

            public override void Reset() => base.Reset();

            public override sealed bool IncrementToken() => m_input.IncrementToken(); // Simply pass tokens through unchanged
        }

        private class DisposableTokenFilter : PassThroughTokenFilter, IDisposable
        {
            internal int disposeCount;
            internal int closeCount;
            private readonly bool throwOnDispose;

            public DisposableTokenFilter(TokenStream input, bool throwOnDispose = false)
                : base(input)
            {
                this.throwOnDispose = throwOnDispose;
            }

            public override void Close()
            {
                closeCount++;
                base.Close();
            }

            public void Dispose()
            {
                disposeCount++;
                if (throwOnDispose)
                {
                    throw RuntimeException.Create("disposal error");
                }
            }
        }

        private class NonDisposableTokenFilter : PassThroughTokenFilter
        {
            internal int closeCount;

            public NonDisposableTokenFilter(TokenStream input)
                : base(input)
            {
            }

            public override void Close()
            {
                closeCount++;
                base.Close();
            }
        }

        private class BaseKeywordTokenizer : Tokenizer
        {
            private bool done = false;
            private int finalOffset;
            private readonly ICharTermAttribute termAtt;
            private readonly IOffsetAttribute offsetAtt;

            public BaseKeywordTokenizer(TextReader input)
              : base(input)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();
            }

            public BaseKeywordTokenizer(AttributeFactory factory, TextReader input)
                : base(factory, input)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();
            }

            public override sealed bool IncrementToken()
            {
                if (!done)
                {
                    ClearAttributes();
                    done = true;
                    int upto = 0;
                    char[] buffer = termAtt.Buffer;
                    while (true)
                    {
                        int length = m_input.Read(buffer, upto, buffer.Length - upto);
                        if (length <= 0)
                        {
                            break;
                        }
                        upto += length;
                        if (upto == buffer.Length)
                        {
                            buffer = termAtt.ResizeBuffer(1 + buffer.Length);
                        }
                    }
                    termAtt.Length = upto;
                    finalOffset = CorrectOffset(upto);
                    offsetAtt.SetOffset(CorrectOffset(0), finalOffset);

                    // **Ensure we emit at least one token (even if empty)**
                    if (upto == 0)
                    {
                        termAtt.SetEmpty();
                    }

                    return true;
                }
                return false;
            }

            public override sealed void End()
            {
                base.End();
                // set final offset 
                offsetAtt.SetOffset(finalOffset, finalOffset);
            }

            public override void Reset()
            {
                base.Reset();
                this.done = false;
            }
        }

        private class DisposableKeywordTokenizer : BaseKeywordTokenizer, IDisposable
        {
            internal int disposeCount;
            internal int closeCount;
            private readonly bool throwOnDispose;

            public DisposableKeywordTokenizer(TextReader input, bool throwOnDispose = false)
              : base(input)
            {
                this.throwOnDispose = throwOnDispose;
            }

            public DisposableKeywordTokenizer(AttributeFactory factory, TextReader input)
                : base(factory, input)
            {
            }

            public override void Close()
            {
                closeCount++;
                base.Close();
            }

            public void Dispose()
            {
                disposeCount++;
                if (throwOnDispose)
                {
                    throw RuntimeException.Create("disposal error");
                }
            }
        }

        private class NonDisposableKeywordTokenizer : BaseKeywordTokenizer
        {
            internal int closeCount;

            public NonDisposableKeywordTokenizer(TextReader input)
              : base(input)
            {
            }

            public NonDisposableKeywordTokenizer(AttributeFactory factory, TextReader input)
                : base(factory, input)
            {
            }

            public override void Close()
            {
                closeCount++;
                base.Close();
            }
        }
    }
}
