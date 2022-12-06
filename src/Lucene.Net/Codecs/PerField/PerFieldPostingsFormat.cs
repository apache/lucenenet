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

    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
    using Terms = Lucene.Net.Index.Terms;

    /// <summary>
    /// Enables per field postings support.
    /// <para/>
    /// Note, when extending this class, the name (<see cref="PostingsFormat.Name"/>) is
    /// written into the index. In order for the field to be read, the
    /// name must resolve to your implementation via <see cref="PostingsFormat.ForName(string)"/>.
    /// This method uses <see cref="IPostingsFormatFactory.GetPostingsFormat(string)"/> to resolve format names.
    /// See <see cref="DefaultPostingsFormatFactory"/> for information about how to implement your own <see cref="PostingsFormat"/>.
    /// <para/>
    /// Files written by each posting format have an additional suffix containing the
    /// format name. For example, in a per-field configuration instead of <c>_1.prx</c>
    /// filenames would look like <c>_1_Lucene40_0.prx</c>. 
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    /// <seealso cref="IPostingsFormatFactory"/>
    /// <seealso cref="DefaultPostingsFormatFactory"/>
    [PostingsFormatName("PerField40")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public abstract class PerFieldPostingsFormat : PostingsFormat
    {
        // LUCENENET specific - removed this static variable because our name is determined by the PostingsFormatNameAttribute
        ///// <summary>
        ///// Name of this <seealso cref="PostingsFormat"/>. </summary>
        //public static readonly string PER_FIELD_NAME = "PerField40";

        /// <summary>
        /// <see cref="FieldInfo"/> attribute name used to store the
        /// format name for each field.
        /// </summary>
        public static readonly string PER_FIELD_FORMAT_KEY = typeof(PerFieldPostingsFormat).Name + ".format";

        /// <summary>
        /// <see cref="FieldInfo"/> attribute name used to store the
        /// segment suffix name for each field.
        /// </summary>
        public static readonly string PER_FIELD_SUFFIX_KEY = typeof(PerFieldPostingsFormat).Name + ".suffix";

        /// <summary>
        /// Sole constructor. </summary>
        protected PerFieldPostingsFormat() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : base()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            return new FieldsWriter(this, state);
        }

        internal class FieldsConsumerAndSuffix : IDisposable
        {
            internal FieldsConsumer Consumer { get; set; }
            internal int Suffix { get; set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        private class FieldsWriter : FieldsConsumer
        {
            private readonly PerFieldPostingsFormat outerInstance;

            internal readonly IDictionary<PostingsFormat, FieldsConsumerAndSuffix> formats = new Dictionary<PostingsFormat, FieldsConsumerAndSuffix>();
            internal readonly IDictionary<string, int> suffixes = new Dictionary<string, int>();

            internal readonly SegmentWriteState segmentWriteState;

            public FieldsWriter(PerFieldPostingsFormat outerInstance, SegmentWriteState state)
            {
                this.outerInstance = outerInstance;
                segmentWriteState = state;
            }

            public override TermsConsumer AddField(FieldInfo field)
            {
                PostingsFormat format = outerInstance.GetPostingsFormatForField(field.Name);
                if (format is null)
                {
                    throw IllegalStateException.Create("invalid null PostingsFormat for field=\"" + field.Name + "\"");
                }
                string formatName = format.Name;

                string previousValue = field.PutAttribute(PER_FIELD_FORMAT_KEY, formatName);
                if (Debugging.AssertsEnabled) Debugging.Assert(previousValue is null);

                int suffix;

                if (!formats.TryGetValue(format, out FieldsConsumerAndSuffix consumer) || consumer is null)
                {
                    // First time we are seeing this format; create a new instance

                    // bump the suffix
                    if (!suffixes.TryGetValue(formatName, out suffix))
                    {
                        suffix = 0;
                    }
                    else
                    {
                        suffix = suffix + 1;
                    }
                    suffixes[formatName] = suffix;

                    string segmentSuffix = GetFullSegmentSuffix(field.Name,
                                                                segmentWriteState.SegmentSuffix,
                                                                GetSuffix(formatName, Convert.ToString(suffix, CultureInfo.InvariantCulture)));
                    consumer = new FieldsConsumerAndSuffix();
                    consumer.Consumer = format.FieldsConsumer(new SegmentWriteState(segmentWriteState, segmentSuffix));
                    consumer.Suffix = suffix;
                    formats[format] = consumer;
                }
                else
                {
                    // we've already seen this format, so just grab its suffix
                    if (Debugging.AssertsEnabled) Debugging.Assert(suffixes.ContainsKey(formatName));
                    suffix = consumer.Suffix;
                }

                previousValue = field.PutAttribute(PER_FIELD_SUFFIX_KEY, Convert.ToString(suffix, CultureInfo.InvariantCulture));
                if (Debugging.AssertsEnabled) Debugging.Assert(previousValue is null);

                // TODO: we should only provide the "slice" of FIS
                // that this PF actually sees ... then stuff like
                // .hasProx could work correctly?
                // NOTE: .hasProx is already broken in the same way for the non-perfield case,
                // if there is a fieldinfo with prox that has no postings, you get a 0 byte file.
                return consumer.Consumer.AddField(field);
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
        internal static string GetFullSegmentSuffix(string fieldName, string outerSegmentSuffix, string segmentSuffix)
        {
            if (outerSegmentSuffix.Length == 0)
            {
                return segmentSuffix;
            }
            else
            {
                // TODO: support embedding; I think it should work but
                // we need a test confirm to confirm
                // return outerSegmentSuffix + "_" + segmentSuffix;
                throw IllegalStateException.Create("cannot embed PerFieldPostingsFormat inside itself (field \"" + fieldName + "\" returned PerFieldPostingsFormat)");
            }
        }

        private class FieldsReader : FieldsProducer
        {
            // LUCENENET specific: Use StringComparer.Ordinal to get the same ordering as Java
            internal readonly IDictionary<string, FieldsProducer> fields = new JCG.SortedDictionary<string, FieldsProducer>(StringComparer.Ordinal);
            internal readonly IDictionary<string, FieldsProducer> formats = new Dictionary<string, FieldsProducer>();

            public FieldsReader(SegmentReadState readState)
            {
                // Read _X.per and init each format:
                bool success = false;
                try
                {
                    // Read field name -> format name
                    foreach (FieldInfo fi in readState.FieldInfos)
                    {
                        if (fi.IsIndexed)
                        {
                            string fieldName = fi.Name;
                            string formatName = fi.GetAttribute(PER_FIELD_FORMAT_KEY);
                            if (formatName != null)
                            {
                                // null formatName means the field is in fieldInfos, but has no postings!
                                string suffix = fi.GetAttribute(PER_FIELD_SUFFIX_KEY);
                                if (Debugging.AssertsEnabled) Debugging.Assert(suffix != null);
                                PostingsFormat format = PostingsFormat.ForName(formatName);
                                string segmentSuffix = GetSuffix(formatName, suffix);
                                // LUCENENET: Eliminated extra lookup by using TryGetValue instead of ContainsKey
                                if (!formats.TryGetValue(segmentSuffix, out Codecs.FieldsProducer field))
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override IEnumerator<string> GetEnumerator()
            {
                return fields.Keys.GetEnumerator(); // LUCENENET NOTE: enumerators are not writable in .NET
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override Terms GetTerms(string field)
            {
                if (fields.TryGetValue(field, out FieldsProducer fieldsProducer) && fieldsProducer != null)
                {
                    return fieldsProducer.GetTerms(field);
                }

                return null;
            }

            public override int Count => fields.Count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    IOUtils.Dispose(formats.Values);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long RamBytesUsed()
            {
                long sizeInBytes = 0;
                foreach (KeyValuePair<string, FieldsProducer> entry in formats)
                {
                    sizeInBytes += entry.Key.Length * RamUsageEstimator.NUM_BYTES_CHAR;
                    sizeInBytes += entry.Value.RamBytesUsed();
                }
                return sizeInBytes;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void CheckIntegrity()
            {
                foreach (FieldsProducer producer in formats.Values)
                {
                    producer.CheckIntegrity();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed FieldsProducer FieldsProducer(SegmentReadState state)
        {
            return new FieldsReader(state);
        }

        /// <summary>
        /// Returns the postings format that should be used for writing
        /// new segments of <paramref name="field"/>.
        /// <para/>
        /// The field to format mapping is written to the index, so
        /// this method is only invoked when writing, not when reading.
        /// </summary>
        public abstract PostingsFormat GetPostingsFormatForField(string field);
    }
}