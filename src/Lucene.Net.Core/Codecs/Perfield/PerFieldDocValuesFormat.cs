using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lucene.Net.Codecs.Perfield
{
    using Lucene.Net.Support;

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
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;

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
        public static readonly string PER_FIELD_NAME = "PerFieldDV40";

        /// <summary>
        /// <seealso cref="FieldInfo"/> attribute name used to store the
        ///  format name for each field.
        /// </summary>
        public static readonly string PER_FIELD_FORMAT_KEY = typeof(PerFieldDocValuesFormat).Name + ".format";

        /// <summary>
        /// <seealso cref="FieldInfo"/> attribute name used to store the
        ///  segment suffix name for each field.
        /// </summary>
        public static readonly string PER_FIELD_SUFFIX_KEY = typeof(PerFieldDocValuesFormat).Name + ".suffix";

        /// <summary>
        /// Sole constructor. </summary>
        public PerFieldDocValuesFormat()
            : base(PER_FIELD_NAME)
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

            public void Dispose()
            {
                Consumer.Dispose();
            }
        }

        private class FieldsWriter : DocValuesConsumer
        {
            private readonly PerFieldDocValuesFormat OuterInstance;

            internal readonly IDictionary<DocValuesFormat, ConsumerAndSuffix> Formats = new Dictionary<DocValuesFormat, ConsumerAndSuffix>();
            internal readonly IDictionary<string, int> Suffixes = new Dictionary<string, int>();

            internal readonly SegmentWriteState SegmentWriteState;

            public FieldsWriter(PerFieldDocValuesFormat outerInstance, SegmentWriteState state)
            {
                this.OuterInstance = outerInstance;
                SegmentWriteState = state;
            }

            public override void AddNumericField(FieldInfo field, IEnumerable<long?> values)
            {
                GetInstance(field).AddNumericField(field, values);
            }

            public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
            {
                GetInstance(field).AddBinaryField(field, values);
            }

            public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd)
            {
                GetInstance(field).AddSortedField(field, values, docToOrd);
            }

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
                if (format == null)
                {
                    format = OuterInstance.GetDocValuesFormatForField(field.Name);
                }
                if (format == null)
                {
                    throw new InvalidOperationException("invalid null DocValuesFormat for field=\"" + field.Name + "\"");
                }
                string formatName_ = format.Name;

                string previousValue = field.PutAttribute(PER_FIELD_FORMAT_KEY, formatName_);
                Debug.Assert(field.DocValuesGen != -1 || previousValue == null, "formatName=" + formatName_ + " prevValue=" + previousValue);

                int suffix = -1;

                ConsumerAndSuffix consumer;
                Formats.TryGetValue(format, out consumer);
                if (consumer == null)
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
                            suffix = Convert.ToInt32(suffixAtt);
                        }
                    }

                    if (suffix == -1)
                    {
                        // bump the suffix
                        if (!Suffixes.TryGetValue(formatName_, out suffix))
                        {
                            suffix = 0;
                        }
                        else
                        {
                            suffix = suffix + 1;
                        }
                    }
                    Suffixes[formatName_] = suffix;

                    string segmentSuffix = GetFullSegmentSuffix(SegmentWriteState.SegmentSuffix, GetSuffix(formatName_, Convert.ToString(suffix)));
                    consumer = new ConsumerAndSuffix();
                    consumer.Consumer = format.FieldsConsumer(new SegmentWriteState(SegmentWriteState, segmentSuffix));
                    consumer.Suffix = suffix;
                    Formats[format] = consumer;
                }
                else
                {
                    // we've already seen this format, so just grab its suffix
                    Debug.Assert(Suffixes.ContainsKey(formatName_));
                    suffix = consumer.Suffix;
                }

                previousValue = field.PutAttribute(PER_FIELD_SUFFIX_KEY, Convert.ToString(suffix));
                Debug.Assert(field.DocValuesGen != -1 || previousValue == null, "suffix=" + Convert.ToString(suffix) + " prevValue=" + previousValue);

                // TODO: we should only provide the "slice" of FIS
                // that this DVF actually sees ...
                return consumer.Consumer;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    // Close all subs
                    IOUtils.Close(Formats.Values.ToArray());
                }
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
                            string fieldName = fi.Name;
                            string formatName = fi.GetAttribute(PER_FIELD_FORMAT_KEY);
                            if (formatName != null)
                            {
                                // null formatName means the field is in fieldInfos, but has no docvalues!
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

                IDictionary<DocValuesProducer, DocValuesProducer> oldToNew = new HashMap<DocValuesProducer, DocValuesProducer>();
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
                DocValuesProducer producer;
                if (Fields.TryGetValue(field.Name, out producer))
                {
                    return producer.GetNumeric(field);
                }
                return null;
            }

            public override BinaryDocValues GetBinary(FieldInfo field)
            {
                DocValuesProducer producer;
                if (Fields.TryGetValue(field.Name, out producer))
                {
                    return producer.GetBinary(field);
                }
                return null;
            }

            public override SortedDocValues GetSorted(FieldInfo field)
            {
                DocValuesProducer producer;
                if (Fields.TryGetValue(field.Name, out producer))
                {
                    return producer.GetSorted(field);
                }
                return null;
            }

            public override SortedSetDocValues GetSortedSet(FieldInfo field)
            {
                DocValuesProducer producer;
                if (Fields.TryGetValue(field.Name, out producer))
                {
                    return producer.GetSortedSet(field);
                }
                return null;
            }

            public override Bits GetDocsWithField(FieldInfo field)
            {
                DocValuesProducer producer;
                if (Fields.TryGetValue(field.Name, out producer))
                {
                    return producer.GetDocsWithField(field);
                }
                return null;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    IOUtils.Close(Formats.Values.ToArray());
            }

            public object Clone()
            {
                return new FieldsReader(OuterInstance, this);
            }

            public override long RamBytesUsed()
            {
                long size = 0;
                foreach (KeyValuePair<string, DocValuesProducer> entry in Formats)
                {
                    size += (entry.Key.Length * RamUsageEstimator.NUM_BYTES_CHAR) + entry.Value.RamBytesUsed();
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