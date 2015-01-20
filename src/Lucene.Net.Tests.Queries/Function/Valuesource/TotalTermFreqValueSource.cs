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
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// <code>TotalTermFreqValueSource</code> returns the total term freq
	/// (sum of term freqs across all documents).
	/// </summary>
	/// <remarks>
	/// <code>TotalTermFreqValueSource</code> returns the total term freq
	/// (sum of term freqs across all documents).
	/// Returns -1 if frequencies were omitted for the field, or if
	/// the codec doesn't support this statistic.
	/// </remarks>
	/// <lucene.internal></lucene.internal>
	public class TotalTermFreqValueSource : ValueSource
	{
		protected internal readonly string field;

		protected internal readonly string indexedField;

		protected internal readonly string val;

		protected internal readonly BytesRef indexedBytes;

		public TotalTermFreqValueSource(string field, string val, string indexedField, BytesRef
			 indexedBytes)
		{
			this.field = field;
			this.val = val;
			this.indexedField = indexedField;
			this.indexedBytes = indexedBytes;
		}

		public virtual string Name()
		{
			return "totaltermfreq";
		}

		public override string Description()
		{
			return Name() + '(' + field + ',' + val + ')';
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
			long totalTermFreq = 0;
			foreach (AtomicReaderContext readerContext in searcher.GetTopReaderContext().Leaves
				())
			{
				long val = ((AtomicReader)readerContext.Reader()).TotalTermFreq(new Term(indexedField
					, indexedBytes));
				if (val == -1)
				{
					totalTermFreq = -1;
					break;
				}
				else
				{
					totalTermFreq += val;
				}
			}
			long ttf = totalTermFreq;
			context.Put(this, new _LongDocValues_78(ttf, this));
		}

		private sealed class _LongDocValues_78 : LongDocValues
		{
			public _LongDocValues_78(long ttf, ValueSource baseArg1) : base(baseArg1)
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
			return GetType().GetHashCode() + indexedField.GetHashCode() * 29 + indexedBytes.GetHashCode
				();
		}

		public override bool Equals(object o)
		{
			if (this.GetType() != o.GetType())
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.TotalTermFreqValueSource other = (
				Org.Apache.Lucene.Queries.Function.Valuesource.TotalTermFreqValueSource)o;
			return this.indexedField.Equals(other.indexedField) && this.indexedBytes.Equals(other
				.indexedBytes);
		}
	}
}
