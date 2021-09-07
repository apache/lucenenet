using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Index
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

    using IOUtils = Lucene.Net.Util.IOUtils;
    using TokenStream = Lucene.Net.Analysis.TokenStream;

    /// <summary>
    /// Holds state for inverting all occurrences of a single
    /// field in the document.  This class doesn't do anything
    /// itself; instead, it forwards the tokens produced by
    /// analysis to its own consumer
    /// (<see cref="InvertedDocConsumerPerField"/>).  It also interacts with an
    /// endConsumer (<see cref="InvertedDocEndConsumerPerField"/>).
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
            fieldState = new FieldInvertState(fieldInfo.Name);
            this.consumer = parent.consumer.AddField(this, fieldInfo);
            this.endConsumer = parent.endConsumer.AddField(this, fieldInfo);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal override void Abort()
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
                IIndexableFieldType fieldType = field.IndexableFieldType;

                // TODO FI: this should be "genericized" to querying
                // consumer if it wants to see this particular field
                // tokenized.
                if (fieldType.IsIndexed && doInvert)
                {
                    bool analyzed = fieldType.IsTokenized && docState.analyzer != null;

                    // if the field omits norms, the boost cannot be indexed.
                    if (fieldType.OmitNorms && field.Boost != 1.0f)
                    {
                        throw UnsupportedOperationException.Create("You cannot set an index-time boost: norms are omitted for field '" + field.Name + "'");
                    }

                    // only bother checking offsets if something will consume them.
                    // TODO: after we fix analyzers, also check if termVectorOffsets will be indexed.
                    bool checkOffsets = fieldType.IndexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                    int lastStartOffset = 0;

                    if (i > 0)
                    {
                        fieldState.Position += analyzed ? docState.analyzer.GetPositionIncrementGap(fieldInfo.Name) : 0;
                    }

                    /*
                   * To assist people in tracking down problems in analysis components, we wish to write the field name to the infostream
                   * when we fail. We expect some caller to eventually deal with the real exception, so we don't want any 'catch' clauses,
                   * but rather a finally that takes note of the problem.
                   */

                    bool succeededInProcessingField = false;

                    TokenStream stream = field.GetTokenStream(docState.analyzer);
                    // reset the TokenStream to the first token
                    stream.Reset();

                    try
                    {
                        bool hasMoreTokens = stream.IncrementToken();

                        fieldState.AttributeSource = stream;

                        IOffsetAttribute offsetAttribute = fieldState.AttributeSource.AddAttribute<IOffsetAttribute>();
                        IPositionIncrementAttribute posIncrAttribute = fieldState.AttributeSource.AddAttribute<IPositionIncrementAttribute>();

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
                                    throw new ArgumentException("position increment must be >=0 (got " + posIncr + ") for field '" + field.Name + "'");
                                }
                                if (fieldState.Position == 0 && posIncr == 0)
                                {
                                    throw new ArgumentException("first position increment must be > 0 (got 0) for field '" + field.Name + "'");
                                }
                                int position = fieldState.Position + posIncr;
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
                                fieldState.Position = position;

                                if (posIncr == 0)
                                {
                                    fieldState.NumOverlap++;
                                }

                                if (checkOffsets)
                                {
                                    int startOffset = fieldState.Offset + offsetAttribute.StartOffset;
                                    int endOffset = fieldState.Offset + offsetAttribute.EndOffset;
                                    if (startOffset < 0 || endOffset < startOffset)
                                    {
                                        throw new ArgumentException("startOffset must be non-negative, and endOffset must be >= startOffset, " + "startOffset=" + startOffset + ",endOffset=" + endOffset + " for field '" + field.Name + "'");
                                    }
                                    if (startOffset < lastStartOffset)
                                    {
                                        throw new ArgumentException("offsets must not go backwards startOffset=" + startOffset + " is < lastStartOffset=" + lastStartOffset + " for field '" + field.Name + "'");
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
                                fieldState.Length++;
                                fieldState.Position++;
                            } while (stream.IncrementToken());
                        }
                        // trigger streams to perform end-of-stream operations
                        stream.End();
                        // TODO: maybe add some safety? then again, its already checked
                        // when we come back around to the field...
                        fieldState.Position += posIncrAttribute.PositionIncrement;
                        fieldState.Offset += offsetAttribute.EndOffset;

                        if (docState.maxTermPrefix != null)
                        {
                            string msg = "Document contains at least one immense term in field=\"" + fieldInfo.Name + "\" (whose UTF8 encoding is longer than the max length " + DocumentsWriterPerThread.MAX_TERM_LENGTH_UTF8 + "), all of which were skipped.  Please correct the analyzer to not produce such terms.  The prefix of the first immense term is: '" + docState.maxTermPrefix + "...'";
                            if (docState.infoStream.IsEnabled("IW"))
                            {
                                docState.infoStream.Message("IW", "ERROR: " + msg);
                            }
                            docState.maxTermPrefix = null;
                            throw new ArgumentException(msg);
                        }

                        /* if success was false above there is an exception coming through and we won't get here.*/
                        succeededInProcessingField = true;
                    }
                    finally
                    {
                        if (!succeededInProcessingField)
                        {
                            IOUtils.DisposeWhileHandlingException(stream);
                        }
                        else
                        {
                            stream.Dispose();
                        }
                        if (!succeededInProcessingField && docState.infoStream.IsEnabled("DW"))
                        {
                            docState.infoStream.Message("DW", "An exception was thrown while processing field " + fieldInfo.Name);
                        }
                    }

                    fieldState.Offset += analyzed ? docState.analyzer.GetOffsetGap(fieldInfo.Name) : 0;
                    fieldState.Boost *= field.Boost;
                }

                // LUCENE-2387: don't hang onto the field, so GC can
                // reclaim
                fields[i] = null;
            }

            consumer.Finish();
            endConsumer.Finish();
        }

        internal override FieldInfo FieldInfo => fieldInfo;
    }
}