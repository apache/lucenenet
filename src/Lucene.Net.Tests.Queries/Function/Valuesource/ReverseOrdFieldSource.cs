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
	/// <summary>
	/// Obtains the ordinal of the field value from the default Lucene
	/// <see cref="Org.Apache.Lucene.Search.FieldCache">Org.Apache.Lucene.Search.FieldCache
	/// 	</see>
	/// using getTermsIndex()
	/// and reverses the order.
	/// <br />
	/// The native lucene index order is used to assign an ordinal value for each field value.
	/// <br />Field values (terms) are lexicographically ordered by unicode value, and numbered starting at 1.
	/// <br />
	/// Example of reverse ordinal (rord):<br />
	/// If there were only three field values: "apple","banana","pear"
	/// <br />then rord("apple")=3, rord("banana")=2, ord("pear")=1
	/// <p>
	/// WARNING: ord() depends on the position in an index and can thus change when other documents are inserted or deleted,
	/// or if a MultiSearcher is used.
	/// <br />
	/// WARNING: as of Solr 1.4, ord() and rord() can cause excess memory use since they must use a FieldCache entry
	/// at the top level reader, while sorting and function queries now use entries at the segment level.  Hence sorting
	/// or using a different function query, in addition to ord()/rord() will double memory use.
	/// </summary>
	public class ReverseOrdFieldSource : ValueSource
	{
		public readonly string field;

		public ReverseOrdFieldSource(string field)
		{
			this.field = field;
		}

		public override string Description()
		{
			return "rord(" + field + ')';
		}

		// TODO: this is trappy? perhaps this query instead should make you pass a slow reader yourself?
		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			IndexReader topReader = ReaderUtil.GetTopLevelContext(readerContext).Reader();
			AtomicReader r = SlowCompositeReaderWrapper.Wrap(topReader);
			int off = readerContext.docBase;
			SortedDocValues sindex = FieldCache.DEFAULT.GetTermsIndex(r, field);
			int end = sindex.GetValueCount();
			return new _IntDocValues_78(end, sindex, off, this);
		}

		private sealed class _IntDocValues_78 : IntDocValues
		{
			public _IntDocValues_78(int end, SortedDocValues sindex, int off, ValueSource baseArg1
				) : base(baseArg1)
			{
				this.end = end;
				this.sindex = sindex;
				this.off = off;
			}

			public override int IntVal(int doc)
			{
				return (end - sindex.GetOrd(doc + off) - 1);
			}

			private readonly int end;

			private readonly SortedDocValues sindex;

			private readonly int off;
		}

		public override bool Equals(object o)
		{
			if (o == null || (o.GetType() != typeof(Org.Apache.Lucene.Queries.Function.Valuesource.ReverseOrdFieldSource
				)))
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.ReverseOrdFieldSource other = (Org.Apache.Lucene.Queries.Function.Valuesource.ReverseOrdFieldSource
				)o;
			return this.field.Equals(other.field);
		}

		private static readonly int hcode = typeof(Org.Apache.Lucene.Queries.Function.Valuesource.ReverseOrdFieldSource
			).GetHashCode();

		public override int GetHashCode()
		{
			return hcode + field.GetHashCode();
		}
	}
}
