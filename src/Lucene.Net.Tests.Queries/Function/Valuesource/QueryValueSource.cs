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
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Org.Apache.Lucene.Util.Mutable;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary><code>QueryValueSource</code> returns the relevance score of the query</summary>
	public class QueryValueSource : ValueSource
	{
		internal readonly Query q;

		internal readonly float defVal;

		public QueryValueSource(Query q, float defVal)
		{
			this.q = q;
			this.defVal = defVal;
		}

		public virtual Query GetQuery()
		{
			return q;
		}

		public virtual float GetDefaultValue()
		{
			return defVal;
		}

		public override string Description()
		{
			return "query(" + q + ",def=" + defVal + ")";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary fcontext, AtomicReaderContext
			 readerContext)
		{
			return new QueryDocValues(this, readerContext, fcontext);
		}

		public override int GetHashCode()
		{
			return q.GetHashCode() * 29;
		}

		public override bool Equals(object o)
		{
			if (typeof(Org.Apache.Lucene.Queries.Function.Valuesource.QueryValueSource) != o.
				GetType())
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.QueryValueSource other = (Org.Apache.Lucene.Queries.Function.Valuesource.QueryValueSource
				)o;
			return this.q.Equals(other.q) && this.defVal == other.defVal;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
			Weight w = searcher.CreateNormalizedWeight(q);
			context.Put(this, w);
		}
	}

	internal class QueryDocValues : FloatDocValues
	{
		internal readonly AtomicReaderContext readerContext;

		internal readonly Bits acceptDocs;

		internal readonly Weight weight;

		internal readonly float defVal;

		internal readonly IDictionary fcontext;

		internal readonly Query q;

		internal Scorer scorer;

		internal int scorerDoc;

		internal bool noMatches = false;

		internal int lastDocRequested = int.MaxValue;

		/// <exception cref="System.IO.IOException"></exception>
		public QueryDocValues(QueryValueSource vs, AtomicReaderContext readerContext, IDictionary
			 fcontext) : base(vs)
		{
			// the document the scorer is on
			// the last document requested... start off with high value
			// to trigger a scorer reset on first access.
			this.readerContext = readerContext;
			this.acceptDocs = ((AtomicReader)readerContext.Reader()).GetLiveDocs();
			this.defVal = vs.defVal;
			this.q = vs.q;
			this.fcontext = fcontext;
			Weight w = fcontext == null ? null : (Weight)fcontext.Get(vs);
			if (w == null)
			{
				IndexSearcher weightSearcher;
				if (fcontext == null)
				{
					weightSearcher = new IndexSearcher(ReaderUtil.GetTopLevelContext(readerContext));
				}
				else
				{
					weightSearcher = (IndexSearcher)fcontext.Get("searcher");
					if (weightSearcher == null)
					{
						weightSearcher = new IndexSearcher(ReaderUtil.GetTopLevelContext(readerContext));
					}
				}
				vs.CreateWeight(fcontext, weightSearcher);
				w = (Weight)fcontext.Get(vs);
			}
			weight = w;
		}

		public override float FloatVal(int doc)
		{
			try
			{
				if (doc < lastDocRequested)
				{
					if (noMatches)
					{
						return defVal;
					}
					scorer = weight.Scorer(readerContext, acceptDocs);
					if (scorer == null)
					{
						noMatches = true;
						return defVal;
					}
					scorerDoc = -1;
				}
				lastDocRequested = doc;
				if (scorerDoc < doc)
				{
					scorerDoc = scorer.Advance(doc);
				}
				if (scorerDoc > doc)
				{
					// query doesn't match this document... either because we hit the
					// end, or because the next doc is after this doc.
					return defVal;
				}
				// a match!
				return scorer.Score();
			}
			catch (IOException e)
			{
				throw new RuntimeException("caught exception in QueryDocVals(" + q + ") doc=" + doc
					, e);
			}
		}

		public override bool Exists(int doc)
		{
			try
			{
				if (doc < lastDocRequested)
				{
					if (noMatches)
					{
						return false;
					}
					scorer = weight.Scorer(readerContext, acceptDocs);
					scorerDoc = -1;
					if (scorer == null)
					{
						noMatches = true;
						return false;
					}
				}
				lastDocRequested = doc;
				if (scorerDoc < doc)
				{
					scorerDoc = scorer.Advance(doc);
				}
				if (scorerDoc > doc)
				{
					// query doesn't match this document... either because we hit the
					// end, or because the next doc is after this doc.
					return false;
				}
				// a match!
				return true;
			}
			catch (IOException e)
			{
				throw new RuntimeException("caught exception in QueryDocVals(" + q + ") doc=" + doc
					, e);
			}
		}

		public override object ObjectVal(int doc)
		{
			try
			{
				return Exists(doc) ? scorer.Score() : null;
			}
			catch (IOException e)
			{
				throw new RuntimeException("caught exception in QueryDocVals(" + q + ") doc=" + doc
					, e);
			}
		}

		public override FunctionValues.ValueFiller GetValueFiller()
		{
			//
			// TODO: if we want to support more than one value-filler or a value-filler in conjunction with
			// the FunctionValues, then members like "scorer" should be per ValueFiller instance.
			// Or we can say that the user should just instantiate multiple FunctionValues.
			//
			return new _ValueFiller_199(this);
		}

		private sealed class _ValueFiller_199 : FunctionValues.ValueFiller
		{
			public _ValueFiller_199(QueryDocValues _enclosing)
			{
				this._enclosing = _enclosing;
				this.mval = new MutableValueFloat();
			}

			private readonly MutableValueFloat mval;

			public override MutableValue GetValue()
			{
				return this.mval;
			}

			public override void FillValue(int doc)
			{
				try
				{
					if (this._enclosing.noMatches)
					{
						this.mval.value = this._enclosing.defVal;
						this.mval.exists = false;
						return;
					}
					this._enclosing.scorer = this._enclosing.weight.Scorer(this._enclosing.readerContext
						, this._enclosing.acceptDocs);
					this._enclosing.scorerDoc = -1;
					if (this._enclosing.scorer == null)
					{
						this._enclosing.noMatches = true;
						this.mval.value = this._enclosing.defVal;
						this.mval.exists = false;
						return;
					}
					this._enclosing.lastDocRequested = doc;
					if (this._enclosing.scorerDoc < doc)
					{
						this._enclosing.scorerDoc = this._enclosing.scorer.Advance(doc);
					}
					if (this._enclosing.scorerDoc > doc)
					{
						// query doesn't match this document... either because we hit the
						// end, or because the next doc is after this doc.
						this.mval.value = this._enclosing.defVal;
						this.mval.exists = false;
						return;
					}
					// a match!
					this.mval.value = this._enclosing.scorer.Score();
					this.mval.exists = true;
				}
				catch (IOException e)
				{
					throw new RuntimeException("caught exception in QueryDocVals(" + this._enclosing.
						q + ") doc=" + doc, e);
				}
			}

			private readonly QueryDocValues _enclosing;
		}

		public override string ToString(int doc)
		{
			return "query(" + q + ",def=" + defVal + ")=" + FloatVal(doc);
		}
	}
}
