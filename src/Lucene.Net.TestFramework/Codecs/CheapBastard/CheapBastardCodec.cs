using Lucene.Net.Codecs.DiskDV;
using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Codecs.Lucene46;

namespace Lucene.Net.Codecs.CheapBastard
{
    /// <summary>
    /// Codec that tries to use as little ram as possible because he spent all his money on beer
    /// </summary>
    // TODO: better name :) 
    // but if we named it "LowMemory" in codecs/ package, it would be irresistible like optimize()!
    public class CheapBastardCodec : FilterCodec
    {
        // TODO: would be better to have no terms index at all and bsearch a terms dict
        private readonly PostingsFormat postings = new Lucene41PostingsFormat(100, 200);
        // uncompressing versions, waste lots of disk but no ram
        private readonly StoredFieldsFormat storedFields = new Lucene40StoredFieldsFormat();
        private readonly TermVectorsFormat termVectors = new Lucene40TermVectorsFormat();
        // these go to disk for all docvalues/norms datastructures
        private readonly DocValuesFormat docValues = new DiskDocValuesFormat();
        private readonly NormsFormat norms = new DiskNormsFormat();

        public CheapBastardCodec()
            : base(new Lucene46Codec())
        {
        }

        public override PostingsFormat PostingsFormat
        {
            get { return postings; }
        }

        public override DocValuesFormat DocValuesFormat
        {
            get { return docValues; }
        }

        public override NormsFormat NormsFormat
        {
            get { return norms; }
        }

        public override StoredFieldsFormat StoredFieldsFormat
        {
            get { return storedFields; }
        }

        public override TermVectorsFormat TermVectorsFormat
        {
            get { return termVectors; }
        }
    }
}
