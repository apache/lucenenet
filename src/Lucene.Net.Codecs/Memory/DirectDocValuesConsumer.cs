using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace org.apache.lucene.codecs.memory
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


	using FieldInfo = org.apache.lucene.index.FieldInfo;
	using IndexFileNames = org.apache.lucene.index.IndexFileNames;
	using SegmentWriteState = org.apache.lucene.index.SegmentWriteState;
	using IndexOutput = org.apache.lucene.store.IndexOutput;
	using BytesRef = org.apache.lucene.util.BytesRef;
	using IOUtils = org.apache.lucene.util.IOUtils;

//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
	import static org.apache.lucene.codecs.memory.DirectDocValuesProducer.VERSION_CURRENT;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
	import static org.apache.lucene.codecs.memory.DirectDocValuesProducer.BYTES;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
	import static org.apache.lucene.codecs.memory.DirectDocValuesProducer.SORTED;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
	import static org.apache.lucene.codecs.memory.DirectDocValuesProducer.SORTED_SET;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
	import static org.apache.lucene.codecs.memory.DirectDocValuesProducer.NUMBER;

	/// <summary>
	/// Writer for <seealso cref="DirectDocValuesFormat"/>
	/// </summary>

	internal class DirectDocValuesConsumer : DocValuesConsumer
	{
	  internal IndexOutput data, meta;
	  internal readonly int maxDoc;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: DirectDocValuesConsumer(org.apache.lucene.index.SegmentWriteState state, String dataCodec, String dataExtension, String metaCodec, String metaExtension) throws java.io.IOException
	  internal DirectDocValuesConsumer(SegmentWriteState state, string dataCodec, string dataExtension, string metaCodec, string metaExtension)
	  {
		maxDoc = state.segmentInfo.DocCount;
		bool success = false;
		try
		{
		  string dataName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, dataExtension);
		  data = state.directory.createOutput(dataName, state.context);
		  CodecUtil.writeHeader(data, dataCodec, VERSION_CURRENT);
		  string metaName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, metaExtension);
		  meta = state.directory.createOutput(metaName, state.context);
		  CodecUtil.writeHeader(meta, metaCodec, VERSION_CURRENT);
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.closeWhileHandlingException(this);
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void addNumericField(org.apache.lucene.index.FieldInfo field, Iterable<Number> values) throws java.io.IOException
	  public override void addNumericField(FieldInfo field, IEnumerable<Number> values)
	  {
		meta.writeVInt(field.number);
		meta.writeByte(NUMBER);
		addNumericFieldValues(field, values);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void addNumericFieldValues(org.apache.lucene.index.FieldInfo field, Iterable<Number> values) throws java.io.IOException
	  private void addNumericFieldValues(FieldInfo field, IEnumerable<Number> values)
	  {
		meta.writeLong(data.FilePointer);
		long minValue = long.MaxValue;
		long maxValue = long.MinValue;
		bool missing = false;

		long count = 0;
		foreach (Number nv in values)
		{
		  if (nv != null)
		  {
			long v = (long)nv;
			minValue = Math.Min(minValue, v);
			maxValue = Math.Max(maxValue, v);
		  }
		  else
		  {
			missing = true;
		  }
		  count++;
		  if (count >= DirectDocValuesFormat.MAX_SORTED_SET_ORDS)
		  {
			throw new System.ArgumentException("DocValuesField \"" + field.name + "\" is too large, must be <= " + DirectDocValuesFormat.MAX_SORTED_SET_ORDS + " values/total ords");
		  }
		}
		meta.writeInt((int) count);

		if (missing)
		{
		  long start = data.FilePointer;
		  writeMissingBitset(values);
		  meta.writeLong(start);
		  meta.writeLong(data.FilePointer - start);
		}
		else
		{
		  meta.writeLong(-1L);
		}

		sbyte byteWidth;
		if (minValue >= sbyte.MinValue && maxValue <= sbyte.MaxValue)
		{
		  byteWidth = 1;
		}
		else if (minValue >= short.MinValue && maxValue <= short.MaxValue)
		{
		  byteWidth = 2;
		}
		else if (minValue >= int.MinValue && maxValue <= int.MaxValue)
		{
		  byteWidth = 4;
		}
		else
		{
		  byteWidth = 8;
		}
		meta.writeByte(byteWidth);

		foreach (Number nv in values)
		{
		  long v;
		  if (nv != null)
		  {
			v = (long)nv;
		  }
		  else
		  {
			v = 0;
		  }

		  switch (byteWidth)
		  {
		  case 1:
			data.writeByte((sbyte) v);
			break;
		  case 2:
			data.writeShort((short) v);
			break;
		  case 4:
			data.writeInt((int) v);
			break;
		  case 8:
			data.writeLong(v);
			break;
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void close() throws java.io.IOException
	  public override void close()
	  {
		bool success = false;
		try
		{
		  if (meta != null)
		  {
			meta.writeVInt(-1); // write EOF marker
			CodecUtil.writeFooter(meta); // write checksum
		  }
		  if (data != null)
		  {
			CodecUtil.writeFooter(data);
		  }
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			IOUtils.close(data, meta);
		  }
		  else
		  {
			IOUtils.closeWhileHandlingException(data, meta);
		  }
		  data = meta = null;
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void addBinaryField(org.apache.lucene.index.FieldInfo field, final Iterable<org.apache.lucene.util.BytesRef> values) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  public override void addBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
	  {
		meta.writeVInt(field.number);
		meta.writeByte(BYTES);
		addBinaryFieldValues(field, values);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void addBinaryFieldValues(org.apache.lucene.index.FieldInfo field, final Iterable<org.apache.lucene.util.BytesRef> values) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  private void addBinaryFieldValues(FieldInfo field, IEnumerable<BytesRef> values)
	  {
		// write the byte[] data
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long startFP = data.getFilePointer();
		long startFP = data.FilePointer;
		bool missing = false;
		long totalBytes = 0;
		int count = 0;
		foreach (BytesRef v in values)
		{
		  if (v != null)
		  {
			data.writeBytes(v.bytes, v.offset, v.length);
			totalBytes += v.length;
			if (totalBytes > DirectDocValuesFormat.MAX_TOTAL_BYTES_LENGTH)
			{
			  throw new System.ArgumentException("DocValuesField \"" + field.name + "\" is too large, cannot have more than DirectDocValuesFormat.MAX_TOTAL_BYTES_LENGTH (" + DirectDocValuesFormat.MAX_TOTAL_BYTES_LENGTH + ") bytes");
			}
		  }
		  else
		  {
			missing = true;
		  }
		  count++;
		}

		meta.writeLong(startFP);
		meta.writeInt((int) totalBytes);
		meta.writeInt(count);
		if (missing)
		{
		  long start = data.FilePointer;
		  writeMissingBitset(values);
		  meta.writeLong(start);
		  meta.writeLong(data.FilePointer - start);
		}
		else
		{
		  meta.writeLong(-1L);
		}

		int addr = 0;
		foreach (BytesRef v in values)
		{
		  data.writeInt(addr);
		  if (v != null)
		  {
			addr += v.length;
		  }
		}
		data.writeInt(addr);
	  }

	  // TODO: in some cases representing missing with minValue-1 wouldn't take up additional space and so on,
	  // but this is very simple, and algorithms only check this for values of 0 anyway (doesnt slow down normal decode)
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: void writeMissingBitset(Iterable<?> values) throws java.io.IOException
	  internal virtual void writeMissingBitset<T1>(IEnumerable<T1> values)
	  {
		long bits = 0;
		int count = 0;
		foreach (object v in values)
		{
		  if (count == 64)
		  {
			data.writeLong(bits);
			count = 0;
			bits = 0;
		  }
		  if (v != null)
		  {
			bits |= 1L << (count & 0x3f);
		  }
		  count++;
		}
		if (count > 0)
		{
		  data.writeLong(bits);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void addSortedField(org.apache.lucene.index.FieldInfo field, Iterable<org.apache.lucene.util.BytesRef> values, Iterable<Number> docToOrd) throws java.io.IOException
	  public override void addSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrd)
	  {
		meta.writeVInt(field.number);
		meta.writeByte(SORTED);

		// write the ordinals as numerics
		addNumericFieldValues(field, docToOrd);

		// write the values as binary
		addBinaryFieldValues(field, values);
	  }

	  // note: this might not be the most efficient... but its fairly simple
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void addSortedSetField(org.apache.lucene.index.FieldInfo field, Iterable<org.apache.lucene.util.BytesRef> values, final Iterable<Number> docToOrdCount, final Iterable<Number> ords) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  public override void addSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrdCount, IEnumerable<Number> ords)
	  {
		meta.writeVInt(field.number);
		meta.writeByte(SORTED_SET);

		// First write docToOrdCounts, except we "aggregate" the
		// counts so they turn into addresses, and add a final
		// value = the total aggregate:
		addNumericFieldValues(field, new IterableAnonymousInnerClassHelper(this, docToOrdCount));

		// Write ordinals for all docs, appended into one big
		// numerics:
		addNumericFieldValues(field, ords);

		// write the values as binary
		addBinaryFieldValues(field, values);
	  }

	  private class IterableAnonymousInnerClassHelper : IEnumerable<Number>
	  {
		  private readonly DirectDocValuesConsumer outerInstance;

		  private IEnumerable<Number> docToOrdCount;

		  public IterableAnonymousInnerClassHelper(DirectDocValuesConsumer outerInstance, IEnumerable<Number> docToOrdCount)
		  {
			  this.outerInstance = outerInstance;
			  this.docToOrdCount = docToOrdCount;
		  }


			  // Just aggregates the count values so they become
			  // "addresses", and adds one more value in the end
			  // (the final sum):

		  public virtual IEnumerator<Number> GetEnumerator()
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Iterator<Number> iter = docToOrdCount.iterator();
			IEnumerator<Number> iter = docToOrdCount.GetEnumerator();

			return new IteratorAnonymousInnerClassHelper(this, iter);
		  }

		  private class IteratorAnonymousInnerClassHelper : IEnumerator<Number>
		  {
			  private readonly IterableAnonymousInnerClassHelper outerInstance;

			  private IEnumerator<Number> iter;

			  public IteratorAnonymousInnerClassHelper(IterableAnonymousInnerClassHelper outerInstance, IEnumerator<Number> iter)
			  {
				  this.outerInstance = outerInstance;
				  this.iter = iter;
			  }


			  internal long sum;
			  internal bool ended;

			  public virtual bool hasNext()
			  {
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
				return iter.hasNext() || !ended;
			  }

			  public virtual Number next()
			  {
				long toReturn = sum;

//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
				if (iter.hasNext())
				{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
				  Number n = iter.next();
				  if (n != null)
				  {
					sum += (long)n;
				  }
				}
				else if (!ended)
				{
				  ended = true;
				}
				else
				{
				  Debug.Assert(false);
				}

				return toReturn;
			  }

			  public virtual void remove()
			  {
				throw new System.NotSupportedException();
			  }
		  }
	  }
	}

}