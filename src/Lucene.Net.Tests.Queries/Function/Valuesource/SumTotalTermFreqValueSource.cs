/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Docvalues;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary><code>SumTotalTermFreqValueSource</code> returns the number of tokens.</summary>
	/// <remarks>
	/// <code>SumTotalTermFreqValueSource</code> returns the number of tokens.
	/// (sum of term freqs across all documents, across all terms).
	/// Returns -1 if frequencies were omitted for the field, or if
	/// the codec doesn't support this statistic.
	/// </remarks>
	/// <lucene.internal></lucene.internal>
	public class SumTotalTermFreqValueSource : ValueSource
	{
		protected internal readonly string indexedField;

		public SumTotalTermFreqValueSource(string indexedField)
		{
			this.indexedField = indexedField;
		}

		public virtual string Name()
		{
			return "sumtotaltermfreq";
		}

		public override string Description()
		{
			return Name() + '(' + indexedField + ')';
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			return (FunctionValues)context.Get(this);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
			long sumTotalTermFreq = 0;
			foreach (AtomicReaderContext readerContext in searcher.GetTopReaderContext().Leaves
				())
			{
				Fields fields = ((AtomicReader)readerContext.Reader()).Fields();
				if (fields == null)
				{
					continue;
				}
				Terms terms = fields.Terms(indexedField);
				if (terms == null)
				{
					continue;
				}
				long v = terms.GetSumTotalTermFreq();
				if (v == -1)
				{
					sumTotalTermFreq = -1;
					break;
				}
				else
				{
					sumTotalTermFreq += v;
				}
			}
			long ttf = sumTotalTermFreq;
			context.Put(this, new _LongDocValues_77(ttf, this));
		}

		private sealed class _LongDocValues_77 : LongDocValues
		{
			public _LongDocValues_77(long ttf, ValueSource baseArg1) : base(baseArg1)
			{
				this.ttf = ttf;
			}

			public override long LongVal(int doc)
			{
				return ttf;
			}

			private readonly long ttf;
		}

		public override int GetHashCode()
		{
			return GetType().GetHashCode() + indexedField.GetHashCode();
		}

		public override bool Equals(object o)
		{
			if (this.GetType() != o.GetType())
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.SumTotalTermFreqValueSource other = 
				(Org.Apache.Lucene.Queries.Function.Valuesource.SumTotalTermFreqValueSource)o;
			return this.indexedField.Equals(other.indexedField);
		}
	}
}
