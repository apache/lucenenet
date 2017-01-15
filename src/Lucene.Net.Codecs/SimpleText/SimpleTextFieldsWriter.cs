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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Lucene.Net.Codecs.SimpleText
{
    using IndexOptions = Index.IndexOptions;
    using FieldInfo = Index.FieldInfo;
    using SegmentWriteState = Index.SegmentWriteState;
    using IndexOutput = Store.IndexOutput;
    using BytesRef = Util.BytesRef;

    internal class SimpleTextFieldsWriter : FieldsConsumer
    {

        private IndexOutput _output;
        private readonly BytesRef _scratch = new BytesRef(10);

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
            var fileName = SimpleTextPostingsFormat.GetPostingsFileName(state.SegmentInfo.Name, state.SegmentSuffix);
            _output = state.Directory.CreateOutput(fileName, state.Context);
        }

        private void Write(string s)
        {
            SimpleTextUtil.Write(_output, s, _scratch);
        }

        private void Write(BytesRef b)
        {
            SimpleTextUtil.Write(_output, b);
        }

        private void Newline()
        {
            SimpleTextUtil.WriteNewline(_output);
        }

        public override TermsConsumer AddField(FieldInfo field)
        {
            Write(FIELD);
            Write(field.Name);
            Newline();
            return new SimpleTextTermsWriter(this, field);
        }

        public override void Dispose()
        {
            if (_output == null) return;

            try
            {
                Write(END);
                Newline();
                SimpleTextUtil.WriteChecksum(_output, _scratch);
            }
            finally
            {
                _output.Dispose();
                _output = null;
            }
        }

        private class SimpleTextTermsWriter : TermsConsumer
        {
            private readonly SimpleTextFieldsWriter _outerInstance;
            private readonly SimpleTextPostingsWriter _postingsWriter;

            public SimpleTextTermsWriter(SimpleTextFieldsWriter outerInstance, FieldInfo field)
            {
                _outerInstance = outerInstance;
                _postingsWriter = new SimpleTextPostingsWriter(outerInstance, field);
            }

            public override PostingsConsumer StartTerm(BytesRef term)
            {
                return _postingsWriter.Reset(term);
            }

            public override void FinishTerm(BytesRef term, TermStats stats)
            {
            }

            public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
            {
            }

            public override IComparer<BytesRef> Comparer
            {
                get { return BytesRef.UTF8SortedAsUnicodeComparer; }
            }
        }

        private sealed class SimpleTextPostingsWriter : PostingsConsumer
        {
            private readonly SimpleTextFieldsWriter _outerInstance;

            private BytesRef _term;
            private bool _wroteTerm;
            private readonly IndexOptions _indexOptions;
            private readonly bool _writePositions;
            private readonly bool _writeOffsets;

            // for assert:
            private int _lastStartOffset;

            public SimpleTextPostingsWriter(SimpleTextFieldsWriter outerInstance, FieldInfo field)
            {
                _outerInstance = outerInstance;
                _indexOptions = field.IndexOptions.Value;
                _writePositions = _indexOptions.CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
                _writeOffsets = _indexOptions.CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
            }

            public override void StartDoc(int docId, int termDocFreq)
            {
                if (!_wroteTerm)
                {
                    // we lazily do this, in case the term had zero docs
                    _outerInstance.Write(TERM);
                    _outerInstance.Write(_term);
                    _outerInstance.Newline();
                    _wroteTerm = true;
                }

                _outerInstance.Write(DOC);
                _outerInstance.Write(Convert.ToString(docId, CultureInfo.InvariantCulture));
                _outerInstance.Newline();
                if (_indexOptions != IndexOptions.DOCS_ONLY)
                {
                    _outerInstance.Write(FREQ);
                    _outerInstance.Write(Convert.ToString(termDocFreq, CultureInfo.InvariantCulture));
                    _outerInstance.Newline();
                }

                _lastStartOffset = 0;
            }

            public PostingsConsumer Reset(BytesRef term)
            {
                _term = term;
                _wroteTerm = false;
                return this;
            }

            public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
            {
                if (_writePositions)
                {
                    _outerInstance.Write(POS);
                    _outerInstance.Write(Convert.ToString(position, CultureInfo.InvariantCulture));
                    _outerInstance.Newline();
                }

                if (_writeOffsets)
                {
                    Debug.Assert(endOffset >= startOffset);
                    Debug.Assert(startOffset >= _lastStartOffset,
                        "startOffset=" + startOffset + " lastStartOffset=" + _lastStartOffset);
                    _lastStartOffset = startOffset;
                    _outerInstance.Write(START_OFFSET);
                    _outerInstance.Write(Convert.ToString(startOffset, CultureInfo.InvariantCulture));
                    _outerInstance.Newline();
                    _outerInstance.Write(END_OFFSET);
                    _outerInstance.Write(Convert.ToString(endOffset, CultureInfo.InvariantCulture));
                    _outerInstance.Newline();
                }

                if (payload != null && payload.Length > 0)
                {
                    Debug.Assert(payload.Length != 0);
                    _outerInstance.Write(PAYLOAD);
                    _outerInstance.Write(payload);
                    _outerInstance.Newline();
                }
            }

            public override void FinishDoc()
            {
            }
        }

    }

}