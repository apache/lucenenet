using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lucene.Net.Codecs.SimpleText
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

    using BytesRef = Util.BytesRef;
    using FieldInfo = Index.FieldInfo;
    using IndexOptions = Index.IndexOptions;
    using IndexOutput = Store.IndexOutput;
    using SegmentWriteState = Index.SegmentWriteState;

    internal class SimpleTextFieldsWriter : FieldsConsumer
    {
        private IndexOutput output;
        private readonly BytesRef scratch = new BytesRef(10);

        internal static readonly BytesRef END = new BytesRef("END");
        internal static readonly BytesRef FIELD = new BytesRef("field ");
        internal static readonly BytesRef TERM = new BytesRef("  term ");
        internal static readonly BytesRef DOC = new BytesRef("    doc ");
        internal static readonly BytesRef FREQ = new BytesRef("      freq ");
        internal static readonly BytesRef POS = new BytesRef("      pos ");
        internal static readonly BytesRef START_OFFSET = new BytesRef("      startOffset ");
        internal static readonly BytesRef END_OFFSET = new BytesRef("      endOffset ");
        internal static readonly BytesRef PAYLOAD = new BytesRef("        payload ");

        public SimpleTextFieldsWriter(SegmentWriteState state)
        {
            string fileName = SimpleTextPostingsFormat.GetPostingsFileName(state.SegmentInfo.Name, state.SegmentSuffix);
            output = state.Directory.CreateOutput(fileName, state.Context);
        }

        private void Write(string s)
        {
            SimpleTextUtil.Write(output, s, scratch);
        }

        private void Write(BytesRef b)
        {
            SimpleTextUtil.Write(output, b);
        }

        private void Newline()
        {
            SimpleTextUtil.WriteNewline(output);
        }

        public override TermsConsumer AddField(FieldInfo field)
        {
            Write(FIELD);
            Write(field.Name);
            Newline();
            return new SimpleTextTermsWriter(this, field);
        }

        private class SimpleTextTermsWriter : TermsConsumer
        {
            private readonly SimpleTextPostingsWriter postingsWriter;

            public SimpleTextTermsWriter(SimpleTextFieldsWriter outerInstance, FieldInfo field)
            {
                postingsWriter = new SimpleTextPostingsWriter(outerInstance, field);
            }

            public override PostingsConsumer StartTerm(BytesRef term)
            {
                return postingsWriter.Reset(term);
            }

            public override void FinishTerm(BytesRef term, TermStats stats)
            {
            }

            public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
            {
            }

            public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;
        }

        private class SimpleTextPostingsWriter : PostingsConsumer
        {
            private readonly SimpleTextFieldsWriter outerInstance;

            private BytesRef term;
            private bool wroteTerm;
            private readonly IndexOptions indexOptions;
            private readonly bool writePositions;
            private readonly bool writeOffsets;

            // for assert:
            private int lastStartOffset;

            public SimpleTextPostingsWriter(SimpleTextFieldsWriter outerInstance, FieldInfo field)
            {
                this.outerInstance = outerInstance;
                indexOptions = field.IndexOptions;
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                writePositions = IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
                writeOffsets = IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
                //System.out.println("writeOffsets=" + writeOffsets);
                //System.out.println("writePos=" + writePositions);
            }

            public override void StartDoc(int docId, int termDocFreq)
            {
                if (!wroteTerm)
                {
                    // we lazily do this, in case the term had zero docs
                    outerInstance.Write(TERM);
                    outerInstance.Write(term);
                    outerInstance.Newline();
                    wroteTerm = true;
                }

                outerInstance.Write(DOC);
                outerInstance.Write(Convert.ToString(docId, CultureInfo.InvariantCulture));
                outerInstance.Newline();
                if (indexOptions != IndexOptions.DOCS_ONLY)
                {
                    outerInstance.Write(FREQ);
                    outerInstance.Write(Convert.ToString(termDocFreq, CultureInfo.InvariantCulture));
                    outerInstance.Newline();
                }

                lastStartOffset = 0;
            }

            public virtual PostingsConsumer Reset(BytesRef term)
            {
                this.term = term;
                wroteTerm = false;
                return this;
            }

            public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
            {
                if (writePositions)
                {
                    outerInstance.Write(POS);
                    outerInstance.Write(Convert.ToString(position, CultureInfo.InvariantCulture));
                    outerInstance.Newline();
                }

                if (writeOffsets)
                {
                    if (Debugging.AssertsEnabled)
                    {
                        Debugging.Assert(endOffset >= startOffset);
                        Debugging.Assert(startOffset >= lastStartOffset,
                            "startOffset={0} lastStartOffset={1}", startOffset, lastStartOffset);
                    }
                    lastStartOffset = startOffset;
                    outerInstance.Write(START_OFFSET);
                    outerInstance.Write(Convert.ToString(startOffset, CultureInfo.InvariantCulture));
                    outerInstance.Newline();
                    outerInstance.Write(END_OFFSET);
                    outerInstance.Write(Convert.ToString(endOffset, CultureInfo.InvariantCulture));
                    outerInstance.Newline();
                }

                if (payload != null && payload.Length > 0)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(payload.Length != 0);
                    outerInstance.Write(PAYLOAD);
                    outerInstance.Write(payload);
                    outerInstance.Newline();
                }
            }

            public override void FinishDoc()
            {
            }
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (output is null) return;

                try
                {
                    Write(END);
                    Newline();
                    SimpleTextUtil.WriteChecksum(output, scratch);
                }
                finally
                {
                    output.Dispose();
                    output = null;
                }
            }
        }
    }
}