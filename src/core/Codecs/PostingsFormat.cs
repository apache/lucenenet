using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs
{
    public abstract class PostingsFormat : NamedSPILoader.NamedSPI
    {
        private static readonly NamedSPILoader<PostingsFormat> loader =
            new NamedSPILoader<PostingsFormat>(typeof(PostingsFormat));

        /** Zero-length {@code PostingsFormat} array. */
        public static readonly PostingsFormat[] EMPTY = new PostingsFormat[0];

        /** Unique name that's used to retrieve this format when
         *  reading the index.
         */
        private readonly String name;

        protected PostingsFormat(string name)
        {
            NamedSPILoader.CheckServiceName(name);
            this.name = name;
        }

        public string Name
        {
            get { return name; }
        }

        public abstract FieldsConsumer FieldsConsumer(SegmentWriteState state);

        public abstract FieldsProducer FieldsProducer(SegmentReadState state);

        public override string ToString()
        {
            return "PostingsFormat(name=" + name + ")";
        }

        public static PostingsFormat ForName(String name)
        {
            if (loader == null)
            {
                throw new InvalidOperationException("You called PostingsFormat.forName() before all formats could be initialized. " +
                    "This likely happens if you call it from a PostingsFormat's ctor.");
            }
            return loader.Lookup(name);
        }

        public static ICollection<String> AvailablePostingsFormats
        {
            get
            {
                if (loader == null)
                {
                    throw new InvalidOperationException("You called PostingsFormat.availablePostingsFormats() before all formats could be initialized. " +
                        "This likely happens if you call it from a PostingsFormat's ctor.");
                }
                return loader.AvailableServices;
            }
        }

        public static void ReloadPostingsFormats()
        {
            loader.Reload();
        }
    }
}
