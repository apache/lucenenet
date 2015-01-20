/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Docvalues;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	internal class ConstIntDocValues : IntDocValues
	{
		internal readonly int ival;

		internal readonly float fval;

		internal readonly double dval;

		internal readonly long lval;

		internal readonly string sval;

		internal readonly ValueSource parent;

		internal ConstIntDocValues(int val, ValueSource parent) : base(parent)
		{
			ival = val;
			fval = val;
			dval = val;
			lval = val;
			sval = Sharpen.Extensions.ToString(val);
			this.parent = parent;
		}

		public override float FloatVal(int doc)
		{
			return fval;
		}

		public override int IntVal(int doc)
		{
			return ival;
		}

		public override long LongVal(int doc)
		{
			return lval;
		}

		public override double DoubleVal(int doc)
		{
			return dval;
		}

		public override string StrVal(int doc)
		{
			return sval;
		}

		public override string ToString(int doc)
		{
			return parent.Description() + '=' + sval;
		}
	}

	internal class ConstDoubleDocValues : DoubleDocValues
	{
		internal readonly int ival;

		internal readonly float fval;

		internal readonly double dval;

		internal readonly long lval;

		internal readonly string sval;

		internal readonly ValueSource parent;

		internal ConstDoubleDocValues(double val, ValueSource parent) : base(parent)
		{
			ival = (int)val;
			fval = (float)val;
			dval = val;
			lval = (long)val;
			sval = double.ToString(val);
			this.parent = parent;
		}

		public override float FloatVal(int doc)
		{
			return fval;
		}

		public override int IntVal(int doc)
		{
			return ival;
		}

		public override long LongVal(int doc)
		{
			return lval;
		}

		public override double DoubleVal(int doc)
		{
			return dval;
		}

		public override string StrVal(int doc)
		{
			return sval;
		}

		public override string ToString(int doc)
		{
			return parent.Description() + '=' + sval;
		}
	}

	/// <summary><code>DocFreqValueSource</code> returns the number of documents containing the term.
	/// 	</summary>
	/// <remarks><code>DocFreqValueSource</code> returns the number of documents containing the term.
	/// 	</remarks>
	/// <lucene.internal></lucene.internal>
	public class DocFreqValueSource : ValueSource
	{
		protected internal readonly string field;

		protected internal readonly string indexedField;

		protected internal readonly string val;

		protected internal readonly BytesRef indexedBytes;

		public DocFreqValueSource(string field, string val, string indexedField, BytesRef
			 indexedBytes)
		{
			this.field = field;
			this.val = val;
			this.indexedField = indexedField;
			this.indexedBytes = indexedBytes;
		}

		public virtual string Name()
		{
			return "docfreq";
		}

		public override string Description()
		{
			return Name() + '(' + field + ',' + val + ')';
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			IndexSearcher searcher = (IndexSearcher)context.Get("searcher");
			int docfreq = searcher.GetIndexReader().DocFreq(new Term(indexedField, indexedBytes
				));
			return new ConstIntDocValues(docfreq, this);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
			context.Put("searcher", searcher);
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
			Org.Apache.Lucene.Queries.Function.Valuesource.DocFreqValueSource other = (Org.Apache.Lucene.Queries.Function.Valuesource.DocFreqValueSource
				)o;
			return this.indexedField.Equals(other.indexedField) && this.indexedBytes.Equals(other
				.indexedBytes);
		}
	}
}
