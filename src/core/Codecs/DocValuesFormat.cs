using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs
{
    public abstract class DocValuesFormat : NamedSPILoader.NamedSPI
    {
        private static readonly NamedSPILoader<DocValuesFormat> loader =
            new NamedSPILoader<DocValuesFormat>(typeof(DocValuesFormat));

        private readonly String name;

        protected DocValuesFormat(String name)
        {
            NamedSPILoader.CheckServiceName(name);
            this.name = name;
        }

        public abstract DocValuesConsumer FieldsConsumer(SegmentWriteState state);

        public abstract DocValuesProducer FieldsProducer(SegmentReadState state);

        public string Name
        {
            get { return name; }
        }

        public override string ToString()
        {
            return "DocValuesFormat(name=" + name + ")";
        }

        public static DocValuesFormat ForName(String name)
        {
            if (loader == null)
            {
                throw new InvalidOperationException("You called DocValuesFormat.forName() before all formats could be initialized. " +
                    "This likely happens if you call it from a DocValuesFormat's ctor.");
            }
            return loader.Lookup(name);
        }

        public static ICollection<String> AvailableDocValuesFormats
        {
            get
            {
                if (loader == null)
                {
                    throw new InvalidOperationException("You called DocValuesFormat.availableDocValuesFormats() before all formats could be initialized. " +
                        "This likely happens if you call it from a DocValuesFormat's ctor.");
                }
                return loader.AvailableServices;
            }
        }

        public static void ReloadDocValuesFormats()
        {
            loader.Reload();
        }
    }
}
