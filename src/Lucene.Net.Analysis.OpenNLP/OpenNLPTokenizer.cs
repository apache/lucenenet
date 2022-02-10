// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.OpenNlp.Tools;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using opennlp.tools.util;
using System;
using System.IO;

namespace Lucene.Net.Analysis.OpenNlp
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
    /// Run OpenNLP SentenceDetector and <see cref="Tokenizer"/>.
    /// The last token in each sentence is marked by setting the <see cref="EOS_FLAG_BIT"/> in the <see cref="IFlagsAttribute"/>;
    /// following filters can use this information to apply operations to tokens one sentence at a time.
    /// </summary>
    public sealed class OpenNLPTokenizer : SegmentingTokenizerBase
    {
        public static int EOS_FLAG_BIT = 1;

        private readonly ICharTermAttribute termAtt;
        private readonly IFlagsAttribute flagsAtt;
        private readonly IOffsetAttribute offsetAtt;

        private Span[] termSpans = null;
        private int termNum = 0;
        private int sentenceStart = 0;

        //private readonly NLPSentenceDetectorOp sentenceOp = null; // LUCENENET: Never read
        private readonly NLPTokenizerOp tokenizerOp = null;

        /// <summary>
        /// Creates a new <see cref="OpenNLPTokenizer"/> </summary>
        public OpenNLPTokenizer(TextReader reader, NLPSentenceDetectorOp sentenceOp, NLPTokenizerOp tokenizerOp) // LUCENENET 4.8.0 specific overload to default AttributeFactory
            : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, reader, sentenceOp, tokenizerOp)
        {
        }

        public OpenNLPTokenizer(AttributeFactory factory, TextReader reader, NLPSentenceDetectorOp sentenceOp, NLPTokenizerOp tokenizerOp) // LUCENENET: Added reader param for compatibility with 4.8 - remove when upgrading
            : base(factory, reader, new OpenNLPSentenceBreakIterator(sentenceOp))
        {
            // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention) and refactored to throw on each one separately
            if (sentenceOp is null)
                throw new ArgumentNullException(nameof(sentenceOp), "OpenNLPTokenizer: both a Sentence Detector and a Tokenizer are required");
            if (tokenizerOp is null)
                throw new ArgumentNullException(nameof(tokenizerOp), "OpenNLPTokenizer: both a Sentence Detector and a Tokenizer are required");
            //this.sentenceOp = sentenceOp; // LUCENENET: Never read
            this.tokenizerOp = tokenizerOp;
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.flagsAtt = AddAttribute<IFlagsAttribute>();
            this.offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                termSpans = null;
                termNum = sentenceStart = 0;
            }
        }

        protected override void SetNextSentence(int sentenceStart, int sentenceEnd)
        {
            this.sentenceStart = sentenceStart;
            string sentenceText = new string(m_buffer, sentenceStart, sentenceEnd - sentenceStart);
            termSpans = tokenizerOp.GetTerms(sentenceText);
            termNum = 0;
        }

        protected override bool IncrementWord()
        {
            if (termSpans is null || termNum == termSpans.Length)
            {
                return false;
            }
            ClearAttributes();
            Span term = termSpans[termNum];
            termAtt.CopyBuffer(m_buffer, sentenceStart + term.getStart(), term.length());
            offsetAtt.SetOffset(CorrectOffset(m_offset + sentenceStart + term.getStart()),
                                CorrectOffset(m_offset + sentenceStart + term.getEnd()));
            if (termNum == termSpans.Length - 1)
            {
                flagsAtt.Flags = flagsAtt.Flags | EOS_FLAG_BIT; // mark the last token in the sentence with EOS_FLAG_BIT
            }
            ++termNum;
            return true;
        }

        public override void Reset()
        {
            base.Reset();
            termSpans = null;
            termNum = sentenceStart = 0;
        }
    }
}
