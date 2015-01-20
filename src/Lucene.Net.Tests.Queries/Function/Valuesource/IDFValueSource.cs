/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Similarities;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// Function that returns
	/// <see cref="Org.Apache.Lucene.Search.Similarities.TFIDFSimilarity.Idf(long, long)"
	/// 	>Org.Apache.Lucene.Search.Similarities.TFIDFSimilarity.Idf(long, long)</see>
	/// for every document.
	/// <p>
	/// Note that the configured Similarity for the field must be
	/// a subclass of
	/// <see cref="Org.Apache.Lucene.Search.Similarities.TFIDFSimilarity">Org.Apache.Lucene.Search.Similarities.TFIDFSimilarity
	/// 	</see>
	/// </summary>
	/// <lucene.internal></lucene.internal>
	public class IDFValueSource : DocFreqValueSource
	{
		public IDFValueSource(string field, string val, string indexedField, BytesRef indexedBytes
			) : base(field, val, indexedField, indexedBytes)
		{
		}

		public override string Name()
		{
			return "idf";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			IndexSearcher searcher = (IndexSearcher)context.Get("searcher");
			TFIDFSimilarity sim = AsTFIDF(searcher.GetSimilarity(), field);
			if (sim == null)
			{
				throw new NotSupportedException("requires a TFIDFSimilarity (such as DefaultSimilarity)"
					);
			}
			int docfreq = searcher.GetIndexReader().DocFreq(new Term(indexedField, indexedBytes
				));
			float idf = sim.Idf(docfreq, searcher.GetIndexReader().MaxDoc());
			return new ConstDoubleDocValues(idf, this);
		}

		// tries extra hard to cast the sim to TFIDFSimilarity
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
