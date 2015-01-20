/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Docvalues;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Org.Apache.Lucene.Util.Mutable;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Docvalues
{
	/// <summary>Serves as base class for FunctionValues based on DocTermsIndex.</summary>
	/// <remarks>Serves as base class for FunctionValues based on DocTermsIndex.</remarks>
	/// <lucene.internal></lucene.internal>
	public abstract class DocTermsIndexDocValues : FunctionValues
	{
		protected internal readonly SortedDocValues termsIndex;

		protected internal readonly ValueSource vs;

		protected internal readonly MutableValueStr val = new MutableValueStr();

		protected internal readonly BytesRef spare = new BytesRef();

		protected internal readonly CharsRef spareChars = new CharsRef();

		/// <exception cref="System.IO.IOException"></exception>
		public DocTermsIndexDocValues(ValueSource vs, AtomicReaderContext context, string
			 field)
		{
			try
			{
				termsIndex = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader)context.Reader()), field
					);
			}
			catch (RuntimeException e)
			{
				throw new DocTermsIndexDocValues.DocTermsIndexException(field, e);
			}
			this.vs = vs;
		}

		protected internal abstract string ToTerm(string readableValue);

		public override bool Exists(int doc)
		{
			return OrdVal(doc) >= 0;
		}

		public override int OrdVal(int doc)
		{
			return termsIndex.GetOrd(doc);
		}

		public override int NumOrd()
		{
			return termsIndex.GetValueCount();
		}

		public override bool BytesVal(int doc, BytesRef target)
		{
			termsIndex.Get(doc, target);
			return target.length > 0;
		}

		public override string StrVal(int doc)
		{
			termsIndex.Get(doc, spare);
			if (spare.length == 0)
			{
				return null;
			}
			UnicodeUtil.UTF8toUTF16(spare, spareChars);
			return spareChars.ToString();
		}

		public override bool BoolVal(int doc)
		{
			return Exists(doc);
		}

		public abstract override object ObjectVal(int doc);

		// force subclasses to override
		public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal
			, string upperVal, bool includeLower, bool includeUpper)
		{
			// TODO: are lowerVal and upperVal in indexed form or not?
			lowerVal = lowerVal == null ? null : ToTerm(lowerVal);
			upperVal = upperVal == null ? null : ToTerm(upperVal);
			int lower = int.MinValue;
			if (lowerVal != null)
			{
				lower = termsIndex.LookupTerm(new BytesRef(lowerVal));
				if (lower < 0)
				{
					lower = -lower - 1;
				}
				else
				{
					if (!includeLower)
					{
						lower++;
					}
				}
			}
			int upper = int.MaxValue;
			if (upperVal != null)
			{
				upper = termsIndex.LookupTerm(new BytesRef(upperVal));
				if (upper < 0)
				{
					upper = -upper - 2;
				}
				else
				{
					if (!includeUpper)
					{
						upper--;
					}
				}
			}
			int ll = lower;
			int uu = upper;
			return new _ValueSourceScorer_125(this, ll, uu, reader, this);
		}

		private sealed class _ValueSourceScorer_125 : ValueSourceScorer
		{
			public _ValueSourceScorer_125(DocTermsIndexDocValues _enclosing, int ll, int uu, 
				IndexReader baseArg1, FunctionValues baseArg2) : base(baseArg1, baseArg2)
			{
				this._enclosing = _enclosing;
				this.ll = ll;
				this.uu = uu;
			}

			public override bool MatchesValue(int doc)
			{
				int ord = this._enclosing.termsIndex.GetOrd(doc);
				return ord >= ll && ord <= uu;
			}

			private readonly DocTermsIndexDocValues _enclosing;

			private readonly int ll;

			private readonly int uu;
		}

		public override string ToString(int doc)
		{
			return vs.Description() + '=' + StrVal(doc);
		}

		public override FunctionValues.ValueFiller GetValueFiller()
		{
			return new _ValueFiller_141(this);
		}

		private sealed class _ValueFiller_141 : FunctionValues.ValueFiller
		{
			public _ValueFiller_141(DocTermsIndexDocValues _enclosing)
			{
				this._enclosing = _enclosing;
				this.mval = new MutableValueStr();
			}

			private readonly MutableValueStr mval;

			public override MutableValue GetValue()
			{
				return this.mval;
			}

			public override void FillValue(int doc)
			{
				int ord = this._enclosing.termsIndex.GetOrd(doc);
				if (ord == -1)
				{
					this.mval.value.bytes = BytesRef.EMPTY_BYTES;
					this.mval.value.offset = 0;
					this.mval.value.length = 0;
					this.mval.exists = false;
				}
				else
				{
					this._enclosing.termsIndex.LookupOrd(ord, this.mval.value);
					this.mval.exists = true;
				}
			}

			private readonly DocTermsIndexDocValues _enclosing;
		}

		/// <summary>Custom Exception to be thrown when the DocTermsIndex for a field cannot be generated
		/// 	</summary>
		[System.Serializable]
		public sealed class DocTermsIndexException : RuntimeException
		{
			public DocTermsIndexException(string fieldName, RuntimeException cause) : base("Can't initialize DocTermsIndex to generate (function) FunctionValues for field: "
				 + fieldName, cause)
			{
			}
		}
	}
}
