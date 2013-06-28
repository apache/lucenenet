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
using System.Collections.Generic;
using Document = Lucene.Net.Documents.Document;
using MergeAbortedException = Lucene.Net.Index.MergePolicy.MergeAbortedException;
using Directory = Lucene.Net.Store.Directory;
using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;
using DocValuesType = Lucene.Net.Index.FieldInfo.DocValuesType;
using Lucene.Net.Util;
using Lucene.Net.Store;
using Lucene.Net.Codecs;

namespace Lucene.Net.Index
{

    /// <summary> The SegmentMerger class combines two or more Segments, represented by an IndexReader (<see cref="Add" />,
    /// into a single Segment.  After adding the appropriate readers, call the merge method to combine the 
    /// segments.
    /// <p/> 
    /// If the compoundFile flag is set, then the segments will be merged into a compound file.
    /// 
    /// 
    /// </summary>
    /// <seealso cref="Merge()">
    /// </seealso>
    /// <seealso cref="Add">
    /// </seealso>
    public sealed class SegmentMerger
    {
        private readonly Directory directory;
        private readonly int termIndexInterval;

        private readonly Codec codec;

        private readonly IOContext context;

        private readonly MergeState mergeState;
        private readonly FieldInfos.Builder fieldInfosBuilder;

        public SegmentMerger(IList<AtomicReader> readers, SegmentInfo segmentInfo, InfoStream infoStream, Directory dir, int termIndexInterval,
                MergeState.CheckAbort checkAbort, FieldInfos.FieldNumbers fieldNumbers, IOContext context)
        {
            mergeState = new MergeState(readers, segmentInfo, infoStream, checkAbort);
            directory = dir;
            this.termIndexInterval = termIndexInterval;
            this.codec = segmentInfo.Codec;
            this.context = context;
            this.fieldInfosBuilder = new FieldInfos.Builder(fieldNumbers);
        }

        /// <summary> Merges the readers specified by the <see cref="Add" /> method into the directory passed to the constructor</summary>
        /// <returns> The number of documents that were merged
        /// </returns>
        /// <throws>  CorruptIndexException if the index is corrupt </throws>
        /// <throws>  IOException if there is a low-level IO error </throws>
        public MergeState Merge()
        {
            // NOTE: it's important to add calls to
            // checkAbort.work(...) if you make any changes to this
            // method that will spend alot of time.  The frequency
            // of this check impacts how long
            // IndexWriter.close(false) takes to actually stop the
            // threads.

            mergeState.segmentInfo.DocCount = SetDocMaps();
            MergeFieldInfos();
            SetMatchingSegmentReaders();
            long t0 = 0;
            if (mergeState.infoStream.IsEnabled("SM"))
            {
                t0 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            }
            int numMerged = MergeFields();
            if (mergeState.infoStream.IsEnabled("SM"))
            {
                long t1 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                mergeState.infoStream.Message("SM", (t1 - t0) + " msec to merge stored fields [" + numMerged + " docs]");
            }
            //assert numMerged == mergeState.segmentInfo.getDocCount();

            SegmentWriteState segmentWriteState = new SegmentWriteState(mergeState.infoStream, directory, mergeState.segmentInfo,
                                                                              mergeState.fieldInfos, termIndexInterval, null, context);
            if (mergeState.infoStream.IsEnabled("SM"))
            {
                t0 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            }
            MergeTerms(segmentWriteState);
            if (mergeState.infoStream.IsEnabled("SM"))
            {
                long t1 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                mergeState.infoStream.Message("SM", (t1 - t0) + " msec to merge postings [" + numMerged + " docs]");
            }

            if (mergeState.infoStream.IsEnabled("SM"))
            {
                t0 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            }
            if (mergeState.fieldInfos.HasDocValues)
            {
                MergeDocValues(segmentWriteState);
            }
            if (mergeState.infoStream.IsEnabled("SM"))
            {
                long t1 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                mergeState.infoStream.Message("SM", (t1 - t0) + " msec to merge doc values [" + numMerged + " docs]");
            }

            if (mergeState.fieldInfos.HasNorms)
            {
                if (mergeState.infoStream.IsEnabled("SM"))
                {
                    t0 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                }
                MergeNorms(segmentWriteState);
                if (mergeState.infoStream.IsEnabled("SM"))
                {
                    long t1 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                    mergeState.infoStream.Message("SM", (t1 - t0) + " msec to merge norms [" + numMerged + " docs]");
                }
            }

            if (mergeState.fieldInfos.HasVectors)
            {
                if (mergeState.infoStream.IsEnabled("SM"))
                {
                    t0 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                }
                numMerged = MergeVectors();
                if (mergeState.infoStream.IsEnabled("SM"))
                {
                    long t1 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                    mergeState.infoStream.Message("SM", (t1 - t0) + " msec to merge vectors [" + numMerged + " docs]");
                }
                //assert numMerged == mergeState.segmentInfo.getDocCount();
            }

            // write the merged infos
            FieldInfosWriter fieldInfosWriter = codec.FieldInfosFormat.FieldInfosWriter;
            fieldInfosWriter.Write(directory, mergeState.segmentInfo.name, mergeState.fieldInfos, context);

            return mergeState;
        }

        private void MergeDocValues(SegmentWriteState segmentWriteState)
        {
            DocValuesConsumer consumer = codec.DocValuesFormat.FieldsConsumer(segmentWriteState);
            bool success = false;
            try
            {
                foreach (FieldInfo field in mergeState.fieldInfos)
                {
                    DocValuesType? type = field.DocValuesTypeValue;
                    if (type != null)
                    {
                        if (type == DocValuesType.NUMERIC)
                        {
                            List<NumericDocValues> toMerge = new List<NumericDocValues>();
                            foreach (AtomicReader reader in mergeState.readers)
                            {
                                NumericDocValues values = reader.GetNumericDocValues(field.name);
                                if (values == null)
                                {
                                    values = NumericDocValues.EMPTY;
                                }
                                toMerge.Add(values);
                            }
                            consumer.MergeNumericField(field, mergeState, toMerge);
                        }
                        else if (type == DocValuesType.BINARY)
                        {
                            List<BinaryDocValues> toMerge = new List<BinaryDocValues>();
                            foreach (AtomicReader reader in mergeState.readers)
                            {
                                BinaryDocValues values = reader.GetBinaryDocValues(field.name);
                                if (values == null)
                                {
                                    values = BinaryDocValues.EMPTY;
                                }
                                toMerge.Add(values);
                            }
                            consumer.MergeBinaryField(field, mergeState, toMerge);
                        }
                        else if (type == DocValuesType.SORTED)
                        {
                            List<SortedDocValues> toMerge = new List<SortedDocValues>();
                            foreach (AtomicReader reader in mergeState.readers)
                            {
                                SortedDocValues values = reader.GetSortedDocValues(field.name);
                                if (values == null)
                                {
                                    values = SortedDocValues.EMPTY;
                                }
                                toMerge.Add(values);
                            }
                            consumer.MergeSortedField(field, mergeState, toMerge);
                        }
                        else if (type == DocValuesType.SORTED_SET)
                        {
                            List<SortedSetDocValues> toMerge = new List<SortedSetDocValues>();
                            foreach (AtomicReader reader in mergeState.readers)
                            {
                                SortedSetDocValues values = reader.GetSortedSetDocValues(field.name);
                                if (values == null)
                                {
                                    values = SortedSetDocValues.EMPTY;
                                }
                                toMerge.Add(values);
                            }
                            consumer.MergeSortedSetField(field, mergeState, toMerge);
                        }
                        else
                        {
                            throw new Exception("type=" + type);
                        }
                    }
                }
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(consumer);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)consumer);
                }
            }
        }

        private void MergeNorms(SegmentWriteState segmentWriteState)
        {
            DocValuesConsumer consumer = codec.NormsFormat.NormsConsumer(segmentWriteState);
            bool success = false;
            try
            {
                foreach (FieldInfo field in mergeState.fieldInfos)
                {
                    if (field.HasNorms)
                    {
                        List<NumericDocValues> toMerge = new List<NumericDocValues>();
                        foreach (AtomicReader reader in mergeState.readers)
                        {
                            NumericDocValues norms = reader.GetNormValues(field.name);
                            if (norms == null)
                            {
                                norms = NumericDocValues.EMPTY;
                            }
                            toMerge.Add(norms);
                        }
                        consumer.MergeNumericField(field, mergeState, toMerge);
                    }
                }
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(consumer);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)consumer);
                }
            }
        }

        private void SetMatchingSegmentReaders()
        {
            // If the i'th reader is a SegmentReader and has
            // identical fieldName -> number mapping, then this
            // array will be non-null at position i:
            int numReaders = mergeState.readers.Count;
            mergeState.matchingSegmentReaders = new SegmentReader[numReaders];

            // If this reader is a SegmentReader, and all of its
            // field name -> number mappings match the "merged"
            // FieldInfos, then we can do a bulk copy of the
            // stored fields:
            for (int i = 0; i < numReaders; i++)
            {
                AtomicReader reader = mergeState.readers[i];
                // TODO: we may be able to broaden this to
                // non-SegmentReaders, since FieldInfos is now
                // required?  But... this'd also require exposing
                // bulk-copy (TVs and stored fields) API in foreign
                // readers..
                if (reader is SegmentReader)
                {
                    SegmentReader segmentReader = (SegmentReader)reader;
                    bool same = true;
                    FieldInfos segmentFieldInfos = segmentReader.FieldInfos;
                    foreach (FieldInfo fi in segmentFieldInfos)
                    {
                        FieldInfo other = mergeState.fieldInfos.FieldInfo(fi.number);
                        if (other == null || !other.name.Equals(fi.name))
                        {
                            same = false;
                            break;
                        }
                    }
                    if (same)
                    {
                        mergeState.matchingSegmentReaders[i] = segmentReader;
                        mergeState.matchedCount++;
                    }
                }
            }

            if (mergeState.infoStream.IsEnabled("SM"))
            {
                mergeState.infoStream.Message("SM", "merge store matchedCount=" + mergeState.matchedCount + " vs " + mergeState.readers.Count);
                if (mergeState.matchedCount != mergeState.readers.Count)
                {
                    mergeState.infoStream.Message("SM", "" + (mergeState.readers.Count - mergeState.matchedCount) + " non-bulk merges");
                }
            }
        }

        public void MergeFieldInfos()
        {
            foreach (AtomicReader reader in mergeState.readers)
            {
                FieldInfos readerFieldInfos = reader.FieldInfos;
                foreach (FieldInfo fi in readerFieldInfos)
                {
                    fieldInfosBuilder.Add(fi);
                }
            }
            mergeState.fieldInfos = fieldInfosBuilder.Finish();
        }

        private int MergeFields()
        {
            StoredFieldsWriter fieldsWriter = codec.StoredFieldsFormat.FieldsWriter(directory, mergeState.segmentInfo, context);

            try
            {
                return fieldsWriter.Merge(mergeState);
            }
            finally
            {
                fieldsWriter.Dispose();
            }
        }

        private int MergeVectors()
        {
            TermVectorsWriter termVectorsWriter = codec.TermVectorsFormat.VectorsWriter(directory, mergeState.segmentInfo, context);

            try
            {
                return termVectorsWriter.Merge(mergeState);
            }
            finally
            {
                termVectorsWriter.Dispose();
            }
        }

        private int SetDocMaps()
        {
            int numReaders = mergeState.readers.Count;

            // Remap docIDs
            mergeState.docMaps = new MergeState.DocMap[numReaders];
            mergeState.docBase = new int[numReaders];

            int docBase = 0;

            int i = 0;
            while (i < mergeState.readers.Count)
            {

                AtomicReader reader = mergeState.readers[i];

                mergeState.docBase[i] = docBase;
                MergeState.DocMap docMap = MergeState.DocMap.Build(reader);
                mergeState.docMaps[i] = docMap;
                docBase += docMap.NumDocs;

                i++;
            }

            return docBase;
        }

        private void MergeTerms(SegmentWriteState segmentWriteState)
        {

            List<Fields> fields = new List<Fields>();
            List<ReaderSlice> slices = new List<ReaderSlice>();

            int docBase = 0;

            for (int readerIndex = 0; readerIndex < mergeState.readers.Count; readerIndex++)
            {
                AtomicReader reader = mergeState.readers[readerIndex];
                Fields f = reader.Fields;
                int maxDoc = reader.MaxDoc;
                if (f != null)
                {
                    slices.Add(new ReaderSlice(docBase, maxDoc, readerIndex));
                    fields.Add(f);
                }
                docBase += maxDoc;
            }

            FieldsConsumer consumer = codec.PostingsFormat.FieldsConsumer(segmentWriteState);
            bool success = false;
            try
            {
                consumer.Merge(mergeState, new MultiFields(fields.ToArray(), slices.ToArray()));
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(consumer);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)consumer);
                }
            }
        }
    }
}