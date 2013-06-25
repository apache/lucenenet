using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.PerField
{
    public abstract class PerFieldPostingsFormat : PostingsFormat
    {
        public const String PER_FIELD_NAME = "PerField40";

        public static readonly String PER_FIELD_FORMAT_KEY;
        public static readonly String PER_FIELD_SUFFIX_KEY;

        static PerFieldPostingsFormat()
        {
            PER_FIELD_FORMAT_KEY = typeof(PerFieldPostingsFormat).Name + ".format";
            PER_FIELD_SUFFIX_KEY = typeof(PerFieldPostingsFormat).Name + ".suffix";
        }

        public PerFieldPostingsFormat()
            : base(PER_FIELD_NAME)
        {
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            return new FieldsWriter(this, state);
        }

        internal class FieldsConsumerAndSuffix : IDisposable
        {
            internal FieldsConsumer consumer;
            internal int suffix;

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    consumer.Dispose();
                }

                consumer = null;
            }
        }

        private class FieldsWriter : FieldsConsumer
        {
            private readonly IDictionary<PostingsFormat, FieldsConsumerAndSuffix> formats = new HashMap<PostingsFormat, FieldsConsumerAndSuffix>();
            private readonly IDictionary<String, int> suffixes = new HashMap<String, int>();

            private readonly SegmentWriteState segmentWriteState;

            private readonly PerFieldPostingsFormat parent;

            public FieldsWriter(PerFieldPostingsFormat parent, SegmentWriteState state)
            {
                this.parent = parent;
                segmentWriteState = state;
            }

            public override TermsConsumer AddField(FieldInfo field)
            {
                PostingsFormat format = parent.GetPostingsFormatForField(field.name);
                if (format == null)
                {
                    throw new InvalidOperationException("invalid null PostingsFormat for field=\"" + field.name + "\"");
                }
                String formatName = format.Name;

                String previousValue = field.PutAttribute(PER_FIELD_FORMAT_KEY, formatName);
                //assert previousValue == null;

                int suffix;

                FieldsConsumerAndSuffix consumer = formats[format];
                if (consumer == null)
                {
                    // First time we are seeing this format; create a new instance

                    // bump the suffix
                    suffix = suffixes[formatName];
                    if (suffix == null)
                    {
                        suffix = 0;
                    }
                    else
                    {
                        suffix = suffix + 1;
                    }
                    suffixes[formatName] = suffix;

                    String segmentSuffix = GetFullSegmentSuffix(field.name,
                                                                      segmentWriteState.segmentSuffix,
                                                                      GetSuffix(formatName, suffix.ToString()));
                    consumer = new FieldsConsumerAndSuffix();
                    consumer.consumer = format.FieldsConsumer(new SegmentWriteState(segmentWriteState, segmentSuffix));
                    consumer.suffix = suffix;
                    formats[format] = consumer;
                }
                else
                {
                    // we've already seen this format, so just grab its suffix
                    //assert suffixes.containsKey(formatName);
                    suffix = consumer.suffix;
                }

                previousValue = field.PutAttribute(PER_FIELD_SUFFIX_KEY, suffix.ToString());
                //assert previousValue == null;

                // TODO: we should only provide the "slice" of FIS
                // that this PF actually sees ... then stuff like
                // .hasProx could work correctly?
                // NOTE: .hasProx is already broken in the same way for the non-perfield case,
                // if there is a fieldinfo with prox that has no postings, you get a 0 byte file.
                return consumer.consumer.AddField(field);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    IOUtils.Close(formats.Values.ToArray());
                }
            }
        }

        internal static String GetSuffix(String formatName, String suffix)
        {
            return formatName + "_" + suffix;
        }

        internal static String GetFullSegmentSuffix(String fieldName, String outerSegmentSuffix, String segmentSuffix)
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
            private readonly IDictionary<String, FieldsProducer> fields = new HashMap<String, FieldsProducer>(); //.NET Port: what to do about treemap?
            private readonly IDictionary<String, FieldsProducer> formats = new HashMap<String, FieldsProducer>();

            public FieldsReader(SegmentReadState readState)
            {

                // Read _X.per and init each format:
                bool success = false;
                try
                {
                    // Read field name -> format name
                    foreach (FieldInfo fi in readState.fieldInfos)
                    {
                        if (fi.IsIndexed)
                        {
                            String fieldName = fi.name;
                            String formatName = fi.GetAttribute(PER_FIELD_FORMAT_KEY);
                            if (formatName != null)
                            {
                                // null formatName means the field is in fieldInfos, but has no postings!
                                String suffix = fi.GetAttribute(PER_FIELD_SUFFIX_KEY);
                                //assert suffix != null;
                                PostingsFormat format = PostingsFormat.ForName(formatName);
                                String segmentSuffix = GetSuffix(formatName, suffix);
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
                        IOUtils.CloseWhileHandlingException(formats.Values.Cast<IDisposable>().ToArray());
                    }
                }
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return fields.Keys.GetEnumerator();
            }

            public override Terms Terms(string field)
            {
                FieldsProducer fieldsProducer = fields[field];
                return fieldsProducer == null ? null : fieldsProducer.Terms(field);
            }

            public override int Size
            {
                get { return fields.Count; }
            }

            protected override void Dispose(bool disposing)
            {
                IOUtils.Close(formats.Values.ToArray());
            }
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            return new FieldsReader(state);
        }

        public abstract PostingsFormat GetPostingsFormatForField(String field);
    }
}
