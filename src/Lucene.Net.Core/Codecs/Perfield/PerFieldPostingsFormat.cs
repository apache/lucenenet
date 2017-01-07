using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
    using Terms = Lucene.Net.Index.Terms;

    /// <summary>
    /// Enables per field postings support.
    /// <p>
    /// Note, when extending this class, the name (<seealso cref="#getName"/>) is
    /// written into the index. In order for the field to be read, the
    /// name must resolve to your implementation via <seealso cref="#forName(String)"/>.
    /// this method uses Java's
    /// <seealso cref="ServiceLoader Service Provider Interface"/> to resolve format names.
    /// <p>
    /// Files written by each posting format have an additional suffix containing the
    /// format name. For example, in a per-field configuration instead of <tt>_1.prx</tt>
    /// filenames would look like <tt>_1_Lucene40_0.prx</tt>. </summary>
    /// <seealso cref= ServiceLoader
    /// @lucene.experimental </seealso>

    public abstract class PerFieldPostingsFormat : PostingsFormat
    {
        /// <summary>
        /// Name of this <seealso cref="PostingsFormat"/>. </summary>
        public static readonly string PER_FIELD_NAME = "PerField40";

        /// <summary>
        /// <seealso cref="FieldInfo"/> attribute name used to store the
        ///  format name for each field.
        /// </summary>
        public static readonly string PER_FIELD_FORMAT_KEY = typeof(PerFieldPostingsFormat).Name + ".format";

        /// <summary>
        /// <seealso cref="FieldInfo"/> attribute name used to store the
        ///  segment suffix name for each field.
        /// </summary>
        public static readonly string PER_FIELD_SUFFIX_KEY = typeof(PerFieldPostingsFormat).Name + ".suffix";

        /// <summary>
        /// Sole constructor. </summary>
        public PerFieldPostingsFormat()
            : base(PER_FIELD_NAME)
        {
        }

        public override sealed FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            return new FieldsWriter(this, state);
        }

        internal class FieldsConsumerAndSuffix : IDisposable
        {
            internal FieldsConsumer Consumer { get; set; }
            internal int Suffix { get; set; }

            public void Dispose()
            {
                Consumer.Dispose();
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
                if (format == null)
                {
                    throw new InvalidOperationException("invalid null PostingsFormat for field=\"" + field.Name + "\"");
                }
                string formatName = format.Name;

                string previousValue = field.PutAttribute(PER_FIELD_FORMAT_KEY, formatName);
                //Debug.Assert(previousValue == null);

                int suffix;

                FieldsConsumerAndSuffix consumer;
                formats.TryGetValue(format, out consumer);
                if (consumer == null)
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

                    string segmentSuffix = GetFullSegmentSuffix(field.Name, segmentWriteState.SegmentSuffix, GetSuffix(formatName, Convert.ToString(suffix)));
                    consumer = new FieldsConsumerAndSuffix();
                    consumer.Consumer = format.FieldsConsumer(new SegmentWriteState(segmentWriteState, segmentSuffix));
                    consumer.Suffix = suffix;
                    formats[format] = consumer;
                }
                else
                {
                    // we've already seen this format, so just grab its suffix
                    Debug.Assert(suffixes.ContainsKey(formatName));
                    suffix = consumer.Suffix;
                }

                previousValue = field.PutAttribute(PER_FIELD_SUFFIX_KEY, Convert.ToString(suffix));
                //Debug.Assert(previousValue == null);

                // TODO: we should only provide the "slice" of FIS
                // that this PF actually sees ... then stuff like
                // .hasProx could work correctly?
                // NOTE: .hasProx is already broken in the same way for the non-perfield case,
                // if there is a fieldinfo with prox that has no postings, you get a 0 byte file.
                return consumer.Consumer.AddField(field);
            }

            public override void Dispose()
            {
                // Close all subs
                IOUtils.Close(formats.Values.ToArray());
            }
        }

        internal static string GetSuffix(string formatName, string suffix)
        {
            return formatName + "_" + suffix;
        }

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
                throw new InvalidOperationException("cannot embed PerFieldPostingsFormat inside itself (field \"" + fieldName + "\" returned PerFieldPostingsFormat)");
            }
        }

        private class FieldsReader : FieldsProducer
        {
            private readonly PerFieldPostingsFormat outerInstance;

            internal readonly IDictionary<string, FieldsProducer> fields = new SortedDictionary<string, FieldsProducer>();
            internal readonly IDictionary<string, FieldsProducer> formats = new Dictionary<string, FieldsProducer>();

            public FieldsReader(PerFieldPostingsFormat outerInstance, SegmentReadState readState)
            {
                this.outerInstance = outerInstance;

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
                                Debug.Assert(suffix != null);
                                PostingsFormat format = PostingsFormat.ForName(formatName);
                                string segmentSuffix = GetSuffix(formatName, suffix);
                                if (!formats.ContainsKey(segmentSuffix))
                                {
                                    formats[segmentSuffix] = format.FieldsProducer(new SegmentReadState(readState, segmentSuffix));
                                }
                                fields[fieldName] = formats[segmentSuffix];
                            }
                        }
                    }
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.CloseWhileHandlingException(formats.Values);
                    }
                }
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return fields.Keys.GetEnumerator();
            }

            public override Terms Terms(string field)
            {
                FieldsProducer fieldsProducer;
                fields.TryGetValue(field, out fieldsProducer);
                return fieldsProducer == null ? null : fieldsProducer.Terms(field);
            }

            public override int Count
            {
                get { return fields.Count; }
            }

            public override void Dispose()
            {
                IOUtils.Close(formats.Values.ToArray());
            }

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

            public override void CheckIntegrity()
            {
                foreach (FieldsProducer producer in formats.Values)
                {
                    producer.CheckIntegrity();
                }
            }
        }

        public override sealed FieldsProducer FieldsProducer(SegmentReadState state)
        {
            return new FieldsReader(this, state);
        }

        /// <summary>
        /// Returns the postings format that should be used for writing
        /// new segments of <code>field</code>.
        /// <p>
        /// The field to format mapping is written to the index, so
        /// this method is only invoked when writing, not when reading.
        /// </summary>
        public abstract PostingsFormat GetPostingsFormatForField(string field);
    }
}