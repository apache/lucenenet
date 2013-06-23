/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using TokenStream = Lucene.Net.Analysis.TokenStream;
using Lucene.Net.Util;

namespace Lucene.Net.Index
{

    /// <summary> Holds state for inverting all occurrences of a single
    /// field in the document.  This class doesn't do anything
    /// itself; instead, it forwards the tokens produced by
    /// analysis to its own consumer
    /// (InvertedDocConsumerPerField).  It also interacts with an
    /// endConsumer (InvertedDocEndConsumerPerField).
    /// </summary>

    internal sealed class DocInverterPerField : DocFieldConsumerPerField
    {
        internal readonly FieldInfo fieldInfo;
        internal readonly InvertedDocConsumerPerField consumer;
        internal readonly InvertedDocEndConsumerPerField endConsumer;
        internal readonly DocumentsWriterPerThread.DocState docState;
        internal readonly FieldInvertState fieldState;

        public DocInverterPerField(DocInverter parent, FieldInfo fieldInfo)
        {
            this.fieldInfo = fieldInfo;
            docState = parent.docState;
            fieldState = new FieldInvertState(fieldInfo.name);
            this.consumer = parent.consumer.AddField(this, fieldInfo);
            this.endConsumer = parent.endConsumer.AddField(this, fieldInfo);
        }

        public override void Abort()
        {
            try
            {
                consumer.Abort();
            }
            finally
            {
                endConsumer.Abort();
            }
        }

        public override void ProcessFields(IIndexableField[] fields, int count)
        {
            fieldState.Reset();

            bool doInvert = consumer.Start(fields, count);

            for (int i = 0; i < count; i++)
            {

                IIndexableField field = fields[i];
                IIndexableFieldType fieldType = field.FieldType;

                // TODO FI: this should be "genericized" to querying
                // consumer if it wants to see this particular field
                // tokenized.
                if (fieldType.Indexed && doInvert)
                {
                    bool analyzed = fieldType.Tokenized && docState.analyzer != null;

                    // if the field omits norms, the boost cannot be indexed.
                    if (fieldType.OmitNorms && field.Boost != 1.0f)
                    {
                        throw new NotSupportedException("You cannot set an index-time boost: norms are omitted for field '" + field.Name + "'");
                    }

                    // only bother checking offsets if something will consume them.
                    // TODO: after we fix analyzers, also check if termVectorOffsets will be indexed.
                    bool checkOffsets = fieldType.IndexOptions == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                    int lastStartOffset = 0;

                    if (i > 0)
                    {
                        fieldState.position += analyzed ? docState.analyzer.GetPositionIncrementGap(fieldInfo.name) : 0;
                    }

                    TokenStream stream = field.TokenStream(docState.analyzer);
                    // reset the TokenStream to the first token
                    stream.Reset();

                    bool success2 = false;

                    try
                    {
                        bool hasMoreTokens = stream.IncrementToken();

                        fieldState.attributeSource = stream;

                        IOffsetAttribute offsetAttribute = fieldState.attributeSource.AddAttribute<IOffsetAttribute>();
                        IPositionIncrementAttribute posIncrAttribute = fieldState.attributeSource.AddAttribute<IPositionIncrementAttribute>();

                        if (hasMoreTokens)
                        {
                            consumer.Start(field);

                            do
                            {
                                // If we hit an exception in stream.next below
                                // (which is fairly common, eg if analyzer
                                // chokes on a given document), then it's
                                // non-aborting and (above) this one document
                                // will be marked as deleted, but still
                                // consume a docID

                                int posIncr = posIncrAttribute.PositionIncrement;
                                if (posIncr < 0)
                                {
                                    throw new ArgumentException("position increment must be >=0 (got " + posIncr + ")");
                                }
                                if (fieldState.position == 0 && posIncr == 0)
                                {
                                    throw new ArgumentException("first position increment must be > 0 (got 0)");
                                }
                                int position = fieldState.position + posIncr;
                                if (position > 0)
                                {
                                    // NOTE: confusing: this "mirrors" the
                                    // position++ we do below
                                    position--;
                                }
                                else if (position < 0)
                                {
                                    throw new ArgumentException("position overflow for field '" + field.Name + "'");
                                }

                                // position is legal, we can safely place it in fieldState now.
                                // not sure if anything will use fieldState after non-aborting exc...
                                fieldState.position = position;

                                if (posIncr == 0)
                                    fieldState.numOverlap++;

                                if (checkOffsets)
                                {
                                    int startOffset = fieldState.offset + offsetAttribute.StartOffset;
                                    int endOffset = fieldState.offset + offsetAttribute.EndOffset;
                                    if (startOffset < 0 || endOffset < startOffset)
                                    {
                                        throw new ArgumentException("startOffset must be non-negative, and endOffset must be >= startOffset, "
                                            + "startOffset=" + startOffset + ",endOffset=" + endOffset);
                                    }
                                    if (startOffset < lastStartOffset)
                                    {
                                        throw new ArgumentException("offsets must not go backwards startOffset="
                                             + startOffset + " is < lastStartOffset=" + lastStartOffset);
                                    }
                                    lastStartOffset = startOffset;
                                }

                                bool success = false;
                                try
                                {
                                    // If we hit an exception in here, we abort
                                    // all buffered documents since the last
                                    // flush, on the likelihood that the
                                    // internal state of the consumer is now
                                    // corrupt and should not be flushed to a
                                    // new segment:
                                    consumer.Add();
                                    success = true;
                                }
                                finally
                                {
                                    if (!success)
                                    {
                                        docState.docWriter.SetAborting();
                                    }
                                }
                                fieldState.length++;
                                fieldState.position++;
                            } while (stream.IncrementToken());
                        }
                        // trigger streams to perform end-of-stream operations
                        stream.End();

                        fieldState.offset += offsetAttribute.EndOffset;
                        success2 = true;
                    }
                    finally
                    {
                        if (!success2)
                        {
                            IOUtils.CloseWhileHandlingException((IDisposable)stream);
                        }
                        else
                        {
                            stream.Dispose();
                        }
                    }

                    fieldState.offset += analyzed ? docState.analyzer.GetOffsetGap(fieldInfo.name) : 0;
                    fieldState.boost *= field.Boost;
                }

                // LUCENE-2387: don't hang onto the field, so GC can
                // reclaim
                fields[i] = null;
            }

            consumer.Finish();
            endConsumer.Finish();
        }

        public override FieldInfo FieldInfo
        {
            get { return fieldInfo; }
        }
    }
}