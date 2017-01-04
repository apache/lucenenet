using Lucene.Net.Support;
using System;
using System.Collections.Generic;

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
    using LegacyDocValuesType = Lucene.Net.Codecs.Lucene40.Lucene40FieldInfosReader.LegacyDocValuesType;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using PagedBytes = Lucene.Net.Util.PagedBytes;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;

    /// <summary>
    /// Reads the 4.0 format of norms/docvalues
    /// @lucene.experimental </summary>
    /// @deprecated Only for reading old 4.0 and 4.1 segments
    [Obsolete("Only for reading old 4.0 and 4.1 segments")]
    internal sealed class Lucene40DocValuesReader : DocValuesProducer
    {
        private readonly Directory dir;
        private readonly SegmentReadState state;
        private readonly string legacyKey;
        private static readonly string segmentSuffix = "dv";

        // ram instances we have already loaded
        private readonly IDictionary<int, NumericDocValues> numericInstances = new Dictionary<int, NumericDocValues>();

        private readonly IDictionary<int, BinaryDocValues> binaryInstances = new Dictionary<int, BinaryDocValues>();
        private readonly IDictionary<int, SortedDocValues> sortedInstances = new Dictionary<int, SortedDocValues>();

        private readonly AtomicLong ramBytesUsed;

        internal Lucene40DocValuesReader(SegmentReadState state, string filename, string legacyKey)
        {
            this.state = state;
            this.legacyKey = legacyKey;
            this.dir = new CompoundFileDirectory(state.Directory, filename, state.Context, false);
            ramBytesUsed = new AtomicLong(RamUsageEstimator.ShallowSizeOf(this.GetType()));
        }

        public override NumericDocValues GetNumeric(FieldInfo field)
        {
            lock (this)
            {
                NumericDocValues instance;
                if (!numericInstances.TryGetValue(field.Number, out instance))
                {
                    string fileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number), segmentSuffix, "dat");
                    IndexInput input = dir.OpenInput(fileName, state.Context);
                    bool success = false;
                    try
                    {
                        var type = LegacyDocValuesType.ValueOf(field.GetAttribute(legacyKey));

                        //switch (Enum.Parse(typeof(LegacyDocValuesType), field.GetAttribute(LegacyKey)))
                        //{
                        if (type == LegacyDocValuesType.VAR_INTS)
                        {
                            instance = LoadVarIntsField(field, input);
                        }
                        else if (type == LegacyDocValuesType.FIXED_INTS_8)
                        {
                            instance = LoadByteField(field, input);
                        }
                        else if (type == LegacyDocValuesType.FIXED_INTS_16)
                        {
                            instance = LoadShortField(field, input);
                        }
                        else if (type == LegacyDocValuesType.FIXED_INTS_32)
                        {
                            instance = LoadIntField(field, input);
                        }
                        else if (type == LegacyDocValuesType.FIXED_INTS_64)
                        {
                            instance = LoadLongField(field, input);
                        }
                        else if (type == LegacyDocValuesType.FLOAT_32)
                        {
                            instance = LoadFloatField(field, input);
                        }
                        else if (type == LegacyDocValuesType.FLOAT_64)
                        {
                            instance = LoadDoubleField(field, input);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }

                        CodecUtil.CheckEOF(input);
                        success = true;
                    }
                    finally
                    {
                        if (success)
                        {
                            IOUtils.Close(input);
                        }
                        else
                        {
                            IOUtils.CloseWhileHandlingException(input);
                        }
                    }
                    numericInstances[field.Number] = instance;
                }
                return instance;
            }
        }

        private NumericDocValues LoadVarIntsField(FieldInfo field, IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.VAR_INTS_CODEC_NAME, Lucene40DocValuesFormat.VAR_INTS_VERSION_START, Lucene40DocValuesFormat.VAR_INTS_VERSION_CURRENT);
            var header = (sbyte)input.ReadByte();
            if (header == Lucene40DocValuesFormat.VAR_INTS_FIXED_64)
            {
                int maxDoc = state.SegmentInfo.DocCount;
                var values = new long[maxDoc];
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = input.ReadLong();
                }
                ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
                return new NumericDocValuesAnonymousInnerClassHelper(values);
            }
            else if (header == Lucene40DocValuesFormat.VAR_INTS_PACKED)
            {
                long minValue = input.ReadLong();
                long defaultValue = input.ReadLong();
                PackedInts.Reader reader = PackedInts.GetReader(input);
                ramBytesUsed.AddAndGet(reader.RamBytesUsed());
                return new NumericDocValuesAnonymousInnerClassHelper2(minValue, defaultValue, reader);
            }
            else
            {
                throw new CorruptIndexException("invalid VAR_INTS header byte: " + header + " (resource=" + input + ")");
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper : NumericDocValues
        {
            private readonly long[] values;

            public NumericDocValuesAnonymousInnerClassHelper(long[] values)
            {
                this.values = values;
            }

            public override long Get(int docID)
            {
                return values[docID];
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper2 : NumericDocValues
        {
            private readonly long minValue;
            private readonly long defaultValue;
            private readonly PackedInts.Reader reader;

            public NumericDocValuesAnonymousInnerClassHelper2(long minValue, long defaultValue, PackedInts.Reader reader)
            {
                this.minValue = minValue;
                this.defaultValue = defaultValue;
                this.reader = reader;
            }

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

        private NumericDocValues LoadByteField(FieldInfo field, IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.INTS_CODEC_NAME, Lucene40DocValuesFormat.INTS_VERSION_START, Lucene40DocValuesFormat.INTS_VERSION_CURRENT);
            int valueSize = input.ReadInt();
            if (valueSize != 1)
            {
                throw new CorruptIndexException("invalid valueSize: " + valueSize);
            }
            int maxDoc = state.SegmentInfo.DocCount;
            var values = new byte[maxDoc];
            input.ReadBytes(values, 0, values.Length);
            ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
            return new NumericDocValuesAnonymousInnerClassHelper3(values);
        }

        private class NumericDocValuesAnonymousInnerClassHelper3 : NumericDocValues
        {
            private readonly byte[] values;

            public NumericDocValuesAnonymousInnerClassHelper3(byte[] values)
            {
                this.values = values;
            }

            public override long Get(int docID)
            {
                return (sbyte)values[docID];
            }
        }

        private NumericDocValues LoadShortField(FieldInfo field, IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.INTS_CODEC_NAME, Lucene40DocValuesFormat.INTS_VERSION_START, Lucene40DocValuesFormat.INTS_VERSION_CURRENT);
            int valueSize = input.ReadInt();
            if (valueSize != 2)
            {
                throw new CorruptIndexException("invalid valueSize: " + valueSize);
            }
            int maxDoc = state.SegmentInfo.DocCount;
            short[] values = new short[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadShort();
            }
            ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
            return new NumericDocValuesAnonymousInnerClassHelper4(values);
        }

        private class NumericDocValuesAnonymousInnerClassHelper4 : NumericDocValues
        {
            private readonly short[] values;

            public NumericDocValuesAnonymousInnerClassHelper4(short[] values)
            {
                this.values = values;
            }

            public override long Get(int docID)
            {
                return values[docID];
            }
        }

        private NumericDocValues LoadIntField(FieldInfo field, IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.INTS_CODEC_NAME, Lucene40DocValuesFormat.INTS_VERSION_START, Lucene40DocValuesFormat.INTS_VERSION_CURRENT);
            int valueSize = input.ReadInt();
            if (valueSize != 4)
            {
                throw new CorruptIndexException("invalid valueSize: " + valueSize);
            }
            int maxDoc = state.SegmentInfo.DocCount;
            var values = new int[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadInt();
            }
            ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
            return new NumericDocValuesAnonymousInnerClassHelper5(values);
        }

        private class NumericDocValuesAnonymousInnerClassHelper5 : NumericDocValues
        {
            private readonly int[] values;

            public NumericDocValuesAnonymousInnerClassHelper5(int[] values)
            {
                this.values = values;
            }

            public override long Get(int docID)
            {
                return values[docID];
            }
        }

        private NumericDocValues LoadLongField(FieldInfo field, IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.INTS_CODEC_NAME, Lucene40DocValuesFormat.INTS_VERSION_START, Lucene40DocValuesFormat.INTS_VERSION_CURRENT);
            int valueSize = input.ReadInt();
            if (valueSize != 8)
            {
                throw new CorruptIndexException("invalid valueSize: " + valueSize);
            }
            int maxDoc = state.SegmentInfo.DocCount;
            long[] values = new long[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadLong();
            }
            ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
            return new NumericDocValuesAnonymousInnerClassHelper6(values);
        }

        private class NumericDocValuesAnonymousInnerClassHelper6 : NumericDocValues
        {
            private readonly long[] values;

            public NumericDocValuesAnonymousInnerClassHelper6(long[] values)
            {
                this.values = values;
            }

            public override long Get(int docID)
            {
                return values[docID];
            }
        }

        private NumericDocValues LoadFloatField(FieldInfo field, IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.FLOATS_CODEC_NAME, Lucene40DocValuesFormat.FLOATS_VERSION_START, Lucene40DocValuesFormat.FLOATS_VERSION_CURRENT);
            int valueSize = input.ReadInt();
            if (valueSize != 4)
            {
                throw new CorruptIndexException("invalid valueSize: " + valueSize);
            }
            int maxDoc = state.SegmentInfo.DocCount;
            int[] values = new int[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadInt();
            }
            ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
            return new NumericDocValuesAnonymousInnerClassHelper7(values);
        }

        private class NumericDocValuesAnonymousInnerClassHelper7 : NumericDocValues
        {
            private readonly int[] values;

            public NumericDocValuesAnonymousInnerClassHelper7(int[] values)
            {
                this.values = values;
            }

            public override long Get(int docID)
            {
                return values[docID];
            }
        }

        private NumericDocValues LoadDoubleField(FieldInfo field, IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.FLOATS_CODEC_NAME, Lucene40DocValuesFormat.FLOATS_VERSION_START, Lucene40DocValuesFormat.FLOATS_VERSION_CURRENT);
            int valueSize = input.ReadInt();
            if (valueSize != 8)
            {
                throw new CorruptIndexException("invalid valueSize: " + valueSize);
            }
            int maxDoc = state.SegmentInfo.DocCount;
            long[] values = new long[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadLong();
            }
            ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
            return new NumericDocValuesAnonymousInnerClassHelper8(values);
        }

        private class NumericDocValuesAnonymousInnerClassHelper8 : NumericDocValues
        {
            private readonly long[] values;

            public NumericDocValuesAnonymousInnerClassHelper8(long[] values)
            {
                this.values = values;
            }

            public override long Get(int docID)
            {
                return values[docID];
            }
        }

        public override BinaryDocValues GetBinary(FieldInfo field)
        {
            lock (this)
            {
                BinaryDocValues instance;
                if (!binaryInstances.TryGetValue(field.Number, out instance))
                {
                    var type = LegacyDocValuesType.ValueOf(field.GetAttribute(legacyKey));

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
                        throw new InvalidOperationException();
                    }
                    binaryInstances[field.Number] = instance;
                }
                return instance;
            }
        }

        private BinaryDocValues LoadBytesFixedStraight(FieldInfo field)
        {
            string fileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number), segmentSuffix, "dat");
            IndexInput input = dir.OpenInput(fileName, state.Context);
            bool success = false;
            try
            {
                CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.BYTES_FIXED_STRAIGHT_CODEC_NAME, Lucene40DocValuesFormat.BYTES_FIXED_STRAIGHT_VERSION_START, Lucene40DocValuesFormat.BYTES_FIXED_STRAIGHT_VERSION_CURRENT);
                int fixedLength = input.ReadInt();
                var bytes = new PagedBytes(16);
                bytes.Copy(input, fixedLength * (long)state.SegmentInfo.DocCount);
                PagedBytes.Reader bytesReader = bytes.Freeze(true);
                CodecUtil.CheckEOF(input);
                success = true;
                ramBytesUsed.AddAndGet(bytes.RamBytesUsed());
                return new BinaryDocValuesAnonymousInnerClassHelper(fixedLength, bytesReader);
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(input);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(input);
                }
            }
        }

        private class BinaryDocValuesAnonymousInnerClassHelper : BinaryDocValues
        {
            private readonly int fixedLength;
            private readonly PagedBytes.Reader bytesReader;

            public BinaryDocValuesAnonymousInnerClassHelper(int fixedLength, PagedBytes.Reader bytesReader)
            {
                this.fixedLength = fixedLength;
                this.bytesReader = bytesReader;
            }

            public override void Get(int docID, BytesRef result)
            {
                bytesReader.FillSlice(result, fixedLength * (long)docID, fixedLength);
            }
        }

        private BinaryDocValues LoadBytesVarStraight(FieldInfo field)
        {
            string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number), segmentSuffix, "dat");
            string indexName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number), segmentSuffix, "idx");
            IndexInput data = null;
            IndexInput index = null;
            bool success = false;
            try
            {
                data = dir.OpenInput(dataName, state.Context);
                CodecUtil.CheckHeader(data, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_START, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_CURRENT);
                index = dir.OpenInput(indexName, state.Context);
                CodecUtil.CheckHeader(index, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_START, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_CURRENT);
                long totalBytes = index.ReadVLong();
                PagedBytes bytes = new PagedBytes(16);
                bytes.Copy(data, totalBytes);
                PagedBytes.Reader bytesReader = bytes.Freeze(true);
                PackedInts.Reader reader = PackedInts.GetReader(index);
                CodecUtil.CheckEOF(data);
                CodecUtil.CheckEOF(index);
                success = true;
                ramBytesUsed.AddAndGet(bytes.RamBytesUsed() + reader.RamBytesUsed());
                return new BinaryDocValuesAnonymousInnerClassHelper2(bytesReader, reader);
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(data, index);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(data, index);
                }
            }
        }

        private class BinaryDocValuesAnonymousInnerClassHelper2 : BinaryDocValues
        {
            private readonly PagedBytes.Reader bytesReader;
            private readonly PackedInts.Reader reader;

            public BinaryDocValuesAnonymousInnerClassHelper2(PagedBytes.Reader bytesReader, PackedInts.Reader reader)
            {
                this.bytesReader = bytesReader;
                this.reader = reader;
            }

            public override void Get(int docID, BytesRef result)
            {
                long startAddress = reader.Get(docID);
                long endAddress = reader.Get(docID + 1);
                bytesReader.FillSlice(result, startAddress, (int)(endAddress - startAddress));
            }
        }

        private BinaryDocValues LoadBytesFixedDeref(FieldInfo field)
        {
            string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number), segmentSuffix, "dat");
            string indexName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number), segmentSuffix, "idx");
            IndexInput data = null;
            IndexInput index = null;
            bool success = false;
            try
            {
                data = dir.OpenInput(dataName, state.Context);
                CodecUtil.CheckHeader(data, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_START, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_CURRENT);
                index = dir.OpenInput(indexName, state.Context);
                CodecUtil.CheckHeader(index, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_START, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_CURRENT);

                int fixedLength = data.ReadInt();
                int valueCount = index.ReadInt();
                PagedBytes bytes = new PagedBytes(16);
                bytes.Copy(data, fixedLength * (long)valueCount);
                PagedBytes.Reader bytesReader = bytes.Freeze(true);
                PackedInts.Reader reader = PackedInts.GetReader(index);
                CodecUtil.CheckEOF(data);
                CodecUtil.CheckEOF(index);
                ramBytesUsed.AddAndGet(bytes.RamBytesUsed() + reader.RamBytesUsed());
                success = true;
                return new BinaryDocValuesAnonymousInnerClassHelper3(fixedLength, bytesReader, reader);
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(data, index);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(data, index);
                }
            }
        }

        private class BinaryDocValuesAnonymousInnerClassHelper3 : BinaryDocValues
        {
            private readonly int fixedLength;
            private readonly PagedBytes.Reader bytesReader;
            private readonly PackedInts.Reader reader;

            public BinaryDocValuesAnonymousInnerClassHelper3(int fixedLength, PagedBytes.Reader bytesReader, PackedInts.Reader reader)
            {
                this.fixedLength = fixedLength;
                this.bytesReader = bytesReader;
                this.reader = reader;
            }

            public override void Get(int docID, BytesRef result)
            {
                long offset = fixedLength * reader.Get(docID);
                bytesReader.FillSlice(result, offset, fixedLength);
            }
        }

        private BinaryDocValues LoadBytesVarDeref(FieldInfo field)
        {
            string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number), segmentSuffix, "dat");
            string indexName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number), segmentSuffix, "idx");
            IndexInput data = null;
            IndexInput index = null;
            bool success = false;
            try
            {
                data = dir.OpenInput(dataName, state.Context);
                CodecUtil.CheckHeader(data, Lucene40DocValuesFormat.BYTES_VAR_DEREF_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_START, Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_CURRENT);
                index = dir.OpenInput(indexName, state.Context);
                CodecUtil.CheckHeader(index, Lucene40DocValuesFormat.BYTES_VAR_DEREF_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_START, Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_CURRENT);

                long totalBytes = index.ReadLong();
                PagedBytes bytes = new PagedBytes(16);
                bytes.Copy(data, totalBytes);
                PagedBytes.Reader bytesReader = bytes.Freeze(true);
                PackedInts.Reader reader = PackedInts.GetReader(index);
                CodecUtil.CheckEOF(data);
                CodecUtil.CheckEOF(index);
                ramBytesUsed.AddAndGet(bytes.RamBytesUsed() + reader.RamBytesUsed());
                success = true;
                return new BinaryDocValuesAnonymousInnerClassHelper4(bytesReader, reader);
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(data, index);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(data, index);
                }
            }
        }

        private class BinaryDocValuesAnonymousInnerClassHelper4 : BinaryDocValues
        {
            private readonly PagedBytes.Reader bytesReader;
            private readonly PackedInts.Reader reader;

            public BinaryDocValuesAnonymousInnerClassHelper4(PagedBytes.Reader bytesReader, PackedInts.Reader reader)
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
            lock (this)
            {
                SortedDocValues instance;
                if (!sortedInstances.TryGetValue(field.Number, out instance))
                {
                    string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number), segmentSuffix, "dat");
                    string indexName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name + "_" + Convert.ToString(field.Number), segmentSuffix, "idx");
                    IndexInput data = null;
                    IndexInput index = null;
                    bool success = false;
                    try
                    {
                        data = dir.OpenInput(dataName, state.Context);
                        index = dir.OpenInput(indexName, state.Context);

                        var type = LegacyDocValuesType.ValueOf(field.GetAttribute(legacyKey));

                        if (type == LegacyDocValuesType.BYTES_FIXED_SORTED)
                        {
                            instance = LoadBytesFixedSorted(field, data, index);
                        }
                        else if (type == LegacyDocValuesType.BYTES_VAR_SORTED)
                        {
                            instance = LoadBytesVarSorted(field, data, index);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }

                        CodecUtil.CheckEOF(data);
                        CodecUtil.CheckEOF(index);
                        success = true;
                    }
                    finally
                    {
                        if (success)
                        {
                            IOUtils.Close(data, index);
                        }
                        else
                        {
                            IOUtils.CloseWhileHandlingException(data, index);
                        }
                    }
                    sortedInstances[field.Number] = instance;
                }
                return instance;
            }
        }

        private SortedDocValues LoadBytesFixedSorted(FieldInfo field, IndexInput data, IndexInput index)
        {
            CodecUtil.CheckHeader(data, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_VERSION_START, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_VERSION_CURRENT);
            CodecUtil.CheckHeader(index, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_VERSION_START, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_VERSION_CURRENT);

            int fixedLength = data.ReadInt();
            int valueCount = index.ReadInt();

            PagedBytes bytes = new PagedBytes(16);
            bytes.Copy(data, fixedLength * (long)valueCount);
            PagedBytes.Reader bytesReader = bytes.Freeze(true);
            PackedInts.Reader reader = PackedInts.GetReader(index);
            ramBytesUsed.AddAndGet(bytes.RamBytesUsed() + reader.RamBytesUsed());

            return CorrectBuggyOrds(new SortedDocValuesAnonymousInnerClassHelper(fixedLength, valueCount, bytesReader, reader));
        }

        private class SortedDocValuesAnonymousInnerClassHelper : SortedDocValues
        {
            private readonly int fixedLength;
            private readonly int valueCount;
            private readonly PagedBytes.Reader bytesReader;
            private readonly PackedInts.Reader reader;

            public SortedDocValuesAnonymousInnerClassHelper(int fixedLength, int valueCount, PagedBytes.Reader bytesReader, PackedInts.Reader reader)
            {
                this.fixedLength = fixedLength;
                this.valueCount = valueCount;
                this.bytesReader = bytesReader;
                this.reader = reader;
            }

            public override int GetOrd(int docID)
            {
                return (int)reader.Get(docID);
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                bytesReader.FillSlice(result, fixedLength * (long)ord, fixedLength);
            }

            public override int ValueCount
            {
                get
                {
                    return valueCount;
                }
            }
        }

        private SortedDocValues LoadBytesVarSorted(FieldInfo field, IndexInput data, IndexInput index)
        {
            CodecUtil.CheckHeader(data, Lucene40DocValuesFormat.BYTES_VAR_SORTED_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_VAR_SORTED_VERSION_START, Lucene40DocValuesFormat.BYTES_VAR_SORTED_VERSION_CURRENT);
            CodecUtil.CheckHeader(index, Lucene40DocValuesFormat.BYTES_VAR_SORTED_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_VAR_SORTED_VERSION_START, Lucene40DocValuesFormat.BYTES_VAR_SORTED_VERSION_CURRENT);

            long maxAddress = index.ReadLong();
            PagedBytes bytes = new PagedBytes(16);
            bytes.Copy(data, maxAddress);
            PagedBytes.Reader bytesReader = bytes.Freeze(true);
            PackedInts.Reader addressReader = PackedInts.GetReader(index);
            PackedInts.Reader ordsReader = PackedInts.GetReader(index);

            int valueCount = addressReader.Size - 1;
            ramBytesUsed.AddAndGet(bytes.RamBytesUsed() + addressReader.RamBytesUsed() + ordsReader.RamBytesUsed());

            return CorrectBuggyOrds(new SortedDocValuesAnonymousInnerClassHelper2(bytesReader, addressReader, ordsReader, valueCount));
        }

        private class SortedDocValuesAnonymousInnerClassHelper2 : SortedDocValues
        {
            private readonly PagedBytes.Reader bytesReader;
            private readonly PackedInts.Reader addressReader;
            private readonly PackedInts.Reader ordsReader;
            private readonly int valueCount;

            public SortedDocValuesAnonymousInnerClassHelper2(PagedBytes.Reader bytesReader, PackedInts.Reader addressReader, PackedInts.Reader ordsReader, int valueCount)
            {
                this.bytesReader = bytesReader;
                this.addressReader = addressReader;
                this.ordsReader = ordsReader;
                this.valueCount = valueCount;
            }

            public override int GetOrd(int docID)
            {
                return (int)ordsReader.Get(docID);
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                long startAddress = addressReader.Get(ord);
                long endAddress = addressReader.Get(ord + 1);
                bytesReader.FillSlice(result, startAddress, (int)(endAddress - startAddress));
            }

            public override int ValueCount
            {
                get
                {
                    return valueCount;
                }
            }
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
            return new SortedDocValuesAnonymousInnerClassHelper3(@in);
        }

        private class SortedDocValuesAnonymousInnerClassHelper3 : SortedDocValues
        {
            private readonly SortedDocValues @in;

            public SortedDocValuesAnonymousInnerClassHelper3(SortedDocValues @in)
            {
                this.@in = @in;
            }

            public override int GetOrd(int docID)
            {
                return @in.GetOrd(docID) - 1;
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                @in.LookupOrd(ord + 1, result);
            }

            public override int ValueCount
            {
                get
                {
                    return @in.ValueCount - 1;
                }
            }
        }

        public override SortedSetDocValues GetSortedSet(FieldInfo field)
        {
            throw new InvalidOperationException("Lucene 4.0 does not support SortedSet: how did you pull this off?");
        }

        public override IBits GetDocsWithField(FieldInfo field)
        {
            return new Lucene.Net.Util.Bits.MatchAllBits(state.SegmentInfo.DocCount);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                dir.Dispose();
            }
        }

        public override long RamBytesUsed()
        {
            return ramBytesUsed.Get();
        }

        public override void CheckIntegrity()
        {
        }
    }
}