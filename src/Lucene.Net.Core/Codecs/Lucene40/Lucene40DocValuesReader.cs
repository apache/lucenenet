using System;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene40
{
    using Lucene.Net.Support;
    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;
    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
    using Directory = Lucene.Net.Store.Directory;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IOUtils = Lucene.Net.Util.IOUtils;

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
        private readonly Directory Dir;
        private readonly SegmentReadState State;
        private readonly string LegacyKey;
        private const string SegmentSuffix = "dv";

        // ram instances we have already loaded
        private readonly IDictionary<int, NumericDocValues> NumericInstances = new Dictionary<int, NumericDocValues>();

        private readonly IDictionary<int, BinaryDocValues> BinaryInstances = new Dictionary<int, BinaryDocValues>();
        private readonly IDictionary<int, SortedDocValues> SortedInstances = new Dictionary<int, SortedDocValues>();

        private readonly AtomicLong RamBytesUsed_Renamed;

        internal Lucene40DocValuesReader(SegmentReadState state, string filename, string legacyKey)
        {
            this.State = state;
            this.LegacyKey = legacyKey;
            this.Dir = new CompoundFileDirectory(state.Directory, filename, state.Context, false);
            RamBytesUsed_Renamed = new AtomicLong(RamUsageEstimator.ShallowSizeOf(this.GetType()));
        }

        public override NumericDocValues GetNumeric(FieldInfo field)
        {
            lock (this)
            {
                NumericDocValues instance;
                if (!NumericInstances.TryGetValue(field.Number, out instance))
                {
                    string fileName = IndexFileNames.SegmentFileName(State.SegmentInfo.Name + "_" + Convert.ToString(field.Number), SegmentSuffix, "dat");
                    IndexInput input = Dir.OpenInput(fileName, State.Context);
                    bool success = false;
                    try
                    {
                        var type = LegacyDocValuesType.ValueOf(field.GetAttribute(LegacyKey));

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
                    NumericInstances[field.Number] = instance;
                }
                return instance;
            }
        }

        private NumericDocValues LoadVarIntsField(FieldInfo field, IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.VAR_INTS_CODEC_NAME, Lucene40DocValuesFormat.VAR_INTS_VERSION_START, Lucene40DocValuesFormat.VAR_INTS_VERSION_CURRENT);
            byte header = input.ReadByte();
            if (header == Lucene40DocValuesFormat.VAR_INTS_FIXED_64)
            {
                int maxDoc = State.SegmentInfo.DocCount;
                long[] values = new long[maxDoc];
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = input.ReadLong();
                }
                RamBytesUsed_Renamed.AddAndGet(RamUsageEstimator.SizeOf(values));
                return new NumericDocValuesAnonymousInnerClassHelper(this, values);
            }
            else if (header == Lucene40DocValuesFormat.VAR_INTS_PACKED)
            {
                long minValue = input.ReadLong();
                long defaultValue = input.ReadLong();
                PackedInts.Reader reader = PackedInts.GetReader(input);
                RamBytesUsed_Renamed.AddAndGet(reader.RamBytesUsed());
                return new NumericDocValuesAnonymousInnerClassHelper2(this, minValue, defaultValue, reader);
            }
            else
            {
                throw new CorruptIndexException("invalid VAR_INTS header byte: " + header + " (resource=" + input + ")");
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper : NumericDocValues
        {
            private readonly Lucene40DocValuesReader OuterInstance;

            private long[] Values;

            public NumericDocValuesAnonymousInnerClassHelper(Lucene40DocValuesReader outerInstance, long[] values)
            {
                this.OuterInstance = outerInstance;
                this.Values = values;
            }

            public override long Get(int docID)
            {
                return Values[docID];
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper2 : NumericDocValues
        {
            private readonly Lucene40DocValuesReader OuterInstance;

            private long MinValue;
            private long DefaultValue;
            private PackedInts.Reader Reader;

            public NumericDocValuesAnonymousInnerClassHelper2(Lucene40DocValuesReader outerInstance, long minValue, long defaultValue, PackedInts.Reader reader)
            {
                this.OuterInstance = outerInstance;
                this.MinValue = minValue;
                this.DefaultValue = defaultValue;
                this.Reader = reader;
            }

            public override long Get(int docID)
            {
                long value = Reader.Get(docID);
                if (value == DefaultValue)
                {
                    return 0;
                }
                else
                {
                    return MinValue + value;
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
            int maxDoc = State.SegmentInfo.DocCount;
            sbyte[] values = new sbyte[maxDoc];
            input.ReadBytes(values, 0, values.Length);
            RamBytesUsed_Renamed.AddAndGet(RamUsageEstimator.SizeOf(values));
            return new NumericDocValuesAnonymousInnerClassHelper3(this, values);
        }

        private class NumericDocValuesAnonymousInnerClassHelper3 : NumericDocValues
        {
            private readonly Lucene40DocValuesReader OuterInstance;

            private sbyte[] Values;

            public NumericDocValuesAnonymousInnerClassHelper3(Lucene40DocValuesReader outerInstance, sbyte[] values)
            {
                this.OuterInstance = outerInstance;
                this.Values = values;
            }

            public override long Get(int docID)
            {
                return Values[docID];
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
            int maxDoc = State.SegmentInfo.DocCount;
            short[] values = new short[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadShort();
            }
            RamBytesUsed_Renamed.AddAndGet(RamUsageEstimator.SizeOf(values));
            return new NumericDocValuesAnonymousInnerClassHelper4(this, values);
        }

        private class NumericDocValuesAnonymousInnerClassHelper4 : NumericDocValues
        {
            private readonly Lucene40DocValuesReader OuterInstance;

            private short[] Values;

            public NumericDocValuesAnonymousInnerClassHelper4(Lucene40DocValuesReader outerInstance, short[] values)
            {
                this.OuterInstance = outerInstance;
                this.Values = values;
            }

            public override long Get(int docID)
            {
                return Values[docID];
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
            int maxDoc = State.SegmentInfo.DocCount;
            int[] values = new int[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadInt();
            }
            RamBytesUsed_Renamed.AddAndGet(RamUsageEstimator.SizeOf(values));
            return new NumericDocValuesAnonymousInnerClassHelper5(this, values);
        }

        private class NumericDocValuesAnonymousInnerClassHelper5 : NumericDocValues
        {
            private readonly Lucene40DocValuesReader OuterInstance;

            private int[] Values;

            public NumericDocValuesAnonymousInnerClassHelper5(Lucene40DocValuesReader outerInstance, int[] values)
            {
                this.OuterInstance = outerInstance;
                this.Values = values;
            }

            public override long Get(int docID)
            {
                return Values[docID];
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
            int maxDoc = State.SegmentInfo.DocCount;
            long[] values = new long[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadLong();
            }
            RamBytesUsed_Renamed.AddAndGet(RamUsageEstimator.SizeOf(values));
            return new NumericDocValuesAnonymousInnerClassHelper6(this, values);
        }

        private class NumericDocValuesAnonymousInnerClassHelper6 : NumericDocValues
        {
            private readonly Lucene40DocValuesReader OuterInstance;

            private long[] Values;

            public NumericDocValuesAnonymousInnerClassHelper6(Lucene40DocValuesReader outerInstance, long[] values)
            {
                this.OuterInstance = outerInstance;
                this.Values = values;
            }

            public override long Get(int docID)
            {
                return Values[docID];
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
            int maxDoc = State.SegmentInfo.DocCount;
            int[] values = new int[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadInt();
            }
            RamBytesUsed_Renamed.AddAndGet(RamUsageEstimator.SizeOf(values));
            return new NumericDocValuesAnonymousInnerClassHelper7(this, values);
        }

        private class NumericDocValuesAnonymousInnerClassHelper7 : NumericDocValues
        {
            private readonly Lucene40DocValuesReader OuterInstance;

            private int[] Values;

            public NumericDocValuesAnonymousInnerClassHelper7(Lucene40DocValuesReader outerInstance, int[] values)
            {
                this.OuterInstance = outerInstance;
                this.Values = values;
            }

            public override long Get(int docID)
            {
                return Values[docID];
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
            int maxDoc = State.SegmentInfo.DocCount;
            long[] values = new long[maxDoc];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = input.ReadLong();
            }
            RamBytesUsed_Renamed.AddAndGet(RamUsageEstimator.SizeOf(values));
            return new NumericDocValuesAnonymousInnerClassHelper8(this, values);
        }

        private class NumericDocValuesAnonymousInnerClassHelper8 : NumericDocValues
        {
            private readonly Lucene40DocValuesReader OuterInstance;

            private long[] Values;

            public NumericDocValuesAnonymousInnerClassHelper8(Lucene40DocValuesReader outerInstance, long[] values)
            {
                this.OuterInstance = outerInstance;
                this.Values = values;
            }

            public override long Get(int docID)
            {
                return Values[docID];
            }
        }

        public override BinaryDocValues GetBinary(FieldInfo field)
        {
            lock (this)
            {
                BinaryDocValues instance;
                if (!BinaryInstances.TryGetValue(field.Number, out instance))
                {
                    var type = LegacyDocValuesType.ValueOf(field.GetAttribute(LegacyKey));

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
                    BinaryInstances[field.Number] = instance;
                }
                return instance;
            }
        }

        private BinaryDocValues LoadBytesFixedStraight(FieldInfo field)
        {
            string fileName = IndexFileNames.SegmentFileName(State.SegmentInfo.Name + "_" + Convert.ToString(field.Number), SegmentSuffix, "dat");
            IndexInput input = Dir.OpenInput(fileName, State.Context);
            bool success = false;
            try
            {
                CodecUtil.CheckHeader(input, Lucene40DocValuesFormat.BYTES_FIXED_STRAIGHT_CODEC_NAME, Lucene40DocValuesFormat.BYTES_FIXED_STRAIGHT_VERSION_START, Lucene40DocValuesFormat.BYTES_FIXED_STRAIGHT_VERSION_CURRENT);
                int fixedLength = input.ReadInt();
                PagedBytes bytes = new PagedBytes(16);
                bytes.Copy(input, fixedLength * (long)State.SegmentInfo.DocCount);
                PagedBytes.Reader bytesReader = bytes.Freeze(true);
                CodecUtil.CheckEOF(input);
                success = true;
                RamBytesUsed_Renamed.AddAndGet(bytes.RamBytesUsed());
                return new BinaryDocValuesAnonymousInnerClassHelper(this, fixedLength, bytesReader);
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
            private readonly Lucene40DocValuesReader OuterInstance;

            private int FixedLength;
            private PagedBytes.Reader BytesReader;

            public BinaryDocValuesAnonymousInnerClassHelper(Lucene40DocValuesReader outerInstance, int fixedLength, PagedBytes.Reader bytesReader)
            {
                this.OuterInstance = outerInstance;
                this.FixedLength = fixedLength;
                this.BytesReader = bytesReader;
            }

            public override void Get(int docID, BytesRef result)
            {
                BytesReader.FillSlice(result, FixedLength * (long)docID, FixedLength);
            }
        }

        private BinaryDocValues LoadBytesVarStraight(FieldInfo field)
        {
            string dataName = IndexFileNames.SegmentFileName(State.SegmentInfo.Name + "_" + Convert.ToString(field.Number), SegmentSuffix, "dat");
            string indexName = IndexFileNames.SegmentFileName(State.SegmentInfo.Name + "_" + Convert.ToString(field.Number), SegmentSuffix, "idx");
            IndexInput data = null;
            IndexInput index = null;
            bool success = false;
            try
            {
                data = Dir.OpenInput(dataName, State.Context);
                CodecUtil.CheckHeader(data, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_START, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_CURRENT);
                index = Dir.OpenInput(indexName, State.Context);
                CodecUtil.CheckHeader(index, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_START, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_CURRENT);
                long totalBytes = index.ReadVLong();
                PagedBytes bytes = new PagedBytes(16);
                bytes.Copy(data, totalBytes);
                PagedBytes.Reader bytesReader = bytes.Freeze(true);
                PackedInts.Reader reader = PackedInts.GetReader(index);
                CodecUtil.CheckEOF(data);
                CodecUtil.CheckEOF(index);
                success = true;
                RamBytesUsed_Renamed.AddAndGet(bytes.RamBytesUsed() + reader.RamBytesUsed());
                return new BinaryDocValuesAnonymousInnerClassHelper2(this, bytesReader, reader);
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
            private readonly Lucene40DocValuesReader OuterInstance;

            private PagedBytes.Reader BytesReader;
            private PackedInts.Reader Reader;

            public BinaryDocValuesAnonymousInnerClassHelper2(Lucene40DocValuesReader outerInstance, PagedBytes.Reader bytesReader, PackedInts.Reader reader)
            {
                this.OuterInstance = outerInstance;
                this.BytesReader = bytesReader;
                this.Reader = reader;
            }

            public override void Get(int docID, BytesRef result)
            {
                long startAddress = Reader.Get(docID);
                long endAddress = Reader.Get(docID + 1);
                BytesReader.FillSlice(result, startAddress, (int)(endAddress - startAddress));
            }
        }

        private BinaryDocValues LoadBytesFixedDeref(FieldInfo field)
        {
            string dataName = IndexFileNames.SegmentFileName(State.SegmentInfo.Name + "_" + Convert.ToString(field.Number), SegmentSuffix, "dat");
            string indexName = IndexFileNames.SegmentFileName(State.SegmentInfo.Name + "_" + Convert.ToString(field.Number), SegmentSuffix, "idx");
            IndexInput data = null;
            IndexInput index = null;
            bool success = false;
            try
            {
                data = Dir.OpenInput(dataName, State.Context);
                CodecUtil.CheckHeader(data, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_START, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_CURRENT);
                index = Dir.OpenInput(indexName, State.Context);
                CodecUtil.CheckHeader(index, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_START, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_CURRENT);

                int fixedLength = data.ReadInt();
                int valueCount = index.ReadInt();
                PagedBytes bytes = new PagedBytes(16);
                bytes.Copy(data, fixedLength * (long)valueCount);
                PagedBytes.Reader bytesReader = bytes.Freeze(true);
                PackedInts.Reader reader = PackedInts.GetReader(index);
                CodecUtil.CheckEOF(data);
                CodecUtil.CheckEOF(index);
                RamBytesUsed_Renamed.AddAndGet(bytes.RamBytesUsed() + reader.RamBytesUsed());
                success = true;
                return new BinaryDocValuesAnonymousInnerClassHelper3(this, fixedLength, bytesReader, reader);
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
            private readonly Lucene40DocValuesReader OuterInstance;

            private int FixedLength;
            private PagedBytes.Reader BytesReader;
            private PackedInts.Reader Reader;

            public BinaryDocValuesAnonymousInnerClassHelper3(Lucene40DocValuesReader outerInstance, int fixedLength, PagedBytes.Reader bytesReader, PackedInts.Reader reader)
            {
                this.OuterInstance = outerInstance;
                this.FixedLength = fixedLength;
                this.BytesReader = bytesReader;
                this.Reader = reader;
            }

            public override void Get(int docID, BytesRef result)
            {
                long offset = FixedLength * Reader.Get(docID);
                BytesReader.FillSlice(result, offset, FixedLength);
            }
        }

        private BinaryDocValues LoadBytesVarDeref(FieldInfo field)
        {
            string dataName = IndexFileNames.SegmentFileName(State.SegmentInfo.Name + "_" + Convert.ToString(field.Number), SegmentSuffix, "dat");
            string indexName = IndexFileNames.SegmentFileName(State.SegmentInfo.Name + "_" + Convert.ToString(field.Number), SegmentSuffix, "idx");
            IndexInput data = null;
            IndexInput index = null;
            bool success = false;
            try
            {
                data = Dir.OpenInput(dataName, State.Context);
                CodecUtil.CheckHeader(data, Lucene40DocValuesFormat.BYTES_VAR_DEREF_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_START, Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_CURRENT);
                index = Dir.OpenInput(indexName, State.Context);
                CodecUtil.CheckHeader(index, Lucene40DocValuesFormat.BYTES_VAR_DEREF_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_START, Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_CURRENT);

                long totalBytes = index.ReadLong();
                PagedBytes bytes = new PagedBytes(16);
                bytes.Copy(data, totalBytes);
                PagedBytes.Reader bytesReader = bytes.Freeze(true);
                PackedInts.Reader reader = PackedInts.GetReader(index);
                CodecUtil.CheckEOF(data);
                CodecUtil.CheckEOF(index);
                RamBytesUsed_Renamed.AddAndGet(bytes.RamBytesUsed() + reader.RamBytesUsed());
                success = true;
                return new BinaryDocValuesAnonymousInnerClassHelper4(this, bytesReader, reader);
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
            private readonly Lucene40DocValuesReader OuterInstance;

            private PagedBytes.Reader BytesReader;
            private PackedInts.Reader Reader;

            public BinaryDocValuesAnonymousInnerClassHelper4(Lucene40DocValuesReader outerInstance, PagedBytes.Reader bytesReader, PackedInts.Reader reader)
            {
                this.OuterInstance = outerInstance;
                this.BytesReader = bytesReader;
                this.Reader = reader;
            }

            public override void Get(int docID, BytesRef result)
            {
                long startAddress = Reader.Get(docID);
                BytesRef lengthBytes = new BytesRef();
                BytesReader.FillSlice(lengthBytes, startAddress, 1);
                sbyte code = lengthBytes.Bytes[lengthBytes.Offset];
                if ((code & 128) == 0)
                {
                    // length is 1 byte
                    BytesReader.FillSlice(result, startAddress + 1, (int)code);
                }
                else
                {
                    BytesReader.FillSlice(lengthBytes, startAddress + 1, 1);
                    int length = ((code & 0x7f) << 8) | (lengthBytes.Bytes[lengthBytes.Offset] & 0xff);
                    BytesReader.FillSlice(result, startAddress + 2, length);
                }
            }
        }

        public override SortedDocValues GetSorted(FieldInfo field)
        {
            lock (this)
            {
                SortedDocValues instance;
                if (!SortedInstances.TryGetValue(field.Number, out instance))
                {
                    string dataName = IndexFileNames.SegmentFileName(State.SegmentInfo.Name + "_" + Convert.ToString(field.Number), SegmentSuffix, "dat");
                    string indexName = IndexFileNames.SegmentFileName(State.SegmentInfo.Name + "_" + Convert.ToString(field.Number), SegmentSuffix, "idx");
                    IndexInput data = null;
                    IndexInput index = null;
                    bool success = false;
                    try
                    {
                        data = Dir.OpenInput(dataName, State.Context);
                        index = Dir.OpenInput(indexName, State.Context);

                        var type = LegacyDocValuesType.ValueOf(field.GetAttribute(LegacyKey));

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
                    SortedInstances[field.Number] = instance;
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
            RamBytesUsed_Renamed.AddAndGet(bytes.RamBytesUsed() + reader.RamBytesUsed());

            return CorrectBuggyOrds(new SortedDocValuesAnonymousInnerClassHelper(this, fixedLength, valueCount, bytesReader, reader));
        }

        private class SortedDocValuesAnonymousInnerClassHelper : SortedDocValues
        {
            private readonly Lucene40DocValuesReader OuterInstance;

            private int FixedLength;
            private int valueCount;
            private PagedBytes.Reader BytesReader;
            private PackedInts.Reader Reader;

            public SortedDocValuesAnonymousInnerClassHelper(Lucene40DocValuesReader outerInstance, int fixedLength, int valueCount, PagedBytes.Reader bytesReader, PackedInts.Reader reader)
            {
                this.OuterInstance = outerInstance;
                this.FixedLength = fixedLength;
                this.valueCount = valueCount;
                this.BytesReader = bytesReader;
                this.Reader = reader;
            }

            public override int GetOrd(int docID)
            {
                return (int)Reader.Get(docID);
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                BytesReader.FillSlice(result, FixedLength * (long)ord, FixedLength);
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

            int valueCount = addressReader.Size() - 1;
            RamBytesUsed_Renamed.AddAndGet(bytes.RamBytesUsed() + addressReader.RamBytesUsed() + ordsReader.RamBytesUsed());

            return CorrectBuggyOrds(new SortedDocValuesAnonymousInnerClassHelper2(this, bytesReader, addressReader, ordsReader, valueCount));
        }

        private class SortedDocValuesAnonymousInnerClassHelper2 : SortedDocValues
        {
            private readonly Lucene40DocValuesReader OuterInstance;

            private PagedBytes.Reader BytesReader;
            private PackedInts.Reader AddressReader;
            private PackedInts.Reader OrdsReader;
            private int valueCount;

            public SortedDocValuesAnonymousInnerClassHelper2(Lucene40DocValuesReader outerInstance, PagedBytes.Reader bytesReader, PackedInts.Reader addressReader, PackedInts.Reader ordsReader, int valueCount)
            {
                this.OuterInstance = outerInstance;
                this.BytesReader = bytesReader;
                this.AddressReader = addressReader;
                this.OrdsReader = ordsReader;
                this.valueCount = valueCount;
            }

            public override int GetOrd(int docID)
            {
                return (int)OrdsReader.Get(docID);
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                long startAddress = AddressReader.Get(ord);
                long endAddress = AddressReader.Get(ord + 1);
                BytesReader.FillSlice(result, startAddress, (int)(endAddress - startAddress));
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
            int maxDoc = State.SegmentInfo.DocCount;
            for (int i = 0; i < maxDoc; i++)
            {
                if (@in.GetOrd(i) == 0)
                {
                    return @in; // ok
                }
            }

            // we had ord holes, return an ord-shifting-impl that corrects the bug
            return new SortedDocValuesAnonymousInnerClassHelper3(this, @in);
        }

        private class SortedDocValuesAnonymousInnerClassHelper3 : SortedDocValues
        {
            private readonly Lucene40DocValuesReader OuterInstance;

            private SortedDocValues @in;

            public SortedDocValuesAnonymousInnerClassHelper3(Lucene40DocValuesReader outerInstance, SortedDocValues @in)
            {
                this.OuterInstance = outerInstance;
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

        public override Bits GetDocsWithField(FieldInfo field)
        {
            return new Lucene.Net.Util.Bits_MatchAllBits(State.SegmentInfo.DocCount);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Dir.Dispose();
            }
        }

        public override long RamBytesUsed()
        {
            return RamBytesUsed_Renamed.Get();
        }

        public override void CheckIntegrity()
        {
        }
    }
}