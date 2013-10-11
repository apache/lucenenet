using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    public class FieldsQuery : SrndQuery
    {
        private SrndQuery q;
        private IList<string> fieldNames;
        private readonly char fieldOp;
        private readonly string OrOperatorName = "OR"; /* for expanded queries, not normally visible */

        public FieldsQuery(SrndQuery q, IList<string> fieldNames, char fieldOp)
        {
            this.q = q;
            this.fieldNames = fieldNames;
            this.fieldOp = fieldOp;
        }

        public FieldsQuery(SrndQuery q, string fieldName, char fieldOp)
        {
            this.q = q;
            fieldNames = new List<string>();
            fieldNames.Add(fieldName);
            this.fieldOp = fieldOp;
        }

        public override bool IsFieldsSubQueryAcceptable
        {
            get
            {
                return false;
            }
        }

        public Search.Query MakeLuceneQueryNoBoost(BasicQueryFactory qf)
        {
            if (fieldNames.Count == 1)
            { 
                /* single field name: no new queries needed */
                return q.MakeLuceneQueryFieldNoBoost(fieldNames[0], qf);
            }
            else
            { 
                /* OR query over the fields */
                IList<SrndQuery> queries = new List<SrndQuery>();
                IEnumerator<string> fni = FieldNames.GetEnumerator();
                SrndQuery qc;
                while (fni.MoveNext())
                {
                    qc = (SrndQuery)q.Clone();
                    queries.Add(new FieldsQuery(qc, fni.Current, fieldOp));
                }
                OrQuery oq = new OrQuery(queries,
                                        true /* infix OR for field names */,
                                        OrOperatorName);
                // System.out.println(getClass().toString() + ", fields expanded: " + oq.toString()); /* needs testing */
                return oq.MakeLuceneQueryField(null, qf);
            }
        }

        public override Search.Query MakeLuceneQueryFieldNoBoost(string fieldName, BasicQueryFactory qf)
        {
            return MakeLuceneQueryNoBoost(qf); /* use this.fieldNames instead of fieldName */
        }

        public IList<string> FieldNames { get { return fieldNames; } }

        public char FieldOperator { get { return fieldOp; } }

        public override string ToString()
        {
            StringBuilder r = new StringBuilder();
            r.Append("(");
            FieldNamesToString(r);
            r.Append(q.ToString());
            r.Append(")");
            return r.ToString();
        }

        protected void FieldNamesToString(StringBuilder r)
        {
            IEnumerator<string> fni = FieldNames.GetEnumerator();
            while (fni.MoveNext())
            {
                r.Append(fni.Current);
                r.Append(FieldOperator);
            }
        }
    }
}
