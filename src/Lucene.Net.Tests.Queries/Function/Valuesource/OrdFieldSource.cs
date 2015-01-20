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
using Org.Apache.Lucene.Util.Mutable;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// Obtains the ordinal of the field value from the default Lucene
	/// <see cref="Org.Apache.Lucene.Search.FieldCache">Org.Apache.Lucene.Search.FieldCache
	/// 	</see>
	/// using getStringIndex().
	/// <br />
	/// The native lucene index order is used to assign an ordinal value for each field value.
	/// <br />Field values (terms) are lexicographically ordered by unicode value, and numbered starting at 1.
	/// <br />
	/// Example:<br />
	/// If there were only three field values: "apple","banana","pear"
	/// <br />then ord("apple")=1, ord("banana")=2, ord("pear")=3
	/// <p>
	/// WARNING: ord() depends on the position in an index and can thus change when other documents are inserted or deleted,
	/// or if a MultiSearcher is used.
	/// <br />WARNING: as of Solr 1.4, ord() and rord() can cause excess memory use since they must use a FieldCache entry
	/// at the top level reader, while sorting and function queries now use entries at the segment level.  Hence sorting
	/// or using a different function query, in addition to ord()/rord() will double memory use.
	/// </summary>
	public class OrdFieldSource : ValueSource
	{
		protected internal readonly string field;

		public OrdFieldSource(string field)
		{
			this.field = field;
		}

		public override string Description()
		{
			return "ord(" + field + ')';
		}

		// TODO: this is trappy? perhaps this query instead should make you pass a slow reader yourself?
		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			int off = readerContext.docBase;
			IndexReader topReader = ReaderUtil.GetTopLevelContext(readerContext).Reader();
			AtomicReader r = SlowCompositeReaderWrapper.Wrap(topReader);
			SortedDocValues sindex = FieldCache.DEFAULT.GetTermsIndex(r, field);
			return new _IntDocValues_75(sindex, off, this);
		}

		private sealed class _IntDocValues_75 : IntDocValues
		{
			public _IntDocValues_75(SortedDocValues sindex, int off, ValueSource baseArg1) : 
				base(baseArg1)
			{
				this.sindex = sindex;
				this.off = off;
			}

			protected internal string ToTerm(string readableValue)
			{
				return readableValue;
			}

			public override int IntVal(int doc)
			{
				return sindex.GetOrd(doc + off);
			}

			public override int OrdVal(int doc)
			{
				return sindex.GetOrd(doc + off);
			}

			public override int NumOrd()
			{
				return sindex.GetValueCount();
			}

			public override bool Exists(int doc)
			{
				return sindex.GetOrd(doc + off) != 0;
			}

			public override FunctionValues.ValueFiller GetValueFiller()
			{
				return new _ValueFiller_99(sindex);
			}

			private sealed class _ValueFiller_99 : FunctionValues.ValueFiller
			{
				public _ValueFiller_99(SortedDocValues sindex)
				{
					this.sindex = sindex;
					this.mval = new MutableValueInt();
				}

				private readonly MutableValueInt mval;

				public override MutableValue GetValue()
				{
					return this.mval;
				}

				public override void FillValue(int doc)
				{
					this.mval.value = sindex.GetOrd(doc);
					this.mval.exists = this.mval.value != 0;
				}

				private readonly SortedDocValues sindex;
			}

			private readonly SortedDocValues sindex;

			private readonly int off;
		}

		public override bool Equals(object o)
		{
			return o != null && o.GetType() == typeof(Org.Apache.Lucene.Queries.Function.Valuesource.OrdFieldSource
				) && this.field.Equals(((Org.Apache.Lucene.Queries.Function.Valuesource.OrdFieldSource
				)o).field);
		}

		private static readonly int hcode = typeof(Org.Apache.Lucene.Queries.Function.Valuesource.OrdFieldSource
			).GetHashCode();

		public override int GetHashCode()
		{
			return hcode + field.GetHashCode();
		}
	}
}
