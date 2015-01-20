/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using System.IO;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Docvalues;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Org.Apache.Lucene.Util;
using Org.Apache.Lucene.Util.Packed;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>Use a field value and find the Document Frequency within another field.</summary>
	/// <remarks>Use a field value and find the Document Frequency within another field.</remarks>
	/// <since>solr 4.0</since>
	public class JoinDocFreqValueSource : FieldCacheSource
	{
		public static readonly string NAME = "joindf";

		protected internal readonly string qfield;

		public JoinDocFreqValueSource(string field, string qfield) : base(field)
		{
			this.qfield = qfield;
		}

		public override string Description()
		{
			return NAME + "(" + field + ":(" + qfield + "))";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			BinaryDocValues terms = cache.GetTerms(((AtomicReader)readerContext.Reader()), field
				, false, PackedInts.FAST);
			IndexReader top = ReaderUtil.GetTopLevelContext(readerContext).Reader();
			Terms t = MultiFields.GetTerms(top, qfield);
			TermsEnum termsEnum = t == null ? TermsEnum.EMPTY : t.Iterator(null);
			return new _IntDocValues_64(this, terms, termsEnum, this);
		}

		private sealed class _IntDocValues_64 : IntDocValues
		{
			public _IntDocValues_64(JoinDocFreqValueSource _enclosing, BinaryDocValues terms, 
				TermsEnum termsEnum, ValueSource baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.terms = terms;
				this.termsEnum = termsEnum;
				this.@ref = new BytesRef();
			}

			internal readonly BytesRef @ref;

			public override int IntVal(int doc)
			{
				try
				{
					terms.Get(doc, this.@ref);
					if (termsEnum.SeekExact(this.@ref))
					{
						return termsEnum.DocFreq();
					}
					else
					{
						return 0;
					}
				}
				catch (IOException e)
				{
					throw new RuntimeException("caught exception in function " + this._enclosing.Description
						() + " : doc=" + doc, e);
				}
			}

			private readonly JoinDocFreqValueSource _enclosing;

			private readonly BinaryDocValues terms;

			private readonly TermsEnum termsEnum;
		}

		public override bool Equals(object o)
		{
			if (o.GetType() != typeof(Org.Apache.Lucene.Queries.Function.Valuesource.JoinDocFreqValueSource
				))
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.JoinDocFreqValueSource other = (Org.Apache.Lucene.Queries.Function.Valuesource.JoinDocFreqValueSource
				)o;
			if (!qfield.Equals(other.qfield))
			{
				return false;
			}
			return base.Equals(other);
		}

		public override int GetHashCode()
		{
			return qfield.GetHashCode() + base.GetHashCode();
		}
	}
}
