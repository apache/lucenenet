/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Docvalues;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Similarities;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// Function that returns
	/// <see cref="Org.Apache.Lucene.Search.Similarities.TFIDFSimilarity.DecodeNormValue(long)
	/// 	">Org.Apache.Lucene.Search.Similarities.TFIDFSimilarity.DecodeNormValue(long)</see>
	/// for every document.
	/// <p>
	/// Note that the configured Similarity for the field must be
	/// a subclass of
	/// <see cref="Org.Apache.Lucene.Search.Similarities.TFIDFSimilarity">Org.Apache.Lucene.Search.Similarities.TFIDFSimilarity
	/// 	</see>
	/// </summary>
	/// <lucene.internal></lucene.internal>
	public class NormValueSource : ValueSource
	{
		protected internal readonly string field;

		public NormValueSource(string field)
		{
			this.field = field;
		}

		public virtual string Name()
		{
			return "norm";
		}

		public override string Description()
		{
			return Name() + '(' + field + ')';
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
			context.Put("searcher", searcher);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			IndexSearcher searcher = (IndexSearcher)context.Get("searcher");
			TFIDFSimilarity similarity = IDFValueSource.AsTFIDF(searcher.GetSimilarity(), field
				);
			if (similarity == null)
			{
				throw new NotSupportedException("requires a TFIDFSimilarity (such as DefaultSimilarity)"
					);
			}
			NumericDocValues norms = ((AtomicReader)readerContext.Reader()).GetNormValues(field
				);
			if (norms == null)
			{
				return new ConstDoubleDocValues(0.0, this);
			}
			return new _FloatDocValues_71(similarity, norms, this);
		}

		private sealed class _FloatDocValues_71 : FloatDocValues
		{
			public _FloatDocValues_71(TFIDFSimilarity similarity, NumericDocValues norms, ValueSource
				 baseArg1) : base(baseArg1)
			{
				this.similarity = similarity;
				this.norms = norms;
			}

			public override float FloatVal(int doc)
			{
				return similarity.DecodeNormValue(norms.Get(doc));
			}

			private readonly TFIDFSimilarity similarity;

			private readonly NumericDocValues norms;
		}

		public override bool Equals(object o)
		{
			if (this.GetType() != o.GetType())
			{
				return false;
			}
			return this.field.Equals(((Org.Apache.Lucene.Queries.Function.Valuesource.NormValueSource
				)o).field);
		}

		public override int GetHashCode()
		{
			return this.GetType().GetHashCode() + field.GetHashCode();
		}
	}
}
