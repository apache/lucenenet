using J2N.Threading.Atomic;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Codecs.Lucene40
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

    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using IBits = Lucene.Net.Util.IBits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;
    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
    using Directory = Lucene.Net.Store.Directory;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IOUtils = Lucene.Net.Util.IOUtils;
    //using LegacyDocValuesType = Lucene.Net.Codecs.Lucene40.LegacyDocValuesType;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using PagedBytes = Lucene.Net.Util.PagedBytes;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;

    /// <summary>
    /// Reads the 4.0 format of norms/docvalues.
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    [Obsolete("Only for reading old 4.0 and 4.1 segments")]
    internal sealed class Lucene40DocValuesReader : DocValuesProducer
    {
        private readonly Directory dir;
        private readonly SegmentReadState state;
        private readonly string legacyKey;
        private const string segmentSuffix = "dv";

        // ram instances we have already loaded
        private readonly IDictionary<int, NumericDocValues> numericInstances = new Dictionary<int, NumericDocValues>();

        private readonly IDictionary<int, BinaryDocValues> binaryInstances = new Dictionary<int, BinaryDocValues>();
        private readonly IDictionary<int, SortedDocValues> sortedInstances = new Dictionary<int, SortedDocValues>();

        private readonly AtomicInt64 ramBytesUsed;

        internal Lucene40DocValuesReader(SegmentReadState state, string filename, string legacyKey)
        {
            this.state = state;
            this.legacyKey = legacyKey;
            this.dir = new CompoundFileDirectory(state.Directory, filename, state.Context, false);
            ramBytesUsed = new AtomicInt64(RamUsageEstimator.ShallowSizeOf(this.GetType()));
        }

        public override NumericDocValues GetNumeric(FieldInfo field)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (!numericInstances.TryGetValue(field.Number, out NumericDocValues instance))
                {
                    string fileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number, CultureInfo.InvariantCulture), segmentSuffix, "dat");
                    IndexInput input = dir.OpenInput(fileName, state.Context);
                    bool success = false;
                    try
                    {
                        var type = field.GetAttribute(legacyKey).ToLegacyDocValuesType();

                        //switch (Enum.Parse(typeof(LegacyDocValuesType), field.GetAttribute(LegacyKey)))
                        //{
                        if (type == LegacyDocValuesType.VAR_INTS)
                        {
                            instance = LoadVarInt32sField(/* field, // LUCENENET: Never read */ input);
                        }
                        else if (type == LegacyDocValuesType.FIXED_INTS_8)
                        {
                            instance = LoadByteField(/* field, // LUCENENET: Never read */ input);
                        }
                        else if (type == LegacyDocValuesType.FIXED_INTS_16)
                        {
                            instance = LoadInt16Field(/* field, // LUCENENET: Never read */ input);
                        }
                        else if (type == LegacyDocValuesType.FIXED_INTS_32)
                        {
                            instance = LoadInt32Field(/* field, // LUCENENET: Never read */ input);
                        }
                        else if (type == LegacyDocValuesType.FIXED_INTS_64)
                        {
                            instance = LoadInt64Field(/* field, // LUCENENET: Never read */ input);
                        }
                        else if (type == LegacyDocValuesType.FLOAT_32)
                        {
                            instance = LoadSingleField(/* field, // LUCENENET: Never read */ input);
                        }
                        else if (type == LegacyDocValuesType.FLOAT_64)
                        {
                            instance = LoadDoubleField(/* field, // LUCENENET: Never read */ input);
                        }
                        else
                        {
                            throw AssertionError.Create();
                        }

                        CodecUtil.CheckEOF(input);
                        success = true;
                    }
                    finally
                    {
                        if (success)
                        {
                            IOUtils.Dispose(input);
                        }
                        else
                        {
                            IOUtils.DisposeWhileHandlingException(input);
                        }
                    }
                    numericInstances[field.Number] = instance;
                }
                return instance;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// NOTE: This was loadVarIntsField() in Lucene.
        /// </summary>
        private NumericDocValues LoadVarInt32sField(/*FieldInfo field, // LUCENENET: Never read */ IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.VAR_INTS_CODEC_NAME, Lucene40DocValuesFormat.VAR_INTS_VERSION_START, Lucene40DocValuesFormat.VAR_INTS_VERSION_CURRENT);
            var header = (sbyte)input.ReadByte();
            if (header == Lucene40DocValuesFormat.VAR_INTS_FIXED_64)
            {
                int maxDoc = state.SegmentInfo.DocCount;
                var values = new long[maxDoc];
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = input.ReadInt64();
                }
                ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
                return new NumericDocValuesAnonymousClass(values);
            }
            else if (header == Lucene40DocValuesFormat.VAR_INTS_PACKED)
            {
                long minValue = input.ReadInt64();
                long defaultValue = input.ReadInt64();
                PackedInt32s.Reader reader = PackedInt32s.GetReader(input);
                ramBytesUsed.AddAndGet(reader.RamBytesUsed());
                return new NumericDocValuesAnonymousClass2(minValue, defaultValue, reader);
            }
            else
            {
                throw new CorruptIndexException("invalid VAR_INTS header byte: " + header + " (resource=" + input + ")");
            }
        }

        private sealed class NumericDocValuesAnonymousClass : NumericDocValues
        {
            private readonly long[] values;

            public NumericDocValuesAnonymousClass(long[] values)
            {
                this.values = values;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long Get(int docID)
            {
                return values[docID];
            }
        }

        private sealed class NumericDocValuesAnonymousClass2 : NumericDocValues
        {
            private readonly long minValue;
            private readonly long defaultValue;
            private readonly PackedInt32s.Reader reader;

            public NumericDocValuesAnonymousClass2(long minValue, long defaultValue, PackedInt32s.Reader reader)
            {
                this.minValue = minValue;
                this.defaultValue = defaultValue;
                this.reader = reader;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long Get(int docID)
            {
                long value = reader.Get(docID);
                if (value == defaultValue)
                {
                    return 0;
                }
                else
                {
                    return minValue + value;
                }
            }
        }

        private NumericDocValues LoadByteField(/*FieldInfo field, // LUCENENET: Never read */ IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.INTS_CODEC_NAME, Lucene40DocValuesFormat.INTS_VERSION_START, Lucene40DocValuesFormat.INTS_VERSION_CURRENT);
            int valueSize = input.ReadInt32();
            if (valueSize != 1)
            {
                throw new CorruptIndexException("invalid valueSize: " + valueSize);
            }
            int maxDoc = state.SegmentInfo.DocCount;
            var values = new byte[maxDoc];
            input.ReadBytes(values, 0, values.Length);
            ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
            return new NumericDocValuesAnonymousClass3(values);
        }

        private sealed class NumericDocValuesAnonymousClass3 : NumericDocValues
        {
            private readonly byte[] values;

            public NumericDocValuesAnonymousClass3(byte[] values)
            {
                this.values = values;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long Get(int docID)
            {
                return (sbyte)values[docID];
            }
        }

        /// <summary>
        /// NOTE: This was loadShortField() in Lucene.
        /// </summary>
        private NumericDocValues LoadInt16Field(/*FieldInfo field, // LUCENENET: Never read */ IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.INTS_CODEC_NAME, Lucene40DocValuesFormat.INTS_VERSION_START, Lucene40DocValuesFormat.INTS_VERSION_CURRENT);
            int valueSize = input.ReadInt32();
            if (valueSize != 2)
            {
                throw new CorruptIndexException("invalid valueSize: " + valueSize);
            }
            int maxDoc = state.SegmentInfo.DocCount;
            short[] values = new short[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadInt16();
            }
            ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
            return new NumericDocValuesAnonymousClass4(values);
        }

        private sealed class NumericDocValuesAnonymousClass4 : NumericDocValues
        {
            private readonly short[] values;

            public NumericDocValuesAnonymousClass4(short[] values)
            {
                this.values = values;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long Get(int docID)
            {
                return values[docID];
            }
        }

        /// <summary>
        /// NOTE: This was loadIntField() in Lucene.
        /// </summary>
        private NumericDocValues LoadInt32Field(/*FieldInfo field, // LUCENENET: Never read */ IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.INTS_CODEC_NAME, Lucene40DocValuesFormat.INTS_VERSION_START, Lucene40DocValuesFormat.INTS_VERSION_CURRENT);
            int valueSize = input.ReadInt32();
            if (valueSize != 4)
            {
                throw new CorruptIndexException("invalid valueSize: " + valueSize);
            }
            int maxDoc = state.SegmentInfo.DocCount;
            var values = new int[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadInt32();
            }
            ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
            return new NumericDocValuesAnonymousClass5(values);
        }

        private sealed class NumericDocValuesAnonymousClass5 : NumericDocValues
        {
            private readonly int[] values;

            public NumericDocValuesAnonymousClass5(int[] values)
            {
                this.values = values;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long Get(int docID)
            {
                return values[docID];
            }
        }

        /// <summary>
        /// NOTE: This was loadLongField() in Lucene.
        /// </summary>
        private NumericDocValues LoadInt64Field(/*FieldInfo field, // LUCENENET: Never read */ IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.INTS_CODEC_NAME, Lucene40DocValuesFormat.INTS_VERSION_START, Lucene40DocValuesFormat.INTS_VERSION_CURRENT);
            int valueSize = input.ReadInt32();
            if (valueSize != 8)
            {
                throw new CorruptIndexException("invalid valueSize: " + valueSize);
            }
            int maxDoc = state.SegmentInfo.DocCount;
            long[] values = new long[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadInt64();
            }
            ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
            return new NumericDocValuesAnonymousClass6(values);
        }

        private sealed class NumericDocValuesAnonymousClass6 : NumericDocValues
        {
            private readonly long[] values;

            public NumericDocValuesAnonymousClass6(long[] values)
            {
                this.values = values;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long Get(int docID)
            {
                return values[docID];
            }
        }

        /// <summary>
        /// NOTE: This was loadFloatField() in Lucene.
        /// </summary>
        private NumericDocValues LoadSingleField(/*FieldInfo field, // LUCENENET: Never read */ IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.FLOATS_CODEC_NAME, Lucene40DocValuesFormat.FLOATS_VERSION_START, Lucene40DocValuesFormat.FLOATS_VERSION_CURRENT);
            int valueSize = input.ReadInt32();
            if (valueSize != 4)
            {
                throw new CorruptIndexException("invalid valueSize: " + valueSize);
            }
            int maxDoc = state.SegmentInfo.DocCount;
            int[] values = new int[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadInt32();
            }
            ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
            return new NumericDocValuesAnonymousClass7(values);
        }

        private sealed class NumericDocValuesAnonymousClass7 : NumericDocValues
        {
            private readonly int[] values;

            public NumericDocValuesAnonymousClass7(int[] values)
            {
                this.values = values;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long Get(int docID)
            {
                return values[docID];
            }
        }

        private NumericDocValues LoadDoubleField(/*FieldInfo field, // LUCENENET: Never read */ IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.FLOATS_CODEC_NAME, Lucene40DocValuesFormat.FLOATS_VERSION_START, Lucene40DocValuesFormat.FLOATS_VERSION_CURRENT);
            int valueSize = input.ReadInt32();
            if (valueSize != 8)
            {
                throw new CorruptIndexException("invalid valueSize: " + valueSize);
            }
            int maxDoc = state.SegmentInfo.DocCount;
            long[] values = new long[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadInt64();
            }
            ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
            return new NumericDocValuesAnonymousClass8(values);
        }

        private sealed class NumericDocValuesAnonymousClass8 : NumericDocValues
        {
            private readonly long[] values;

            public NumericDocValuesAnonymousClass8(long[] values)
            {
                this.values = values;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long Get(int docID)
            {
                return values[docID];
            }
        }

        public override BinaryDocValues GetBinary(FieldInfo field)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (!binaryInstances.TryGetValue(field.Number, out BinaryDocValues instance))
                {
                    var type = field.GetAttribute(legacyKey).ToLegacyDocValuesType();

                    if (type == LegacyDocValuesType.BYTES_FIXED_STRAIGHT)
                    {
                        instance = LoadBytesFixedStraight(field);
                    }
                    else if (type == LegacyDocValuesType.BYTES_VAR_STRAIGHT)
                    {
                        instance = LoadBytesVarStraight(field);
                    }
                    else if (type == LegacyDocValuesType.BYTES_FIXED_DEREF)
                    {
                        instance = LoadBytesFixedDeref(field);
                    }
                    else if (type == LegacyDocValuesType.BYTES_VAR_DEREF)
                    {
                        instance = LoadBytesVarDeref(field);
                    }
                    else
                    {
                        throw AssertionError.Create();
                    }
                    binaryInstances[field.Number] = instance;
                }
                return instance;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private BinaryDocValues LoadBytesFixedStraight(FieldInfo field)
        {
            string fileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number, CultureInfo.InvariantCulture), segmentSuffix, "dat");
            IndexInput input = dir.OpenInput(fileName, state.Context);
            bool success = false;
            try
            {
                CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.BYTES_FIXED_STRAIGHT_CODEC_NAME, Lucene40DocValuesFormat.BYTES_FIXED_STRAIGHT_VERSION_START, Lucene40DocValuesFormat.BYTES_FIXED_STRAIGHT_VERSION_CURRENT);
                int fixedLength = input.ReadInt32();
                var bytes = new PagedBytes(16);
                bytes.Copy(input, fixedLength * (long)state.SegmentInfo.DocCount);
                PagedBytes.Reader bytesReader = bytes.Freeze(true);
                CodecUtil.CheckEOF(input);
                success = true;
                ramBytesUsed.AddAndGet(bytes.RamBytesUsed());
                return new BinaryDocValuesAnonymousClass(fixedLength, bytesReader);
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(input);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(input);
                }
            }
        }

        private sealed class BinaryDocValuesAnonymousClass : BinaryDocValues
        {
            private readonly int fixedLength;
            private readonly PagedBytes.Reader bytesReader;

            public BinaryDocValuesAnonymousClass(int fixedLength, PagedBytes.Reader bytesReader)
            {
                this.fixedLength = fixedLength;
                this.bytesReader = bytesReader;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void Get(int docID, BytesRef result)
            {
                bytesReader.FillSlice(result, fixedLength * (long)docID, fixedLength);
            }
        }

        private BinaryDocValues LoadBytesVarStraight(FieldInfo field)
        {
            string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number, CultureInfo.InvariantCulture), segmentSuffix, "dat");
            string indexName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number, CultureInfo.InvariantCulture), segmentSuffix, "idx");
            IndexInput data = null;
            IndexInput index = null;
            bool success = false;
            try
            {
                data = dir.OpenInput(dataName, state.Context);
                CodecUtil.CheckHeader(data, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_START, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_CURRENT);
                index = dir.OpenInput(indexName, state.Context);
                CodecUtil.CheckHeader(index, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_START, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_CURRENT);
                long totalBytes = index.ReadVInt64();
                PagedBytes bytes = new PagedBytes(16);
                bytes.Copy(data, totalBytes);
                PagedBytes.Reader bytesReader = bytes.Freeze(true);
                PackedInt32s.Reader reader = PackedInt32s.GetReader(index);
                CodecUtil.CheckEOF(data);
                CodecUtil.CheckEOF(index);
                success = true;
                ramBytesUsed.AddAndGet(bytes.RamBytesUsed() + reader.RamBytesUsed());
                return new BinaryDocValuesAnonymousClass2(bytesReader, reader);
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(data, index);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(data, index);
                }
            }
        }

        private sealed class BinaryDocValuesAnonymousClass2 : BinaryDocValues
        {
            private readonly PagedBytes.Reader bytesReader;
            private readonly PackedInt32s.Reader reader;

            public BinaryDocValuesAnonymousClass2(PagedBytes.Reader bytesReader, PackedInt32s.Reader reader)
            {
                this.bytesReader = bytesReader;
                this.reader = reader;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void Get(int docID, BytesRef result)
            {
                long startAddress = reader.Get(docID);
                long endAddress = reader.Get(docID + 1);
                bytesReader.FillSlice(result, startAddress, (int)(endAddress - startAddress));
            }
        }

        private BinaryDocValues LoadBytesFixedDeref(FieldInfo field)
        {
            string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number, CultureInfo.InvariantCulture), segmentSuffix, "dat");
            string indexName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number, CultureInfo.InvariantCulture), segmentSuffix, "idx");
            IndexInput data = null;
            IndexInput index = null;
            bool success = false;
            try
            {
                data = dir.OpenInput(dataName, state.Context);
                CodecUtil.CheckHeader(data, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_START, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_CURRENT);
                index = dir.OpenInput(indexName, state.Context);
                CodecUtil.CheckHeader(index, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_START, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_CURRENT);

                int fixedLength = data.ReadInt32();
                int valueCount = index.ReadInt32();
                PagedBytes bytes = new PagedBytes(16);
                bytes.Copy(data, fixedLength * (long)valueCount);
                PagedBytes.Reader bytesReader = bytes.Freeze(true);
                PackedInt32s.Reader reader = PackedInt32s.GetReader(index);
                CodecUtil.CheckEOF(data);
                CodecUtil.CheckEOF(index);
                ramBytesUsed.AddAndGet(bytes.RamBytesUsed() + reader.RamBytesUsed());
                success = true;
                return new BinaryDocValuesAnonymousClass3(fixedLength, bytesReader, reader);
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(data, index);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(data, index);
                }
            }
        }

        private sealed class BinaryDocValuesAnonymousClass3 : BinaryDocValues
        {
            private readonly int fixedLength;
            private readonly PagedBytes.Reader bytesReader;
            private readonly PackedInt32s.Reader reader;

            public BinaryDocValuesAnonymousClass3(int fixedLength, PagedBytes.Reader bytesReader, PackedInt32s.Reader reader)
            {
                this.fixedLength = fixedLength;
                this.bytesReader = bytesReader;
                this.reader = reader;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void Get(int docID, BytesRef result)
            {
                long offset = fixedLength * reader.Get(docID);
                bytesReader.FillSlice(result, offset, fixedLength);
            }
        }

        private BinaryDocValues LoadBytesVarDeref(FieldInfo field)
        {
            string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number, CultureInfo.InvariantCulture), segmentSuffix, "dat");
            string indexName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number, CultureInfo.InvariantCulture), segmentSuffix, "idx");
            IndexInput data = null;
            IndexInput index = null;
            bool success = false;
            try
            {
                data = dir.OpenInput(dataName, state.Context);
                CodecUtil.CheckHeader(data, Lucene40DocValuesFormat.BYTES_VAR_DEREF_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_START, Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_CURRENT);
                index = dir.OpenInput(indexName, state.Context);
                CodecUtil.CheckHeader(index, Lucene40DocValuesFormat.BYTES_VAR_DEREF_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_START, Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_CURRENT);

                long totalBytes = index.ReadInt64();
                PagedBytes bytes = new PagedBytes(16);
                bytes.Copy(data, totalBytes);
                PagedBytes.Reader bytesReader = bytes.Freeze(true);
                PackedInt32s.Reader reader = PackedInt32s.GetReader(index);
                CodecUtil.CheckEOF(data);
                CodecUtil.CheckEOF(index);
                ramBytesUsed.AddAndGet(bytes.RamBytesUsed() + reader.RamBytesUsed());
                success = true;
                return new BinaryDocValuesAnonymousClass4(bytesReader, reader);
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(data, index);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(data, index);
                }
            }
        }

        private sealed class BinaryDocValuesAnonymousClass4 : BinaryDocValues
        {
            private readonly PagedBytes.Reader bytesReader;
            private readonly PackedInt32s.Reader reader;

            public BinaryDocValuesAnonymousClass4(PagedBytes.Reader bytesReader, PackedInt32s.Reader reader)
            {
                this.bytesReader = bytesReader;
                this.reader = reader;
            }

            public override void Get(int docID, BytesRef result)
            {
                long startAddress = reader.Get(docID);
                BytesRef lengthBytes = new BytesRef();
                bytesReader.FillSlice(lengthBytes, startAddress, 1);
                var code = lengthBytes.Bytes[lengthBytes.Offset];
                if ((code & 128) == 0)
                {
                    // length is 1 byte
                    bytesReader.FillSlice(result, startAddress + 1, (int)code);
                }
                else
                {
                    bytesReader.FillSlice(lengthBytes, startAddress + 1, 1);
                    int length = ((code & 0x7f) << 8) | (lengthBytes.Bytes[lengthBytes.Offset] & 0xff);
                    bytesReader.FillSlice(result, startAddress + 2, length);
                }
            }
        }

        public override SortedDocValues GetSorted(FieldInfo field)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (!sortedInstances.TryGetValue(field.Number, out SortedDocValues instance))
                {
                    string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number, CultureInfo.InvariantCulture), segmentSuffix, "dat");
                    string indexName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number, CultureInfo.InvariantCulture), segmentSuffix, "idx");
                    IndexInput data = null;
                    IndexInput index = null;
                    bool success = false;
                    try
                    {
                        data = dir.OpenInput(dataName, state.Context);
                        index = dir.OpenInput(indexName, state.Context);

                        var type = field.GetAttribute(legacyKey).ToLegacyDocValuesType();

                        if (type == LegacyDocValuesType.BYTES_FIXED_SORTED)
                        {
                            instance = LoadBytesFixedSorted(/* field, // LUCENENET: Never read */ data, index);
                        }
                        else if (type == LegacyDocValuesType.BYTES_VAR_SORTED)
                        {
                            instance = LoadBytesVarSorted(/* field, // LUCENENET: Never read */ data, index);
                        }
                        else
                        {
                            throw AssertionError.Create();
                        }

                        CodecUtil.CheckEOF(data);
                        CodecUtil.CheckEOF(index);
                        success = true;
                    }
                    finally
                    {
                        if (success)
                        {
                            IOUtils.Dispose(data, index);
                        }
                        else
                        {
                            IOUtils.DisposeWhileHandlingException(data, index);
                        }
                    }
                    sortedInstances[field.Number] = instance;
                }
                return instance;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private SortedDocValues LoadBytesFixedSorted(/*FieldInfo field, // LUCENENET: Never read */ IndexInput data, IndexInput index)
        {
            CodecUtil.CheckHeader(data, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_VERSION_START, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_VERSION_CURRENT);
            CodecUtil.CheckHeader(index, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_VERSION_START, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_VERSION_CURRENT);

            int fixedLength = data.ReadInt32();
            int valueCount = index.ReadInt32();

            PagedBytes bytes = new PagedBytes(16);
            bytes.Copy(data, fixedLength * (long)valueCount);
            PagedBytes.Reader bytesReader = bytes.Freeze(true);
            PackedInt32s.Reader reader = PackedInt32s.GetReader(index);
            ramBytesUsed.AddAndGet(bytes.RamBytesUsed() + reader.RamBytesUsed());

            return CorrectBuggyOrds(new SortedDocValuesAnonymousClass(fixedLength, valueCount, bytesReader, reader));
        }

        private sealed class SortedDocValuesAnonymousClass : SortedDocValues
        {
            private readonly int fixedLength;
            private readonly int valueCount;
            private readonly PagedBytes.Reader bytesReader;
            private readonly PackedInt32s.Reader reader;

            public SortedDocValuesAnonymousClass(int fixedLength, int valueCount, PagedBytes.Reader bytesReader, PackedInt32s.Reader reader)
            {
                this.fixedLength = fixedLength;
                this.valueCount = valueCount;
                this.bytesReader = bytesReader;
                this.reader = reader;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetOrd(int docID)
            {
                return (int)reader.Get(docID);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void LookupOrd(int ord, BytesRef result)
            {
                bytesReader.FillSlice(result, fixedLength * (long)ord, fixedLength);
            }

            public override int ValueCount => valueCount;
        }

        private SortedDocValues LoadBytesVarSorted(/*FieldInfo field, // LUCENENET: Never read */ IndexInput data, IndexInput index)
        {
            CodecUtil.CheckHeader(data, Lucene40DocValuesFormat.BYTES_VAR_SORTED_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_VAR_SORTED_VERSION_START, Lucene40DocValuesFormat.BYTES_VAR_SORTED_VERSION_CURRENT);
            CodecUtil.CheckHeader(index, Lucene40DocValuesFormat.BYTES_VAR_SORTED_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_VAR_SORTED_VERSION_START, Lucene40DocValuesFormat.BYTES_VAR_SORTED_VERSION_CURRENT);

            long maxAddress = index.ReadInt64();
            PagedBytes bytes = new PagedBytes(16);
            bytes.Copy(data, maxAddress);
            PagedBytes.Reader bytesReader = bytes.Freeze(true);
            PackedInt32s.Reader addressReader = PackedInt32s.GetReader(index);
            PackedInt32s.Reader ordsReader = PackedInt32s.GetReader(index);

            int valueCount = addressReader.Count - 1;
            ramBytesUsed.AddAndGet(bytes.RamBytesUsed() + addressReader.RamBytesUsed() + ordsReader.RamBytesUsed());

            return CorrectBuggyOrds(new SortedDocValuesAnonymousClass2(bytesReader, addressReader, ordsReader, valueCount));
        }

        private sealed class SortedDocValuesAnonymousClass2 : SortedDocValues
        {
            private readonly PagedBytes.Reader bytesReader;
            private readonly PackedInt32s.Reader addressReader;
            private readonly PackedInt32s.Reader ordsReader;
            private readonly int valueCount;

            public SortedDocValuesAnonymousClass2(PagedBytes.Reader bytesReader, PackedInt32s.Reader addressReader, PackedInt32s.Reader ordsReader, int valueCount)
            {
                this.bytesReader = bytesReader;
                this.addressReader = addressReader;
                this.ordsReader = ordsReader;
                this.valueCount = valueCount;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetOrd(int docID)
            {
                return (int)ordsReader.Get(docID);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void LookupOrd(int ord, BytesRef result)
            {
                long startAddress = addressReader.Get(ord);
                long endAddress = addressReader.Get(ord + 1);
                bytesReader.FillSlice(result, startAddress, (int)(endAddress - startAddress));
            }

            public override int ValueCount => valueCount;
        }

        // detects and corrects LUCENE-4717 in old indexes
        private SortedDocValues CorrectBuggyOrds(SortedDocValues @in)
        {
            int maxDoc = state.SegmentInfo.DocCount;
            for (int i = 0; i < maxDoc; i++)
            {
                if (@in.GetOrd(i) == 0)
                {
                    return @in; // ok
                }
            }

            // we had ord holes, return an ord-shifting-impl that corrects the bug
            return new SortedDocValuesAnonymousClass3(@in);
        }

        private sealed class SortedDocValuesAnonymousClass3 : SortedDocValues
        {
            private readonly SortedDocValues @in;

            public SortedDocValuesAnonymousClass3(SortedDocValues @in)
            {
                this.@in = @in;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetOrd(int docID)
            {
                return @in.GetOrd(docID) - 1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void LookupOrd(int ord, BytesRef result)
            {
                @in.LookupOrd(ord + 1, result);
            }

            public override int ValueCount => @in.ValueCount - 1;
        }

        public override SortedSetDocValues GetSortedSet(FieldInfo field)
        {
            throw IllegalStateException.Create("Lucene 4.0 does not support SortedSet: how did you pull this off?");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override IBits GetDocsWithField(FieldInfo field)
        {
            return new Lucene.Net.Util.Bits.MatchAllBits(state.SegmentInfo.DocCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                dir.Dispose();
            }
        }

        public override long RamBytesUsed() => ramBytesUsed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void CheckIntegrity() { }
    }
}