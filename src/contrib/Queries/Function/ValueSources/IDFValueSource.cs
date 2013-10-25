using Lucene.Net.Index;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class IDFValueSource : DocFreqValueSource
    {
        public IDFValueSource(string field, string val, string indexedField, BytesRef indexedBytes)
            : base(field, val, indexedField, indexedBytes)
        {
        }

        public override string Name
        {
            get
            {
                return "idf";
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            IndexSearcher searcher = (IndexSearcher)context["searcher"];
            TFIDFSimilarity sim = AsTFIDF(searcher.Similarity, field);
            if (sim == null)
            {
                throw new NotSupportedException("requires a TFIDFSimilarity (such as DefaultSimilarity)");
            }

            int docfreq = searcher.IndexReader.DocFreq(new Term(indexedField, indexedBytes));
            float idf = sim.Idf(docfreq, searcher.IndexReader.MaxDoc);
            return new ConstDoubleDocValues(idf, this);
        }

        internal static TFIDFSimilarity AsTFIDF(Similarity sim, string field)
        {
            while (sim is PerFieldSimilarityWrapper)
            {
                sim = ((PerFieldSimilarityWrapper)sim).Get(field);
            }

            if (sim is TFIDFSimilarity)
            {
                return (TFIDFSimilarity)sim;
            }
            else
            {
                return null;
            }
        }
    }
}
