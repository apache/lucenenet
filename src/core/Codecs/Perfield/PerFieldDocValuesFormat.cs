using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Perfield
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
	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using NumericDocValues = Lucene.Net.Index.NumericDocValues;
	using SegmentReadState = Lucene.Net.Index.SegmentReadState;
	using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
	using SortedDocValues = Lucene.Net.Index.SortedDocValues;
	using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

	/// <summary>
	/// Enables per field docvalues support.
	/// <p>
	/// Note, when extending this class, the name (<seealso cref="#getName"/>) is 
	/// written into the index. In order for the field to be read, the
	/// name must resolve to your implementation via <seealso cref="#forName(String)"/>.
	/// this method uses Java's 
	/// <seealso cref="ServiceLoader Service Provider Interface"/> to resolve format names.
	/// <p>
	/// Files written by each docvalues format have an additional suffix containing the 
	/// format name. For example, in a per-field configuration instead of <tt>_1.dat</tt> 
	/// filenames would look like <tt>_1_Lucene40_0.dat</tt>. </summary>
	/// <seealso cref= ServiceLoader
	/// @lucene.experimental </seealso>

	public abstract class PerFieldDocValuesFormat : DocValuesFormat
	{
	  /// <summary>
	  /// Name of this <seealso cref="PostingsFormat"/>. </summary>
	  public const string PER_FIELD_NAME = "PerFieldDV40";

	  /// <summary>
	  /// <seealso cref="FieldInfo"/> attribute name used to store the
	  ///  format name for each field. 
	  /// </summary>
	  public static readonly string PER_FIELD_FORMAT_KEY = typeof(PerFieldDocValuesFormat).SimpleName + ".format";

	  /// <summary>
	  /// <seealso cref="FieldInfo"/> attribute name used to store the
	  ///  segment suffix name for each field. 
	  /// </summary>
	  public static readonly string PER_FIELD_SUFFIX_KEY = typeof(PerFieldDocValuesFormat).SimpleName + ".suffix";


	  /// <summary>
	  /// Sole constructor. </summary>
	  public PerFieldDocValuesFormat() : base(PER_FIELD_NAME)
	  {
	  }

	  public override sealed DocValuesConsumer FieldsConsumer(SegmentWriteState state)
	  {
		return new FieldsWriter(this, state);
	  }

	  internal class ConsumerAndSuffix : IDisposable
	  {
		internal DocValuesConsumer Consumer;
		internal int Suffix;

		public override void Close()
		{
		  Consumer.close();
		}
	  }

	  private class FieldsWriter : DocValuesConsumer
	  {
		  private readonly PerFieldDocValuesFormat OuterInstance;


		internal readonly IDictionary<DocValuesFormat, ConsumerAndSuffix> Formats = new Dictionary<DocValuesFormat, ConsumerAndSuffix>();
		internal readonly IDictionary<string, int?> Suffixes = new Dictionary<string, int?>();

		internal readonly SegmentWriteState SegmentWriteState;

		public FieldsWriter(PerFieldDocValuesFormat outerInstance, SegmentWriteState state)
		{
			this.OuterInstance = outerInstance;
		  SegmentWriteState = state;
		}

		public override void AddNumericField(FieldInfo field, IEnumerable<Number> values)
		{
		  GetInstance(field).AddNumericField(field, values);
		}

		public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
		{
		  GetInstance(field).AddBinaryField(field, values);
		}

		public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrd)
		{
		  GetInstance(field).AddSortedField(field, values, docToOrd);
		}

		public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrdCount, IEnumerable<Number> ords)
		{
		  GetInstance(field).AddSortedSetField(field, values, docToOrdCount, ords);
		}

		internal virtual DocValuesConsumer GetInstance(FieldInfo field)
		{
		  DocValuesFormat format = null;
		  if (field.DocValuesGen != -1)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String formatName = field.getAttribute(PER_FIELD_FORMAT_KEY);
			string formatName = field.GetAttribute(PER_FIELD_FORMAT_KEY);
			// this means the field never existed in that segment, yet is applied updates
			if (formatName != null)
			{
			  format = DocValuesFormat.ForName(formatName);
			}
		  }
		  if (format == null)
		  {
			format = outerInstance.GetDocValuesFormatForField(field.Name);
		  }
		  if (format == null)
		  {
			throw new IllegalStateException("invalid null DocValuesFormat for field=\"" + field.Name + "\"");
		  }
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String formatName = format.getName();
		  string formatName = format.Name;

		  string previousValue = field.PutAttribute(PER_FIELD_FORMAT_KEY, formatName);
		  Debug.Assert(field.DocValuesGen != -1 || previousValue == null, "formatName=" + formatName + " prevValue=" + previousValue);

		  int? suffix = null;

		  ConsumerAndSuffix consumer = Formats[format];
		  if (consumer == null)
		  {
			// First time we are seeing this format; create a new instance

			if (field.DocValuesGen != -1)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String suffixAtt = field.getAttribute(PER_FIELD_SUFFIX_KEY);
			  string suffixAtt = field.GetAttribute(PER_FIELD_SUFFIX_KEY);
			  // even when dvGen is != -1, it can still be a new field, that never
			  // existed in the segment, and therefore doesn't have the recorded
			  // attributes yet.
			  if (suffixAtt != null)
			  {
				suffix = Convert.ToInt32(suffixAtt);
			  }
			}

			if (suffix == null)
			{
			  // bump the suffix
			  suffix = Suffixes[formatName];
			  if (suffix == null)
			  {
				suffix = 0;
			  }
			  else
			  {
				suffix = suffix + 1;
			  }
			}
			Suffixes[formatName] = suffix;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String segmentSuffix = getFullSegmentSuffix(segmentWriteState.segmentSuffix, getSuffix(formatName, Integer.toString(suffix)));
			string segmentSuffix = GetFullSegmentSuffix(SegmentWriteState.SegmentSuffix, GetSuffix(formatName, Convert.ToString(suffix)));
			consumer = new ConsumerAndSuffix();
			consumer.Consumer = format.FieldsConsumer(new SegmentWriteState(SegmentWriteState, segmentSuffix));
			consumer.Suffix = suffix;
			Formats[format] = consumer;
		  }
		  else
		  {
			// we've already seen this format, so just grab its suffix
			Debug.Assert(Suffixes.ContainsKey(formatName));
			suffix = consumer.Suffix;
		  }

		  previousValue = field.PutAttribute(PER_FIELD_SUFFIX_KEY, Convert.ToString(suffix));
		  Debug.Assert(field.DocValuesGen != -1 || previousValue == null, "suffix=" + Convert.ToString(suffix) + " prevValue=" + previousValue);

		  // TODO: we should only provide the "slice" of FIS
		  // that this DVF actually sees ...
		  return consumer.Consumer;
		}

		public override void Close()
		{
		  // Close all subs
		  IOUtils.Close(Formats.Values);
		}
	  }

	  internal static string GetSuffix(string formatName, string suffix)
	  {
		return formatName + "_" + suffix;
	  }

	  internal static string GetFullSegmentSuffix(string outerSegmentSuffix, string segmentSuffix)
	  {
		if (outerSegmentSuffix.Length == 0)
		{
		  return segmentSuffix;
		}
		else
		{
		  return outerSegmentSuffix + "_" + segmentSuffix;
		}
	  }

	  private class FieldsReader : DocValuesProducer
	  {
		  private readonly PerFieldDocValuesFormat OuterInstance;


		internal readonly IDictionary<string, DocValuesProducer> Fields = new SortedDictionary<string, DocValuesProducer>();
		internal readonly IDictionary<string, DocValuesProducer> Formats = new Dictionary<string, DocValuesProducer>();

		public FieldsReader(PerFieldDocValuesFormat outerInstance, SegmentReadState readState)
		{
			this.OuterInstance = outerInstance;

		  // Read _X.per and init each format:
		  bool success = false;
		  try
		  {
			// Read field name -> format name
			foreach (FieldInfo fi in readState.FieldInfos)
			{
			  if (fi.HasDocValues())
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String fieldName = fi.name;
				string fieldName = fi.Name;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String formatName = fi.getAttribute(PER_FIELD_FORMAT_KEY);
				string formatName = fi.GetAttribute(PER_FIELD_FORMAT_KEY);
				if (formatName != null)
				{
				  // null formatName means the field is in fieldInfos, but has no docvalues!
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String suffix = fi.getAttribute(PER_FIELD_SUFFIX_KEY);
				  string suffix = fi.GetAttribute(PER_FIELD_SUFFIX_KEY);
				  Debug.Assert(suffix != null);
				  DocValuesFormat format = DocValuesFormat.ForName(formatName);
				  string segmentSuffix = GetFullSegmentSuffix(readState.SegmentSuffix, GetSuffix(formatName, suffix));
				  if (!Formats.ContainsKey(segmentSuffix))
				  {
					Formats[segmentSuffix] = format.FieldsProducer(new SegmentReadState(readState, segmentSuffix));
				  }
				  Fields[fieldName] = Formats[segmentSuffix];
				}
			  }
			}
			success = true;
		  }
		  finally
		  {
			if (!success)
			{
			  IOUtils.CloseWhileHandlingException(Formats.Values);
			}
		  }
		}

		internal FieldsReader(PerFieldDocValuesFormat outerInstance, FieldsReader other)
		{
			this.OuterInstance = outerInstance;

		  IDictionary<DocValuesProducer, DocValuesProducer> oldToNew = new IdentityHashMap<DocValuesProducer, DocValuesProducer>();
		  // First clone all formats
		  foreach (KeyValuePair<string, DocValuesProducer> ent in other.Formats)
		  {
			DocValuesProducer values = ent.Value;
			Formats[ent.Key] = values;
			oldToNew[ent.Value] = values;
		  }

		  // Then rebuild fields:
		  foreach (KeyValuePair<string, DocValuesProducer> ent in other.Fields)
		  {
			DocValuesProducer producer = oldToNew[ent.Value];
			Debug.Assert(producer != null);
			Fields[ent.Key] = producer;
		  }
		}

		public override NumericDocValues GetNumeric(FieldInfo field)
		{
		  DocValuesProducer producer = Fields[field.Name];
		  return producer == null ? null : producer.GetNumeric(field);
		}

		public override BinaryDocValues GetBinary(FieldInfo field)
		{
		  DocValuesProducer producer = Fields[field.Name];
		  return producer == null ? null : producer.GetBinary(field);
		}

		public override SortedDocValues GetSorted(FieldInfo field)
		{
		  DocValuesProducer producer = Fields[field.Name];
		  return producer == null ? null : producer.GetSorted(field);
		}

		public override SortedSetDocValues GetSortedSet(FieldInfo field)
		{
		  DocValuesProducer producer = Fields[field.Name];
		  return producer == null ? null : producer.GetSortedSet(field);
		}

		public override Bits GetDocsWithField(FieldInfo field)
		{
		  DocValuesProducer producer = Fields[field.Name];
		  return producer == null ? null : producer.GetDocsWithField(field);
		}

		public override void Close()
		{
		  IOUtils.Close(Formats.Values);
		}

		public override DocValuesProducer Clone()
		{
		  return new FieldsReader(OuterInstance, this);
		}

		public override long RamBytesUsed()
		{
		  long size = 0;
		  foreach (KeyValuePair<string, DocValuesProducer> entry in Formats)
		  {
			size += (entry.Key.length() * RamUsageEstimator.NUM_BYTES_CHAR) + entry.Value.ramBytesUsed();
		  }
		  return size;
		}

		public override void CheckIntegrity()
		{
		  foreach (DocValuesProducer format in Formats.Values)
		  {
			format.CheckIntegrity();
		  }
		}
	  }

	  public override sealed DocValuesProducer FieldsProducer(SegmentReadState state)
	  {
		return new FieldsReader(this, state);
	  }

	  /// <summary>
	  /// Returns the doc values format that should be used for writing 
	  /// new segments of <code>field</code>.
	  /// <p>
	  /// The field to format mapping is written to the index, so
	  /// this method is only invoked when writing, not when reading. 
	  /// </summary>
	  public abstract DocValuesFormat GetDocValuesFormatForField(string field);
	}

}