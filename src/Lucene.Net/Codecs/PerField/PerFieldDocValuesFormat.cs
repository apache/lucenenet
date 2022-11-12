using J2N.Runtime.CompilerServices;
using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Codecs.PerField
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
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using IBits = Lucene.Net.Util.IBits;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;

    /// <summary>
    /// Enables per field docvalues support.
    /// <para/>
    /// Note, when extending this class, the name (<see cref="DocValuesFormat.Name"/>) is
    /// written into the index. In order for the field to be read, the
    /// name must resolve to your implementation via <see cref="DocValuesFormat.ForName(string)"/>.
    /// This method uses <see cref="IDocValuesFormatFactory.GetDocValuesFormat(string)"/> to resolve format names.
    /// See <see cref="DefaultDocValuesFormatFactory"/> for information about how to implement your own <see cref="DocValuesFormat"/>.
    /// <para/>
    /// Files written by each docvalues format have an additional suffix containing the
    /// format name. For example, in a per-field configuration instead of <c>_1.dat</c>
    /// filenames would look like <c>_1_Lucene40_0.dat</c>. 
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="IDocValuesFormatFactory"/>
    /// <seealso cref="DefaultDocValuesFormatFactory"/>
    [DocValuesFormatName("PerFieldDV40")]
    public abstract class PerFieldDocValuesFormat : DocValuesFormat
    {
        // LUCENENET specific: Removing this static variable, since name is now determined by the DocValuesFormatNameAttribute.
        ///// <summary>
        ///// Name of this <seealso cref="PostingsFormat"/>. </summary>
        //public static readonly string PER_FIELD_NAME = "PerFieldDV40";

        /// <summary>
        /// <see cref="FieldInfo"/> attribute name used to store the
        /// format name for each field.
        /// </summary>
        public static readonly string PER_FIELD_FORMAT_KEY = typeof(PerFieldDocValuesFormat).Name + ".format";

        /// <summary>
        /// <see cref="FieldInfo"/> attribute name used to store the
        /// segment suffix name for each field.
        /// </summary>
        public static readonly string PER_FIELD_SUFFIX_KEY = typeof(PerFieldDocValuesFormat).Name + ".suffix";

        /// <summary>
        /// Sole constructor. </summary>
        protected PerFieldDocValuesFormat() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : base()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            return new FieldsWriter(this, state);
        }

        internal class ConsumerAndSuffix : IDisposable
        {
            internal DocValuesConsumer Consumer { get; set; }
            internal int Suffix { get; set; }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Consumer.Dispose();
                }
            }
        }

        private class FieldsWriter : DocValuesConsumer
        {
            private readonly PerFieldDocValuesFormat outerInstance;

            internal readonly IDictionary<DocValuesFormat, ConsumerAndSuffix> formats = new Dictionary<DocValuesFormat, ConsumerAndSuffix>();
            internal readonly IDictionary<string, int?> suffixes = new Dictionary<string, int?>();

            internal readonly SegmentWriteState segmentWriteState;

            public FieldsWriter(PerFieldDocValuesFormat outerInstance, SegmentWriteState state)
            {
                this.outerInstance = outerInstance;
                segmentWriteState = state;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void AddNumericField(FieldInfo field, IEnumerable<long?> values)
            {
                GetInstance(field).AddNumericField(field, values);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
            {
                GetInstance(field).AddBinaryField(field, values);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd)
            {
                GetInstance(field).AddSortedField(field, values, docToOrd);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords)
            {
                GetInstance(field).AddSortedSetField(field, values, docToOrdCount, ords);
            }

            internal virtual DocValuesConsumer GetInstance(FieldInfo field)
            {
                DocValuesFormat format = null;
                if (field.DocValuesGen != -1)
                {
                    string formatName = field.GetAttribute(PER_FIELD_FORMAT_KEY);
                    // this means the field never existed in that segment, yet is applied updates
                    if (formatName != null)
                    {
                        format = DocValuesFormat.ForName(formatName);
                    }
                }
                if (format is null)
                {
                    format = outerInstance.GetDocValuesFormatForField(field.Name);
                }
                if (format is null)
                {
                    throw IllegalStateException.Create("invalid null DocValuesFormat for field=\"" + field.Name + "\"");
                }
                string formatName_ = format.Name;

                string previousValue = field.PutAttribute(PER_FIELD_FORMAT_KEY, formatName_);
                if (Debugging.AssertsEnabled) Debugging.Assert(field.DocValuesGen != -1 || previousValue is null,"formatName={0} prevValue={1}", formatName_, previousValue);

                int? suffix = null;

                if (!formats.TryGetValue(format, out ConsumerAndSuffix consumer) || consumer is null)
                {
                    // First time we are seeing this format; create a new instance

                    if (field.DocValuesGen != -1)
                    {
                        string suffixAtt = field.GetAttribute(PER_FIELD_SUFFIX_KEY);
                        // even when dvGen is != -1, it can still be a new field, that never
                        // existed in the segment, and therefore doesn't have the recorded
                        // attributes yet.
                        if (suffixAtt != null)
                        {
                            suffix = Convert.ToInt32(suffixAtt, CultureInfo.InvariantCulture);
                        }
                    }

                    if (suffix is null)
                    {
                        // bump the suffix
                        if (!suffixes.TryGetValue(formatName_, out suffix) || suffix is null)
                        {
                            suffix = 0;
                        }
                        else
                        {
                            suffix = suffix + 1;
                        }
                    }
                    suffixes[formatName_] = suffix;

                    string segmentSuffix = GetFullSegmentSuffix(segmentWriteState.SegmentSuffix, GetSuffix(formatName_, Convert.ToString(suffix, CultureInfo.InvariantCulture)));
                    consumer = new ConsumerAndSuffix();
                    consumer.Consumer = format.FieldsConsumer(new SegmentWriteState(segmentWriteState, segmentSuffix));
                    consumer.Suffix = suffix.Value; // LUCENENET NOTE: At this point suffix cannot be null
                    formats[format] = consumer;
                }
                else
                {
                    // we've already seen this format, so just grab its suffix
                    if (Debugging.AssertsEnabled) Debugging.Assert(suffixes.ContainsKey(formatName_));
                    suffix = consumer.Suffix;
                }

                previousValue = field.PutAttribute(PER_FIELD_SUFFIX_KEY, Convert.ToString(suffix, CultureInfo.InvariantCulture));
                if (Debugging.AssertsEnabled) Debugging.Assert(field.DocValuesGen != -1 || previousValue is null,"suffix={0} prevValue={1}", suffix, previousValue);

                // TODO: we should only provide the "slice" of FIS
                // that this DVF actually sees ...
                return consumer.Consumer;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    // Close all subs
                    IOUtils.Dispose(formats.Values);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string GetSuffix(string formatName, string suffix)
        {
            return formatName + "_" + suffix;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            private readonly PerFieldDocValuesFormat outerInstance;

            // LUCENENET specific: Use StringComparer.Ordinal to get the same ordering as Java
            internal readonly IDictionary<string, DocValuesProducer> fields = new JCG.SortedDictionary<string, DocValuesProducer>(StringComparer.Ordinal);
            internal readonly IDictionary<string, DocValuesProducer> formats = new Dictionary<string, DocValuesProducer>();

            public FieldsReader(PerFieldDocValuesFormat outerInstance, SegmentReadState readState)
            {
                this.outerInstance = outerInstance;

                // Read _X.per and init each format:
                bool success = false;
                try
                {
                    // Read field name -> format name
                    foreach (FieldInfo fi in readState.FieldInfos)
                    {
                        if (fi.HasDocValues)
                        {
                            string fieldName = fi.Name;
                            string formatName = fi.GetAttribute(PER_FIELD_FORMAT_KEY);
                            if (formatName != null)
                            {
                                // null formatName means the field is in fieldInfos, but has no docvalues!
                                string suffix = fi.GetAttribute(PER_FIELD_SUFFIX_KEY);
                                if (Debugging.AssertsEnabled) Debugging.Assert(suffix != null);
                                DocValuesFormat format = DocValuesFormat.ForName(formatName);
                                string segmentSuffix = GetFullSegmentSuffix(readState.SegmentSuffix, GetSuffix(formatName, suffix));
                                // LUCENENET: Eliminated extra lookup by using TryGetValue instead of ContainsKey
                                if (!formats.TryGetValue(segmentSuffix, out DocValuesProducer field))
                                {
                                    formats[segmentSuffix] = field = format.FieldsProducer(new SegmentReadState(readState, segmentSuffix));
                                }
                                fields[fieldName] = field;
                            }
                        }
                    }
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.DisposeWhileHandlingException(formats.Values);
                    }
                }
            }

            internal FieldsReader(PerFieldDocValuesFormat outerInstance, FieldsReader other)
            {
                this.outerInstance = outerInstance;

                IDictionary<DocValuesProducer, DocValuesProducer> oldToNew = new JCG.Dictionary<DocValuesProducer, DocValuesProducer>(IdentityEqualityComparer<DocValuesProducer>.Default);
                // First clone all formats
                foreach (KeyValuePair<string, DocValuesProducer> ent in other.formats)
                {
                    DocValuesProducer values = ent.Value;
                    formats[ent.Key] = values;
                    oldToNew[ent.Value] = values;
                }

                // Then rebuild fields:
                foreach (KeyValuePair<string, DocValuesProducer> ent in other.fields)
                {
                    oldToNew.TryGetValue(ent.Value, out DocValuesProducer producer);
                    if (Debugging.AssertsEnabled) Debugging.Assert(producer != null);
                    fields[ent.Key] = producer;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override NumericDocValues GetNumeric(FieldInfo field)
            {
                if (fields.TryGetValue(field.Name, out DocValuesProducer producer) && producer != null)
                {
                    return producer.GetNumeric(field);
                }
                return null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override BinaryDocValues GetBinary(FieldInfo field)
            {
                if (fields.TryGetValue(field.Name, out DocValuesProducer producer) && producer != null)
                {
                    return producer.GetBinary(field);
                }
                return null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override SortedDocValues GetSorted(FieldInfo field)
            {
                if (fields.TryGetValue(field.Name, out DocValuesProducer producer) && producer != null)
                {
                    return producer.GetSorted(field);
                }
                return null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override SortedSetDocValues GetSortedSet(FieldInfo field)
            {
                if (fields.TryGetValue(field.Name, out DocValuesProducer producer) && producer != null)
                {
                    return producer.GetSortedSet(field);
                }
                return null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override IBits GetDocsWithField(FieldInfo field)
            {
                if (fields.TryGetValue(field.Name, out DocValuesProducer producer) && producer != null)
                {
                    return producer.GetDocsWithField(field);
                }
                return null;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    IOUtils.Dispose(formats.Values);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public object Clone()
            {
                return new FieldsReader(outerInstance, this);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long RamBytesUsed()
            {
                long size = 0;
                foreach (KeyValuePair<string, DocValuesProducer> entry in formats)
                {
                    size += (entry.Key.Length * RamUsageEstimator.NUM_BYTES_CHAR) 
                        + entry.Value.RamBytesUsed();
                }
                return size;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void CheckIntegrity()
            {
                foreach (DocValuesProducer format in formats.Values)
                {
                    format.CheckIntegrity();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            return new FieldsReader(this, state);
        }

        /// <summary>
        /// Returns the doc values format that should be used for writing
        /// new segments of <paramref name="field"/>.
        /// <para/>
        /// The field to format mapping is written to the index, so
        /// this method is only invoked when writing, not when reading.
        /// </summary>
        public abstract DocValuesFormat GetDocValuesFormatForField(string field);
    }
}