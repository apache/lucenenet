using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs
{
    public abstract class Codec : NamedSPILoader.NamedSPI
    {
        private static readonly NamedSPILoader<Codec> loader = new NamedSPILoader<Codec>(typeof(Codec));

        private readonly string name;

        protected Codec(string name)
        {
            NamedSPILoader.CheckServiceName(name);
            this.name = name;
        }

        public string Name
        {
            get { return name; }
        }

        /** Encodes/decodes postings */
        public abstract PostingsFormat PostingsFormat();

        /** Encodes/decodes docvalues */
        public abstract DocValuesFormat DocValuesFormat();

        /** Encodes/decodes stored fields */
        public abstract StoredFieldsFormat StoredFieldsFormat();

        /** Encodes/decodes term vectors */
        public abstract TermVectorsFormat TermVectorsFormat();

        /** Encodes/decodes field infos file */
        public abstract FieldInfosFormat FieldInfosFormat();

        /** Encodes/decodes segment info file */
        public abstract SegmentInfoFormat SegmentInfoFormat();

        /** Encodes/decodes document normalization values */
        public abstract NormsFormat NormsFormat();

        /** Encodes/decodes live docs */
        public abstract LiveDocsFormat LiveDocsFormat();

        public static Codec ForName(string name)
        {
            if (loader == null)
                throw new InvalidOperationException("You called Codec.forName() before all Codecs could be initialized. " +
                    "This likely happens if you call it from a Codec's ctor.");

            return loader.Lookup(name);
        }

        public static ICollection<string> AvailableCodecs()
        {
            if (loader == null)
            {
                throw new InvalidOperationException("You called Codec.availableCodecs() before all Codecs could be initialized. " +
                    "This likely happens if you call it from a Codec's ctor.");
            }
            return loader.AvailableServices;
        }

        public static void ReloadCodecs()
        {
            loader.Reload();
        }

        private static Codec defaultCodec = Codec.ForName("Lucene42");

        public static Codec Default
        {
            get { return defaultCodec; }
            set
            {
                defaultCodec = value;
            }
        }

        public override string ToString()
        {
            return name;
        }
    }
}
