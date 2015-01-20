/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.Text;
using Lucene.Net.Queryparser.Surround.Query;
using Sharpen;

namespace Lucene.Net.Queryparser.Surround.Query
{
	/// <summary>Forms an OR query of the provided query across multiple fields.</summary>
	/// <remarks>Forms an OR query of the provided query across multiple fields.</remarks>
	public class FieldsQuery : SrndQuery
	{
		private SrndQuery q;

		private IList<string> fieldNames;

		private readonly char fieldOp;

		private readonly string OrOperatorName = "OR";

		public FieldsQuery(SrndQuery q, IList<string> fieldNames, char fieldOp)
		{
			this.q = q;
			this.fieldNames = fieldNames;
			this.fieldOp = fieldOp;
		}

		public FieldsQuery(SrndQuery q, string fieldName, char fieldOp)
		{
			this.q = q;
			fieldNames = new AList<string>();
			fieldNames.AddItem(fieldName);
			this.fieldOp = fieldOp;
		}

		public override bool IsFieldsSubQueryAcceptable()
		{
			return false;
		}

		public virtual Lucene.Net.Search.Query MakeLuceneQueryNoBoost(BasicQueryFactory
			 qf)
		{
			if (fieldNames.Count == 1)
			{
				return q.MakeLuceneQueryFieldNoBoost(fieldNames[0], qf);
			}
			else
			{
				IList<SrndQuery> queries = new AList<SrndQuery>();
				Iterator<string> fni = GetFieldNames().ListIterator();
				SrndQuery qc;
				while (fni.HasNext())
				{
					qc = q.Clone();
					queries.AddItem(new Lucene.Net.Queryparser.Surround.Query.FieldsQuery(qc, 
						fni.Next(), fieldOp));
				}
				OrQuery oq = new OrQuery(queries, true, OrOperatorName);
				// System.out.println(getClass().toString() + ", fields expanded: " + oq.toString()); /* needs testing */
				return oq.MakeLuceneQueryField(null, qf);
			}
		}

		public override Lucene.Net.Search.Query MakeLuceneQueryFieldNoBoost(string
			 fieldName, BasicQueryFactory qf)
		{
			return MakeLuceneQueryNoBoost(qf);
		}

		public virtual IList<string> GetFieldNames()
		{
			return fieldNames;
		}

		public virtual char GetFieldOperator()
		{
			return fieldOp;
		}

		public override string ToString()
		{
			StringBuilder r = new StringBuilder();
			r.Append("(");
			FieldNamesToString(r);
			r.Append(q.ToString());
			r.Append(")");
			return r.ToString();
		}

		protected internal virtual void FieldNamesToString(StringBuilder r)
		{
			Iterator<string> fni = GetFieldNames().ListIterator();
			while (fni.HasNext())
			{
				r.Append(fni.Next());
				r.Append(GetFieldOperator());
			}
		}
	}
}
