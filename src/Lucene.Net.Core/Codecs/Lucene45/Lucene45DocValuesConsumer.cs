using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lucene.Net.Codecs.Lucene45
{
    using BlockPackedWriter = Lucene.Net.Util.Packed.BlockPackedWriter;
    using BytesRef = Lucene.Net.Util.BytesRef;

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

    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using MathUtil = Lucene.Net.Util.MathUtil;
    using MonotonicBlockPackedWriter = Lucene.Net.Util.Packed.MonotonicBlockPackedWriter;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using RAMOutputStream = Lucene.Net.Store.RAMOutputStream;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
    using StringHelper = Lucene.Net.Util.StringHelper;

    /// <summary>
    /// writer for <seealso cref="Lucene45DocValuesFormat"/> </summary>
    public class Lucene45DocValuesConsumer : DocValuesConsumer, IDisposable
    {
        internal static readonly int BLOCK_SIZE = 16384;
        internal static readonly int ADDRESS_INTERVAL = 16;
        internal static readonly long MISSING_ORD = BitConverter.DoubleToInt64Bits(-1);

        /// <summary>
        /// Compressed using packed blocks of ints. </summary>
        public const int DELTA_COMPRESSED = 0;

        /// <summary>
        /// Compressed by computing the GCD. </summary>
        public const int GCD_COMPRESSED = 1;

        /// <summary>
        /// Compressed by giving IDs to unique values. </summary>
        public const int TABLE_COMPRESSED = 2;

        /// <summary>
        /// Uncompressed binary, written directly (fixed length). </summary>
        public const int BINARY_FIXED_UNCOMPRESSED = 0;

        /// <summary>
        /// Uncompressed binary, written directly (variable length). </summary>
        public const int BINARY_VARIABLE_UNCOMPRESSED = 1;

        /// <summary>
        /// Compressed binary with shared prefixes </summary>
        public const int BINARY_PREFIX_COMPRESSED = 2;

        /// <summary>
        /// Standard storage for sorted set values with 1 level of indirection:
        ///  docId -> address -> ord.
        /// </summary>
        public static readonly int SORTED_SET_WITH_ADDRESSES = 0;

        /// <summary>
        /// Single-valued sorted set values, encoded as sorted values, so no level
        ///  of indirection: docId -> ord.
        /// </summary>
        public static readonly int SORTED_SET_SINGLE_VALUED_SORTED = 1;

        internal IndexOutput Data, Meta;
        internal readonly int MaxDoc;

        /// <summary>
        /// expert: Creates a new writer </summary>
        public Lucene45DocValuesConsumer(SegmentWriteState state, string dataCodec, string dataExtension, string metaCodec, string metaExtension)
        {
            bool success = false;
            try
            {
                string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, dataExtension);
                Data = state.Directory.CreateOutput(dataName, state.Context);
                CodecUtil.WriteHeader(Data, dataCodec, Lucene45DocValuesFormat.VERSION_CURRENT);
                string metaName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, metaExtension);
                Meta = state.Directory.CreateOutput(metaName, state.Context);
                CodecUtil.WriteHeader(Meta, metaCodec, Lucene45DocValuesFormat.VERSION_CURRENT);
                MaxDoc = state.SegmentInfo.DocCount;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(this);
                }
            }
        }

        public override void AddNumericField(FieldInfo field, IEnumerable<long?> values)
        {
            AddNumericField(field, values, true);
        }

        internal virtual void AddNumericField(FieldInfo field, IEnumerable<long?> values, bool optimizeStorage)
        {
            long count = 0;
            long minValue = long.MaxValue;
            long maxValue = long.MinValue;
            long gcd = 0;
            bool missing = false;
            // TODO: more efficient?
            HashSet<long> uniqueValues = null;
            
            if (optimizeStorage)
            {
                uniqueValues = new HashSet<long>();

                foreach (long? nv in values)
                {
                    long v;
                    if (nv == null)
                    {
                        v = 0;
                        missing = true;
                    }
                    else
                    {
                        v = nv.Value;
                    }

                    if (gcd != 1)
                    {
                        if (v < long.MinValue / 2 || v > long.MaxValue / 2)
                        {
                            // in that case v - minValue might overflow and make the GCD computation return
                            // wrong results. Since these extreme values are unlikely, we just discard
                            // GCD computation for them
                            gcd = 1;
                        } // minValue needs to be set first
                        else if (count != 0)
                        {
                            gcd = MathUtil.Gcd(gcd, v - minValue);
                        }
                    }

                    minValue = Math.Min(minValue, v);
                    maxValue = Math.Max(maxValue, v);

                    if (uniqueValues != null)
                    {
                        if (uniqueValues.Add(v))
                        {
                            if (uniqueValues.Count > 256)
                            {
                                uniqueValues = null;
                            }
                        }
                    }

                    ++count;
                }
            }
            else
            {
                foreach (var nv in values)
                {
                    ++count;
                }
            }

            long delta = maxValue - minValue;

            int format;
            if (uniqueValues != null && (delta < 0L || PackedInts.BitsRequired(uniqueValues.Count - 1) < PackedInts.BitsRequired(delta)) && count <= int.MaxValue)
            {
                format = TABLE_COMPRESSED;
            }
            else if (gcd != 0 && gcd != 1)
            {
                format = GCD_COMPRESSED;
            }
            else
            {
                format = DELTA_COMPRESSED;
            }
            Meta.WriteVInt(field.Number);
            Meta.WriteByte((byte)Lucene45DocValuesFormat.NUMERIC);
            Meta.WriteVInt(format);
            if (missing)
            {
                Meta.WriteLong(Data.FilePointer);
                WriteMissingBitset(values);
            }
            else
            {
                Meta.WriteLong(-1L);
            }
            Meta.WriteVInt(PackedInts.VERSION_CURRENT);
            Meta.WriteLong(Data.FilePointer);
            Meta.WriteVLong(count);
            Meta.WriteVInt(BLOCK_SIZE);

            switch (format)
            {
                case GCD_COMPRESSED:
                    Meta.WriteLong(minValue);
                    Meta.WriteLong(gcd);
                    BlockPackedWriter quotientWriter = new BlockPackedWriter(Data, BLOCK_SIZE);
                    foreach (long? nv in values)
                    {
                        long value = nv == null ? 0 : nv.Value;
                        quotientWriter.Add((value - minValue) / gcd);
                    }
                    quotientWriter.Finish();
                    break;

                case DELTA_COMPRESSED:
                    BlockPackedWriter writer = new BlockPackedWriter(Data, BLOCK_SIZE);
                    foreach (long? nv in values)
                    {
                        writer.Add(nv == null ? 0 : nv.Value);
                    }
                    writer.Finish();
                    break;

                case TABLE_COMPRESSED:
                    long[] decode = uniqueValues.ToArray();//LUCENE TO-DO Hadd oparamerter before
                    Dictionary<long, int> encode = new Dictionary<long, int>();
                    Meta.WriteVInt(decode.Length);
                    for (int i = 0; i < decode.Length; i++)
                    {
                        Meta.WriteLong(decode[i]);
                        encode[decode[i]] = i;
                    }
                    int bitsRequired = PackedInts.BitsRequired(uniqueValues.Count - 1);
                    PackedInts.Writer ordsWriter = PackedInts.GetWriterNoHeader(Data, PackedInts.Format.PACKED, (int)count, bitsRequired, PackedInts.DEFAULT_BUFFER_SIZE);
                    foreach (long? nv in values)
                    {
                        ordsWriter.Add(encode[nv == null ? 0 : nv.Value]);
                    }
                    ordsWriter.Finish();
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        // TODO: in some cases representing missing with minValue-1 wouldn't take up additional space and so on,
        // but this is very simple, and algorithms only check this for values of 0 anyway (doesnt slow down normal decode)
        internal virtual void WriteMissingBitset(IEnumerable values)
        {
            sbyte bits = 0;
            int count = 0;
            foreach (object v in values)
            {
                if (count == 8)
                {
                    Data.WriteByte((byte)bits);
                    count = 0;
                    bits = 0;
                }
                if (v != null)
                {
                    bits |= (sbyte)(1 << (count & 7));
                }
                count++;
            }
            if (count > 0)
            {
                Data.WriteByte((byte)bits);
            }
        }

        public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
        {
            // write the byte[] data
            Meta.WriteVInt(field.Number);
            Meta.WriteByte((byte)Lucene45DocValuesFormat.BINARY);
            int minLength = int.MaxValue;
            int maxLength = int.MinValue;
            long startFP = Data.FilePointer;
            long count = 0;
            bool missing = false;
            foreach (BytesRef v in values)
            {
                int length;
                if (v == null)
                {
                    length = 0;
                    missing = true;
                }
                else
                {
                    length = v.Length;
                }
                minLength = Math.Min(minLength, length);
                maxLength = Math.Max(maxLength, length);
                if (v != null)
                {
                    Data.WriteBytes(v.Bytes, v.Offset, v.Length);
                }
                count++;
            }
            Meta.WriteVInt(minLength == maxLength ? BINARY_FIXED_UNCOMPRESSED : BINARY_VARIABLE_UNCOMPRESSED);
            if (missing)
            {
                Meta.WriteLong(Data.FilePointer);
                WriteMissingBitset(values);
            }
            else
            {
                Meta.WriteLong(-1L);
            }
            Meta.WriteVInt(minLength);
            Meta.WriteVInt(maxLength);
            Meta.WriteVLong(count);
            Meta.WriteLong(startFP);

            // if minLength == maxLength, its a fixed-length byte[], we are done (the addresses are implicit)
            // otherwise, we need to record the length fields...
            if (minLength != maxLength)
            {
                Meta.WriteLong(Data.FilePointer);
                Meta.WriteVInt(PackedInts.VERSION_CURRENT);
                Meta.WriteVInt(BLOCK_SIZE);

                MonotonicBlockPackedWriter writer = new MonotonicBlockPackedWriter(Data, BLOCK_SIZE);
                long addr = 0;
                foreach (BytesRef v in values)
                {
                    if (v != null)
                    {
                        addr += v.Length;
                    }
                    writer.Add(addr);
                }
                writer.Finish();
            }
        }

        /// <summary>
        /// expert: writes a value dictionary for a sorted/sortedset field </summary>
        protected internal virtual void AddTermsDict(FieldInfo field, IEnumerable<BytesRef> values)
        {
            // first check if its a "fixed-length" terms dict
            int minLength = int.MaxValue;
            int maxLength = int.MinValue;
            foreach (BytesRef v in values)
            {
                minLength = Math.Min(minLength, v.Length);
                maxLength = Math.Max(maxLength, v.Length);
            }
            if (minLength == maxLength)
            {
                // no index needed: direct addressing by mult
                AddBinaryField(field, values);
            }
            else
            {
                // header
                Meta.WriteVInt(field.Number);
                Meta.WriteByte((byte)Lucene45DocValuesFormat.BINARY);
                Meta.WriteVInt(BINARY_PREFIX_COMPRESSED);
                Meta.WriteLong(-1L);
                // now write the bytes: sharing prefixes within a block
                long startFP = Data.FilePointer;
                // currently, we have to store the delta from expected for every 1/nth term
                // we could avoid this, but its not much and less overall RAM than the previous approach!
                RAMOutputStream addressBuffer = new RAMOutputStream();
                MonotonicBlockPackedWriter termAddresses = new MonotonicBlockPackedWriter(addressBuffer, BLOCK_SIZE);
                BytesRef lastTerm = new BytesRef();
                long count = 0;
                foreach (BytesRef v in values)
                {
                    if (count % ADDRESS_INTERVAL == 0)
                    {
                        termAddresses.Add(Data.FilePointer - startFP);
                        // force the first term in a block to be abs-encoded
                        lastTerm.Length = 0;
                    }

                    // prefix-code
                    int sharedPrefix = StringHelper.BytesDifference(lastTerm, v);
                    Data.WriteVInt(sharedPrefix);
                    Data.WriteVInt(v.Length - sharedPrefix);
                    Data.WriteBytes(v.Bytes, v.Offset + sharedPrefix, v.Length - sharedPrefix);
                    lastTerm.CopyBytes(v);
                    count++;
                }
                long indexStartFP = Data.FilePointer;
                // write addresses of indexed terms
                termAddresses.Finish();
                addressBuffer.WriteTo(Data);
                addressBuffer = null;
                termAddresses = null;
                Meta.WriteVInt(minLength);
                Meta.WriteVInt(maxLength);
                Meta.WriteVLong(count);
                Meta.WriteLong(startFP);
                Meta.WriteVInt(ADDRESS_INTERVAL);
                Meta.WriteLong(indexStartFP);
                Meta.WriteVInt(PackedInts.VERSION_CURRENT);
                Meta.WriteVInt(BLOCK_SIZE);
            }
        }

        public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd)
        {
            Meta.WriteVInt(field.Number);
            Meta.WriteByte((byte)Lucene45DocValuesFormat.SORTED);
            AddTermsDict(field, values);
            AddNumericField(field, docToOrd, false);
        }

        private static bool IsSingleValued(IEnumerable<long?> docToOrdCount)
        {
            return docToOrdCount.All(ordCount => ordCount <= 1);
        }

        public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords)
        {
            Meta.WriteVInt(field.Number);
            Meta.WriteByte((byte)Lucene45DocValuesFormat.SORTED_SET);

            if (IsSingleValued(docToOrdCount))
            {
                Meta.WriteVInt(SORTED_SET_SINGLE_VALUED_SORTED);
                // The field is single-valued, we can encode it as SORTED
                AddSortedField(field, values, GetSortedSetEnumerable(docToOrdCount, ords));
                return;
            }

            Meta.WriteVInt(SORTED_SET_WITH_ADDRESSES);

            // write the ord -> byte[] as a binary field
            AddTermsDict(field, values);

            // write the stream of ords as a numeric field
            // NOTE: we could return an iterator that delta-encodes these within a doc
            AddNumericField(field, ords, false);

            // write the doc -> ord count as a absolute index to the stream
            Meta.WriteVInt(field.Number);
            Meta.WriteByte((byte)Lucene45DocValuesFormat.NUMERIC);
            Meta.WriteVInt(DELTA_COMPRESSED);
            Meta.WriteLong(-1L);
            Meta.WriteVInt(PackedInts.VERSION_CURRENT);
            Meta.WriteLong(Data.FilePointer);
            Meta.WriteVLong(MaxDoc);
            Meta.WriteVInt(BLOCK_SIZE);

            var writer = new MonotonicBlockPackedWriter(Data, BLOCK_SIZE);
            long addr = 0;
            foreach (long? v in docToOrdCount)
            {
                addr += v.Value;
                writer.Add(addr);
            }
            writer.Finish();
        }

        private IEnumerable<long?> GetSortedSetEnumerable(IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords)
        {
            IEnumerator<long?> docToOrdCountIter = docToOrdCount.GetEnumerator();
            IEnumerator<long?> ordsIter = ords.GetEnumerator();

            const long MISSING_ORD = -1;

            while (docToOrdCountIter.MoveNext())
            {
                long current = docToOrdCountIter.Current.Value;
                if (current == 0)
                {
                    yield return MISSING_ORD;
                }
                else
                {
                    Debug.Assert(current == 1);
                    ordsIter.MoveNext();
                    yield return ordsIter.Current;
                }
            }

            Debug.Assert(!ordsIter.MoveNext());
        }

        /*
      private class IterableAnonymousInnerClassHelper : IEnumerable<int>
	  {
		  private readonly Lucene45DocValuesConsumer OuterInstance;

		  private IEnumerable<int> DocToOrdCount;
		  private IEnumerable<long> Ords;

		  public IterableAnonymousInnerClassHelper(IEnumerable<int> docToOrdCount, IEnumerable<long> ords)
		  {
			  //this.OuterInstance = outerInstance;
			  this.DocToOrdCount = docToOrdCount;
			  this.Ords = ords;
		  }

          public virtual IEnumerator<BytesRef> GetEnumerator()
		  {
			*/
        /*IEnumerator<Number> docToOrdCountIt = DocToOrdCount.GetEnumerator();
      IEnumerator<Number> ordsIt = Ords.GetEnumerator();
      return new IteratorAnonymousInnerClassHelper(this, docToOrdCountIt, ordsIt);*/
        /*
return new SortedSetIterator(DocToOrdCount.GetEnumerator(), Ords.GetEnumerator());
}

System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
{
return GetEnumerator();
}

private class SortedSetIterator : IEnumerator<BytesRef>
{
internal byte[] buffer = new byte[10]; //Initial size, will grow if needed
internal ByteArrayDataOutput output = new ByteArrayDataOutput();
internal BytesRef bytesRef = new BytesRef();

internal IEnumerator<int> counts;
internal IEnumerator<long> ords;

internal SortedSetIterator(IEnumerator<int> counts, IEnumerator<long> ords)
{
this.counts = counts;
this.ords = ords;
}

public BytesRef Current
{
get
{
return bytesRef;
}
}

public void Dispose()
{
counts.Dispose();
ords.Dispose();
}

object System.Collections.IEnumerator.Current
{
get { return bytesRef;  }
}

public bool MoveNext()
{
if (!counts.MoveNext())
return false;

int count = counts.Current;
int maxSize = count * 9;//worst case
if (maxSize > buffer.Length)
buffer = ArrayUtil.Grow(buffer, maxSize);

try
{
EncodeValues(count);
}
catch (System.IO.IOException)
{
throw;
}

bytesRef.Bytes = buffer;
bytesRef.Offset = 0;
bytesRef.Length = output.Position;

return true;
}

private void EncodeValues(int count)
{
output.Reset(buffer);
long lastOrd = 0;
for (int i = 0; i < count; i++)
{
ords.MoveNext();
long ord = ords.Current;
output.WriteVLong(ord - lastOrd);
lastOrd = ord;
}
}

public void Reset()
{
throw new NotImplementedException();
}
}*/

        /*private class IteratorAnonymousInnerClassHelper : IEnumerator<Number>
        {
            private readonly IterableAnonymousInnerClassHelper OuterInstance;

            private IEnumerator<Number> DocToOrdCountIt;
            private IEnumerator<Number> OrdsIt;

            public IteratorAnonymousInnerClassHelper(IterableAnonymousInnerClassHelper outerInstance, IEnumerator<Number> docToOrdCountIt, IEnumerator<Number> ordsIt)
            {
                this.OuterInstance = outerInstance;
                this.DocToOrdCountIt = docToOrdCountIt;
                this.OrdsIt = ordsIt;
            }

            public virtual bool HasNext()
            {
              return DocToOrdCountIt.HasNext();
            }

            public virtual Number Next()
            {
              Number ordCount = DocToOrdCountIt.next();
              if ((long)ordCount == 0)
              {
                return MISSING_ORD;
              }
              else
              {
                Debug.Assert((long)ordCount == 1);
                return OrdsIt.next();
              }
            }

            public virtual void Remove()
            {
              throw new System.NotSupportedException();
            }
        }*/

        //}

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                bool success = false;
                try
                {
                    if (Meta != null)
                    {
                        Meta.WriteVInt(-1); // write EOF marker
                        CodecUtil.WriteFooter(Meta); // write checksum
                    }
                    if (Data != null)
                    {
                        CodecUtil.WriteFooter(Data); // write checksum
                    }
                    success = true;
                }
                finally
                {
                    if (success)
                    {
                        IOUtils.Close(Data, Meta);
                    }
                    else
                    {
                        IOUtils.CloseWhileHandlingException(Data, Meta);
                    }
                    Meta = Data = null;
                }
            }
        }
    }
}