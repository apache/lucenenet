using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene42
{
    internal class Lucene42DocValuesProducer : DocValuesProducer
    {
        // metadata maps (just file pointers and minimal stuff)
        private readonly IDictionary<int, NumericEntry> numerics;
        private readonly IDictionary<int, BinaryEntry> binaries;
        private readonly IDictionary<int, FSTEntry> fsts;
        private readonly IndexInput data;

        // ram instances we have already loaded
        private readonly IDictionary<int, NumericDocValues> numericInstances =
            new HashMap<int, NumericDocValues>();
        private readonly IDictionary<int, BinaryDocValues> binaryInstances =
            new HashMap<int, BinaryDocValues>();
        private readonly IDictionary<int, FST<long>> fstInstances =
            new HashMap<int, FST<long>>();

        private readonly int maxDoc;

        internal Lucene42DocValuesProducer(SegmentReadState state, String dataCodec, String dataExtension, String metaCodec, String metaExtension)
        {
            maxDoc = state.segmentInfo.DocCount;
            String metaName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix, metaExtension);
            // read in the entries from the metadata file.
            IndexInput input = state.directory.OpenInput(metaName, state.context);
            bool success = false;
            try
            {
                CodecUtil.CheckHeader(input, metaCodec,
                                          Lucene42DocValuesConsumer.VERSION_START,
                                          Lucene42DocValuesConsumer.VERSION_START);
                numerics = new HashMap<int, NumericEntry>();
                binaries = new HashMap<int, BinaryEntry>();
                fsts = new HashMap<int, FSTEntry>();
                ReadFields(input, state.fieldInfos);
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
                    IOUtils.CloseWhileHandlingException((IDisposable)input);
                }
            }

            String dataName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix, dataExtension);
            data = state.directory.OpenInput(dataName, state.context);
            CodecUtil.CheckHeader(data, dataCodec,
                                        Lucene42DocValuesConsumer.VERSION_START,
                                        Lucene42DocValuesConsumer.VERSION_START);
        }

        private void ReadFields(IndexInput meta, FieldInfos infos)
        {
            int fieldNumber = meta.ReadVInt();
            // TODO: .NET Port: I had to add the != 255 case here for it to work in testing, but that means 
            // you can't have more than 255 fields, which seems wrong to me.
            while (fieldNumber != -1 && fieldNumber != 255)
            {
                int fieldType = meta.ReadByte();
                if (fieldType == Lucene42DocValuesConsumer.NUMBER)
                {
                    NumericEntry entry = new NumericEntry();
                    entry.offset = meta.ReadLong();
                    entry.format = (sbyte)meta.ReadByte();
                    if (entry.format != Lucene42DocValuesConsumer.UNCOMPRESSED)
                    {
                        entry.packedIntsVersion = meta.ReadVInt();
                    }
                    numerics[fieldNumber] = entry;
                }
                else if (fieldType == Lucene42DocValuesConsumer.BYTES)
                {
                    BinaryEntry entry = new BinaryEntry();
                    entry.offset = meta.ReadLong();
                    entry.numBytes = meta.ReadLong();
                    entry.minLength = meta.ReadVInt();
                    entry.maxLength = meta.ReadVInt();
                    if (entry.minLength != entry.maxLength)
                    {
                        entry.packedIntsVersion = meta.ReadVInt();
                        entry.blockSize = meta.ReadVInt();
                    }
                    binaries[fieldNumber] = entry;
                }
                else if (fieldType == Lucene42DocValuesConsumer.FST)
                {
                    FSTEntry entry = new FSTEntry();
                    entry.offset = meta.ReadLong();
                    entry.numOrds = meta.ReadVLong();
                    fsts[fieldNumber] = entry;
                }
                else
                {
                    throw new CorruptIndexException("invalid entry type: " + fieldType + ", input=" + meta);
                }
                fieldNumber = meta.ReadVInt();
            }
        }

        public override NumericDocValues GetNumeric(FieldInfo field)
        {
            NumericDocValues instance = numericInstances[field.number];
            if (instance == null)
            {
                instance = LoadNumeric(field);
                numericInstances[field.number] = instance;
            }
            return instance;
        }

        private NumericDocValues LoadNumeric(FieldInfo field)
        {
            NumericEntry entry = numerics[field.number];
            data.Seek(entry.offset);
            if (entry.format == Lucene42DocValuesConsumer.TABLE_COMPRESSED)
            {
                int size = data.ReadVInt();
                long[] decode = new long[size];
                for (int i = 0; i < decode.Length; i++)
                {
                    decode[i] = data.ReadLong();
                }
                int formatID = data.ReadVInt();
                int bitsPerValue = data.ReadVInt();
                PackedInts.IReader reader = PackedInts.GetReaderNoHeader(data, PackedInts.Format.ById(formatID), entry.packedIntsVersion, maxDoc, bitsPerValue);

                return new AnonymousTableCompressedNumericDocValues(decode, reader);
            }
            else if (entry.format == Lucene42DocValuesConsumer.DELTA_COMPRESSED)
            {
                int blockSize = data.ReadVInt();
                BlockPackedReader reader = new BlockPackedReader(data, entry.packedIntsVersion, blockSize, maxDoc, false);

                return new AnonymousDeltaCompressedNumericDocValues(reader);
            }
            else if (entry.format == Lucene42DocValuesConsumer.UNCOMPRESSED)
            {
                byte[] bytes = new byte[maxDoc];
                data.ReadBytes(bytes, 0, bytes.Length);

                return new AnonymousUncompressedNumericDocValues(bytes);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private sealed class AnonymousTableCompressedNumericDocValues : NumericDocValues
        {
            private readonly long[] decode;
            private readonly PackedInts.IReader reader;

            public AnonymousTableCompressedNumericDocValues(long[] decode, PackedInts.IReader reader)
            {
                this.decode = decode;
                this.reader = reader;
            }

            public override long Get(int docID)
            {
                return decode[(int)reader.Get(docID)];
            }
        }

        private sealed class AnonymousDeltaCompressedNumericDocValues : NumericDocValues
        {
            private readonly BlockPackedReader reader;

            public AnonymousDeltaCompressedNumericDocValues(BlockPackedReader reader)
            {
                this.reader = reader;
            }

            public override long Get(int docID)
            {
                return reader.Get(docID);
            }
        }

        private sealed class AnonymousUncompressedNumericDocValues : NumericDocValues
        {
            private readonly byte[] bytes;

            public AnonymousUncompressedNumericDocValues(byte[] bytes)
            {
                this.bytes = bytes;
            }

            public override long Get(int docID)
            {
                return bytes[docID];
            }
        }

        public override BinaryDocValues GetBinary(FieldInfo field)
        {
            BinaryDocValues instance = binaryInstances[field.number];
            if (instance == null)
            {
                instance = LoadBinary(field);
                binaryInstances[field.number] = instance;
            }
            return instance;
        }

        private BinaryDocValues LoadBinary(FieldInfo field)
        {
            BinaryEntry entry = binaries[field.number];
            data.Seek(entry.offset);
            PagedBytes bytes = new PagedBytes(16);
            bytes.Copy(data, entry.numBytes);
            PagedBytes.Reader bytesReader = bytes.Freeze(true);
            if (entry.minLength == entry.maxLength)
            {
                int fixedLength = entry.minLength;
                return new AnonymousDelegatedBinaryDocValues((docID, result) =>
                {
                    bytesReader.FillSlice(result, fixedLength * (long)docID, fixedLength);
                });
            }
            else
            {
                MonotonicBlockPackedReader addresses = new MonotonicBlockPackedReader(data, entry.packedIntsVersion, entry.blockSize, maxDoc, false);
                return new AnonymousDelegatedBinaryDocValues((docID, result) =>
                {
                    long startAddress = docID == 0 ? 0 : addresses.Get(docID - 1);
                    long endAddress = addresses.Get(docID);
                    bytesReader.FillSlice(result, startAddress, (int)(endAddress - startAddress));
                });
            }
        }

        private sealed class AnonymousDelegatedBinaryDocValues : BinaryDocValues
        {
            private readonly Action<int, BytesRef> delegated;

            public AnonymousDelegatedBinaryDocValues(Action<int, BytesRef> delegated)
            {
                this.delegated = delegated;
            }

            public override void Get(int docID, BytesRef result)
            {
                delegated(docID, result);
            }
        }

        public override SortedDocValues GetSorted(FieldInfo field)
        {
            FSTEntry entry = fsts[field.number];
            FST<long> instance;
            lock (this)
            {
                instance = fstInstances[field.number];
                if (instance == null)
                {
                    data.Seek(entry.offset);
                    instance = new FST<long>(data, PositiveIntOutputs.GetSingleton(true));
                    fstInstances[field.number] = instance;
                }
            }
            NumericDocValues docToOrd = GetNumeric(field);
            FST<long> fst = instance;

            // per-thread resources
            FST.BytesReader input = fst.GetBytesReader();
            FST.Arc<long> firstArc = new FST.Arc<long>();
            FST.Arc<long> scratchArc = new FST.Arc<long>();
            IntsRef scratchInts = new IntsRef();
            BytesRefFSTEnum<long> fstEnum = new BytesRefFSTEnum<long>(fst);

            return new AnonymousSortedDocValues(docToOrd, input, fst, firstArc, scratchArc, scratchInts, fstEnum, entry);
        }

        private sealed class AnonymousSortedDocValues : SortedDocValues
        {
            private readonly NumericDocValues docToOrd;
            private readonly FST.BytesReader input;
            private readonly FST<long> fst;
            private readonly FST.Arc<long> firstArc;
            private readonly FST.Arc<long> scratchArc;
            private readonly IntsRef scratchInts;
            private readonly BytesRefFSTEnum<long> fstEnum;
            private readonly FSTEntry entry;

            public AnonymousSortedDocValues(NumericDocValues docToOrd, FST.BytesReader input, FST<long> fst,
                FST.Arc<long> firstArc, FST.Arc<long> scratchArc, IntsRef scratchInts, BytesRefFSTEnum<long> fstEnum,
                FSTEntry entry)
            {
                this.docToOrd = docToOrd;
                this.input = input;
                this.fst = fst;
                this.firstArc = firstArc;
                this.scratchArc = scratchArc;
                this.scratchInts = scratchInts;
                this.fstEnum = fstEnum;
                this.entry = entry;
            }

            public override int GetOrd(int docID)
            {
                return (int)docToOrd.Get(docID);
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                try
                {
                    input.Position = 0;
                    fst.GetFirstArc(firstArc);
                    IntsRef output = Lucene.Net.Util.Fst.Util.GetByOutput(fst, ord, input, firstArc, scratchArc, scratchInts);
                    result.bytes = new sbyte[output.length];
                    result.offset = 0;
                    result.length = 0;
                    Lucene.Net.Util.Fst.Util.ToBytesRef(output, result);
                }
                catch (System.IO.IOException)
                {
                    throw;
                }
            }

            public override int LookupTerm(BytesRef key)
            {
                try
                {
                    BytesRefFSTEnum<long>.InputOutput<long> o = fstEnum.SeekCeil(key);
                    if (o == null)
                    {
                        return -ValueCount - 1;
                    }
                    else if (o.Input.Equals(key))
                    {
                        return (int)o.Output;
                    }
                    else
                    {
                        return (int)-o.Output - 1;
                    }
                }
                catch (System.IO.IOException)
                {
                    throw;
                }
            }

            public override int ValueCount
            {
                get { return (int)entry.numOrds; }
            }

            public override TermsEnum TermsEnum
            {
                get
                {
                    return new FSTTermsEnum(fst);
                }
            }
        }

        public override SortedSetDocValues GetSortedSet(FieldInfo field)
        {
            FSTEntry entry = fsts[field.number];
            if (entry.numOrds == 0)
            {
                return SortedSetDocValues.EMPTY; // empty FST!
            }
            FST<long> instance;
            lock (this)
            {
                instance = fstInstances[field.number];
                if (instance == null)
                {
                    data.Seek(entry.offset);
                    instance = new FST<long>(data, PositiveIntOutputs.GetSingleton(true));
                    fstInstances[field.number] = instance;
                }
            }
            BinaryDocValues docToOrds = GetBinary(field);
            FST<long> fst = instance;

            // per-thread resources
            FST.BytesReader in2 = fst.GetBytesReader();
            FST.Arc<long> firstArc = new FST.Arc<long>();
            FST.Arc<long> scratchArc = new FST.Arc<long>();
            IntsRef scratchInts = new IntsRef();
            BytesRefFSTEnum<long> fstEnum = new BytesRefFSTEnum<long>(fst);
            BytesRef bytesref = new BytesRef();
            ByteArrayDataInput input = new ByteArrayDataInput();

            return new AnonymousSortedSetDocValues(docToOrds, fst, in2, firstArc, scratchArc, scratchInts, fstEnum, bytesref, input, entry);
        }

        private sealed class AnonymousSortedSetDocValues : SortedSetDocValues
        {
            private readonly BinaryDocValues docToOrds;
            private readonly FST<long> fst;
            private readonly FST.BytesReader in2;
            private readonly FST.Arc<long> firstArc;
            private readonly FST.Arc<long> scratchArc;
            private readonly IntsRef scratchInts;
            private readonly BytesRefFSTEnum<long> fstEnum;
            private readonly BytesRef bytesref;
            private readonly ByteArrayDataInput input;
            private readonly FSTEntry entry;
            private long currentOrd;

            public AnonymousSortedSetDocValues(BinaryDocValues docToOrds, FST<long> fst, FST.BytesReader in2, FST.Arc<long> firstArc,
                FST.Arc<long> scratchArc, IntsRef scratchInts, BytesRefFSTEnum<long> fstEnum, BytesRef bytesref, ByteArrayDataInput input, FSTEntry entry)
            {
                this.docToOrds = docToOrds;
                this.fst = fst;
                this.in2 = in2;
                this.firstArc = firstArc;
                this.scratchArc = scratchArc;
                this.scratchInts = scratchInts;
                this.fstEnum = fstEnum;
                this.bytesref = bytesref;
                this.input = input;
                this.entry = entry;
            }

            public override long NextOrd()
            {
                if (input.EOF)
                {
                    return NO_MORE_ORDS;
                }
                else
                {
                    currentOrd += input.ReadVLong();
                    return currentOrd;
                }
            }

            public override void SetDocument(int docID)
            {
                docToOrds.Get(docID, bytesref);
                input.Reset((byte[])(Array)bytesref.bytes, bytesref.offset, bytesref.length);
                currentOrd = 0;
            }

            public override void LookupOrd(long ord, BytesRef result)
            {
                try
                {
                    in2.Position = 0;
                    fst.GetFirstArc(firstArc);
                    IntsRef output = Lucene.Net.Util.Fst.Util.GetByOutput(fst, ord, in2, firstArc, scratchArc, scratchInts);
                    result.bytes = new sbyte[output.length];
                    result.offset = 0;
                    result.length = 0;
                    Lucene.Net.Util.Fst.Util.ToBytesRef(output, result);
                }
                catch (System.IO.IOException)
                {
                    throw;
                }
            }

            public override long LookupTerm(BytesRef key)
            {
                try
                {
                    BytesRefFSTEnum<long>.InputOutput<long> o = fstEnum.SeekCeil(key);
                    if (o == null)
                    {
                        return -ValueCount - 1;
                    }
                    else if (o.Input.Equals(key))
                    {
                        return (int)o.Output;
                    }
                    else
                    {
                        return -o.Output - 1;
                    }
                }
                catch (System.IO.IOException)
                {
                    throw;
                }
            }

            public override long ValueCount
            {
                get { return entry.numOrds; }
            }

            public override TermsEnum TermsEnum
            {
                get
                {
                    return new FSTTermsEnum(fst);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                data.Dispose();
            }
        }

        internal class NumericEntry
        {
            internal long offset;
            internal sbyte format;
            internal int packedIntsVersion;
        }

        internal class BinaryEntry
        {
            internal long offset;
            internal long numBytes;
            internal int minLength;
            internal int maxLength;
            internal int packedIntsVersion;
            internal int blockSize;
        }

        internal class FSTEntry
        {
            internal long offset;
            internal long numOrds;
        }

        private class FSTTermsEnum : TermsEnum
        {
            internal readonly BytesRefFSTEnum<long> in2;

            // this is all for the complicated seek(ord)...
            // maybe we should add a FSTEnum that supports this operation?
            internal readonly FST<long> fst;
            internal readonly FST.BytesReader bytesReader;
            internal readonly FST.Arc<long> firstArc = new FST.Arc<long>();
            internal readonly FST.Arc<long> scratchArc = new FST.Arc<long>();
            internal readonly IntsRef scratchInts = new IntsRef();
            internal readonly BytesRef scratchBytes = new BytesRef();

            internal FSTTermsEnum(FST<long> fst)
            {
                this.fst = fst;
                in2 = new BytesRefFSTEnum<long>(fst);
                bytesReader = fst.GetBytesReader();
            }

            public override BytesRef Next()
            {
                BytesRefFSTEnum<long>.InputOutput<long> io = in2.Next();
                if (io == null)
                {
                    return null;
                }
                else
                {
                    return io.Input;
                }
            }

            public override IComparer<BytesRef> Comparator
            {
                get { return BytesRef.UTF8SortedAsUnicodeComparer; }
            }

            public override SeekStatus SeekCeil(BytesRef text, bool useCache)
            {
                if (in2.SeekCeil(text) == null)
                {
                    return SeekStatus.END;
                }
                else if (Term.Equals(text))
                {
                    // TODO: add SeekStatus to FSTEnum like in https://issues.apache.org/jira/browse/LUCENE-3729
                    // to remove this comparision?
                    return SeekStatus.FOUND;
                }
                else
                {
                    return SeekStatus.NOT_FOUND;
                }
            }

            public override bool SeekExact(BytesRef text, bool useCache)
            {
                if (in2.SeekExact(text) == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            public override void SeekExact(long ord)
            {
                // TODO: would be better to make this simpler and faster.
                // but we dont want to introduce a bug that corrupts our enum state!
                bytesReader.Position = 0;
                fst.GetFirstArc(firstArc);
                IntsRef output = Lucene.Net.Util.Fst.Util.GetByOutput(fst, ord, bytesReader, firstArc, scratchArc, scratchInts);
                scratchBytes.bytes = new sbyte[output.length];
                scratchBytes.offset = 0;
                scratchBytes.length = 0;
                Lucene.Net.Util.Fst.Util.ToBytesRef(output, scratchBytes);
                // TODO: we could do this lazily, better to try to push into FSTEnum though?
                in2.SeekExact(scratchBytes);
            }

            public override BytesRef Term
            {
                get { return in2.Current().Input; }
            }

            public override long Ord
            {
                get { return in2.Current().Output; }
            }

            public override int DocFreq
            {
                get { throw new NotSupportedException(); }
            }

            public override long TotalTermFreq
            {
                get { throw new NotSupportedException(); }
            }

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
            {
                throw new NotSupportedException();
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                throw new NotSupportedException();
            }
        }
    }
}
