using J2N;
using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;

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

    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using DocValuesConsumer = Lucene.Net.Codecs.DocValuesConsumer;
    using FieldInfosWriter = Lucene.Net.Codecs.FieldInfosWriter;
    using FieldsConsumer = Lucene.Net.Codecs.FieldsConsumer;
    using IBits = Lucene.Net.Util.IBits;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using StoredFieldsWriter = Lucene.Net.Codecs.StoredFieldsWriter;
    using TermVectorsWriter = Lucene.Net.Codecs.TermVectorsWriter;

    /// <summary>
    /// The <see cref="SegmentMerger"/> class combines two or more Segments, represented by an
    /// <see cref="IndexReader"/>, into a single Segment.  Call the merge method to combine the
    /// segments.
    /// </summary>
    /// <seealso cref="Merge()"/>
    internal sealed class SegmentMerger
    {
        private readonly Directory directory;
        private readonly int termIndexInterval;

        private readonly Codec codec;

        private readonly IOContext context;

        private readonly MergeState mergeState;
        private readonly FieldInfos.Builder fieldInfosBuilder;

        // note, just like in codec apis Directory 'dir' is NOT the same as segmentInfo.dir!!
        internal SegmentMerger(IList<AtomicReader> readers, SegmentInfo segmentInfo, InfoStream infoStream, Directory dir, int termIndexInterval, CheckAbort checkAbort, FieldInfos.FieldNumbers fieldNumbers, IOContext context, bool validate)
        {
            // validate incoming readers
            if (validate)
            {
                foreach (AtomicReader reader in readers)
                {
                    reader.CheckIntegrity();
                }
            }
            mergeState = new MergeState(readers, segmentInfo, infoStream, checkAbort);
            directory = dir;
            this.termIndexInterval = termIndexInterval;
            this.codec = segmentInfo.Codec;
            this.context = context;
            this.fieldInfosBuilder = new FieldInfos.Builder(fieldNumbers);
            mergeState.SegmentInfo.DocCount = SetDocMaps();
        }

        /// <summary>
        /// <c>True</c> if any merging should happen </summary>
        internal bool ShouldMerge => mergeState.SegmentInfo.DocCount > 0;

        /// <summary>
        /// Merges the readers into the directory passed to the constructor </summary>
        /// <returns> The number of documents that were merged </returns>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal MergeState Merge()
        {
            if (!ShouldMerge)
            {
                throw IllegalStateException.Create("Merge would result in 0 document segment");
            }
            // NOTE: it's important to add calls to
            // checkAbort.work(...) if you make any changes to this
            // method that will spend alot of time.  The frequency
            // of this check impacts how long
            // IndexWriter.close(false) takes to actually stop the
            // threads.
            MergeFieldInfos();
            SetMatchingSegmentReaders();
            long t0 = 0;
            if (mergeState.InfoStream.IsEnabled("SM"))
            {
                t0 = Time.NanoTime();
            }
            int numMerged = MergeFields();
            if (mergeState.InfoStream.IsEnabled("SM"))
            {
                long t1 = Time.NanoTime();
                mergeState.InfoStream.Message("SM", ((t1 - t0) / 1000000) + " msec to merge stored fields [" + numMerged + " docs]");
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(numMerged == mergeState.SegmentInfo.DocCount);

            SegmentWriteState segmentWriteState = new SegmentWriteState(mergeState.InfoStream, directory, mergeState.SegmentInfo, mergeState.FieldInfos, termIndexInterval, null, context);
            if (mergeState.InfoStream.IsEnabled("SM"))
            {
                t0 = Time.NanoTime();
            }
            MergeTerms(segmentWriteState);
            if (mergeState.InfoStream.IsEnabled("SM"))
            {
                long t1 = Time.NanoTime();
                mergeState.InfoStream.Message("SM", ((t1 - t0) / 1000000) + " msec to merge postings [" + numMerged + " docs]");
            }

            if (mergeState.InfoStream.IsEnabled("SM"))
            {
                t0 = Time.NanoTime();
            }
            if (mergeState.FieldInfos.HasDocValues)
            {
                MergeDocValues(segmentWriteState);
            }
            if (mergeState.InfoStream.IsEnabled("SM"))
            {
                long t1 = Time.NanoTime();
                mergeState.InfoStream.Message("SM", ((t1 - t0) / 1000000) + " msec to merge doc values [" + numMerged + " docs]");
            }

            if (mergeState.FieldInfos.HasNorms)
            {
                if (mergeState.InfoStream.IsEnabled("SM"))
                {
                    t0 = Time.NanoTime();
                }
                MergeNorms(segmentWriteState);
                if (mergeState.InfoStream.IsEnabled("SM"))
                {
                    long t1 = Time.NanoTime();
                    mergeState.InfoStream.Message("SM", ((t1 - t0) / 1000000) + " msec to merge norms [" + numMerged + " docs]");
                }
            }

            if (mergeState.FieldInfos.HasVectors)
            {
                if (mergeState.InfoStream.IsEnabled("SM"))
                {
                    t0 = Time.NanoTime();
                }
                numMerged = MergeVectors();
                if (mergeState.InfoStream.IsEnabled("SM"))
                {
                    long t1 = Time.NanoTime();
                    mergeState.InfoStream.Message("SM", ((t1 - t0) / 1000000) + " msec to merge vectors [" + numMerged + " docs]");
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(numMerged == mergeState.SegmentInfo.DocCount);
            }

            // write the merged infos
            FieldInfosWriter fieldInfosWriter = codec.FieldInfosFormat.FieldInfosWriter;
            fieldInfosWriter.Write(directory, mergeState.SegmentInfo.Name, "", mergeState.FieldInfos, context);

            return mergeState;
        }

        private void MergeDocValues(SegmentWriteState segmentWriteState)
        {
            DocValuesConsumer consumer = codec.DocValuesFormat.FieldsConsumer(segmentWriteState);
            bool success = false;
            try
            {
                foreach (FieldInfo field in mergeState.FieldInfos)
                {
                    DocValuesType type = field.DocValuesType;
                    if (type != DocValuesType.NONE)
                    {
                        if (type == DocValuesType.NUMERIC)
                        {
                            IList<NumericDocValues> toMerge = new JCG.List<NumericDocValues>();
                            IList<IBits> docsWithField = new JCG.List<IBits>();
                            foreach (AtomicReader reader in mergeState.Readers)
                            {
                                NumericDocValues values = reader.GetNumericDocValues(field.Name);
                                IBits bits = reader.GetDocsWithField(field.Name);
                                if (values is null)
                                {
                                    values = DocValues.EMPTY_NUMERIC;
                                    bits = new Lucene.Net.Util.Bits.MatchNoBits(reader.MaxDoc);
                                }
                                toMerge.Add(values);
                                docsWithField.Add(bits);
                            }
                            consumer.MergeNumericField(field, mergeState, toMerge, docsWithField);
                        }
                        else if (type == DocValuesType.BINARY)
                        {
                            IList<BinaryDocValues> toMerge = new JCG.List<BinaryDocValues>();
                            IList<IBits> docsWithField = new JCG.List<IBits>();
                            foreach (AtomicReader reader in mergeState.Readers)
                            {
                                BinaryDocValues values = reader.GetBinaryDocValues(field.Name);
                                IBits bits = reader.GetDocsWithField(field.Name);
                                if (values is null)
                                {
                                    values = DocValues.EMPTY_BINARY;
                                    bits = new Lucene.Net.Util.Bits.MatchNoBits(reader.MaxDoc);
                                }
                                toMerge.Add(values);
                                docsWithField.Add(bits);
                            }
                            consumer.MergeBinaryField(field, mergeState, toMerge, docsWithField);
                        }
                        else if (type == DocValuesType.SORTED)
                        {
                            IList<SortedDocValues> toMerge = new JCG.List<SortedDocValues>();
                            foreach (AtomicReader reader in mergeState.Readers)
                            {
                                SortedDocValues values = reader.GetSortedDocValues(field.Name);
                                if (values is null)
                                {
                                    values = DocValues.EMPTY_SORTED;
                                }
                                toMerge.Add(values);
                            }
                            consumer.MergeSortedField(field, mergeState, toMerge);
                        }
                        else if (type == DocValuesType.SORTED_SET)
                        {
                            IList<SortedSetDocValues> toMerge = new JCG.List<SortedSetDocValues>();
                            foreach (AtomicReader reader in mergeState.Readers)
                            {
                                SortedSetDocValues values = reader.GetSortedSetDocValues(field.Name);
                                if (values is null)
                                {
                                    values = DocValues.EMPTY_SORTED_SET;
                                }
                                toMerge.Add(values);
                            }
                            consumer.MergeSortedSetField(field, mergeState, toMerge);
                        }
                        else
                        {
                            throw AssertionError.Create("type=" + type);
                        }
                    }
                }
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(consumer);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(consumer);
                }
            }
        }

        private void MergeNorms(SegmentWriteState segmentWriteState)
        {
            DocValuesConsumer consumer = codec.NormsFormat.NormsConsumer(segmentWriteState);
            bool success = false;
            try
            {
                foreach (FieldInfo field in mergeState.FieldInfos)
                {
                    if (field.HasNorms)
                    {
                        IList<NumericDocValues> toMerge = new JCG.List<NumericDocValues>();
                        IList<IBits> docsWithField = new JCG.List<IBits>();
                        foreach (AtomicReader reader in mergeState.Readers)
                        {
                            NumericDocValues norms = reader.GetNormValues(field.Name);
                            if (norms is null)
                            {
                                norms = DocValues.EMPTY_NUMERIC;
                            }
                            toMerge.Add(norms);
                            docsWithField.Add(new Lucene.Net.Util.Bits.MatchAllBits(reader.MaxDoc));
                        }
                        consumer.MergeNumericField(field, mergeState, toMerge, docsWithField);
                    }
                }
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(consumer);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(consumer);
                }
            }
        }

        private void SetMatchingSegmentReaders()
        {
            // If the i'th reader is a SegmentReader and has
            // identical fieldName -> number mapping, then this
            // array will be non-null at position i:
            int numReaders = mergeState.Readers.Count;
            mergeState.MatchingSegmentReaders = new SegmentReader[numReaders];

            // If this reader is a SegmentReader, and all of its
            // field name -> number mappings match the "merged"
            // FieldInfos, then we can do a bulk copy of the
            // stored fields:
            for (int i = 0; i < numReaders; i++)
            {
                AtomicReader reader = mergeState.Readers[i];
                // TODO: we may be able to broaden this to
                // non-SegmentReaders, since FieldInfos is now
                // required?  But... this'd also require exposing
                // bulk-copy (TVs and stored fields) API in foreign
                // readers..
                if (reader is SegmentReader segmentReader)
                {
                    bool same = true;
                    FieldInfos segmentFieldInfos = segmentReader.FieldInfos;
                    foreach (FieldInfo fi in segmentFieldInfos)
                    {
                        FieldInfo other = mergeState.FieldInfos.FieldInfo(fi.Number);
                        if (other is null || !other.Name.Equals(fi.Name, StringComparison.Ordinal))
                        {
                            same = false;
                            break;
                        }
                    }
                    if (same)
                    {
                        mergeState.MatchingSegmentReaders[i] = segmentReader;
                        mergeState.MatchedCount++;
                    }
                }
            }

            if (mergeState.InfoStream.IsEnabled("SM"))
            {
                mergeState.InfoStream.Message("SM", "merge store matchedCount=" + mergeState.MatchedCount + " vs " + mergeState.Readers.Count);
                if (mergeState.MatchedCount != mergeState.Readers.Count)
                {
                    mergeState.InfoStream.Message("SM", "" + (mergeState.Readers.Count - mergeState.MatchedCount) + " non-bulk merges");
                }
            }
        }

        public void MergeFieldInfos()
        {
            foreach (AtomicReader reader in mergeState.Readers)
            {
                FieldInfos readerFieldInfos = reader.FieldInfos;
                foreach (FieldInfo fi in readerFieldInfos)
                {
                    fieldInfosBuilder.Add(fi);
                }
            }
            mergeState.FieldInfos = fieldInfosBuilder.Finish();
        }

        ///
        /// <returns> The number of documents in all of the readers </returns>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        private int MergeFields()
        {
            StoredFieldsWriter fieldsWriter = codec.StoredFieldsFormat.FieldsWriter(directory, mergeState.SegmentInfo, context);

            try
            {
                return fieldsWriter.Merge(mergeState);
            }
            finally
            {
                fieldsWriter.Dispose();
            }
        }

        /// <summary>
        /// Merge the TermVectors from each of the segments into the new one. </summary>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        private int MergeVectors()
        {
            TermVectorsWriter termVectorsWriter = codec.TermVectorsFormat.VectorsWriter(directory, mergeState.SegmentInfo, context);

            try
            {
                return termVectorsWriter.Merge(mergeState);
            }
            finally
            {
                termVectorsWriter.Dispose();
            }
        }

        // NOTE: removes any "all deleted" readers from mergeState.readers
        private int SetDocMaps()
        {
            int numReaders = mergeState.Readers.Count;

            // Remap docIDs
            mergeState.DocMaps = new MergeState.DocMap[numReaders];
            mergeState.DocBase = new int[numReaders];

            int docBase = 0;

            int i = 0;
            while (i < mergeState.Readers.Count)
            {
                AtomicReader reader = mergeState.Readers[i];

                mergeState.DocBase[i] = docBase;
                MergeState.DocMap docMap = MergeState.DocMap.Build(reader);
                mergeState.DocMaps[i] = docMap;
                docBase += docMap.NumDocs;

                i++;
            }

            return docBase;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void MergeTerms(SegmentWriteState segmentWriteState)
        {
            IList<Fields> fields = new JCG.List<Fields>();
            IList<ReaderSlice> slices = new JCG.List<ReaderSlice>();

            int docBase = 0;

            for (int readerIndex = 0; readerIndex < mergeState.Readers.Count; readerIndex++)
            {
                AtomicReader reader = mergeState.Readers[readerIndex];
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
                consumer.Merge(mergeState, new MultiFields(fields.ToArray(/*Fields.EMPTY_ARRAY*/), slices.ToArray(/*ReaderSlice.EMPTY_ARRAY*/)));
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(consumer);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(consumer);
                }
            }
        }
    }
}