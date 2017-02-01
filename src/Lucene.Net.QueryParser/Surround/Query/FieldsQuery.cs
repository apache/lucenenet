using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Forms an OR query of the provided query across multiple fields.
    /// </summary>
    public class FieldsQuery : SrndQuery /* mostly untested */
    {
        private SrndQuery q;
        private IList<string> fieldNames;
        private readonly char fieldOp;
        private readonly string orOperatorName = "OR"; /* for expanded queries, not normally visible */

        public FieldsQuery(SrndQuery q, IList<string> fieldNames, char fieldOp)
        {
            this.q = q;
            this.fieldNames = fieldNames;
            this.fieldOp = fieldOp;
        }

        public FieldsQuery(SrndQuery q, string fieldName, char fieldOp)
        {
            this.q = q;
            var fieldNameList = new List<string>();
            fieldNameList.Add(fieldName);
            this.fieldNames = fieldNameList;
            this.fieldOp = fieldOp;
        }

        public override bool IsFieldsSubQueryAcceptable
        {
            get { return false; }
        }

        public virtual Search.Query MakeLuceneQueryNoBoost(BasicQueryFactory qf)
        {
            if (fieldNames.Count == 1)
            { /* single field name: no new queries needed */
                return q.MakeLuceneQueryFieldNoBoost(fieldNames.FirstOrDefault(), qf);
            }
            else
            { /* OR query over the fields */
                List<SrndQuery> queries = new List<SrndQuery>();
                foreach (var fieldName in fieldNames)
                {
                    var qc = (SrndQuery)q.Clone();
                    queries.Add(new FieldsQuery(qc, fieldName, fieldOp));
                }
                OrQuery oq = new OrQuery(queries,
                                        true /* infix OR for field names */,
                                        orOperatorName);
                // System.out.println(getClass().toString() + ", fields expanded: " + oq.toString()); /* needs testing */
                return oq.MakeLuceneQueryField(null, qf);
            }
        }

        public override Search.Query MakeLuceneQueryFieldNoBoost(string fieldName, BasicQueryFactory qf)
        {
            return MakeLuceneQueryNoBoost(qf); /* use this.fieldNames instead of fieldName */
        }

        public virtual IList<string> FieldNames { get { return fieldNames; } }

        public virtual char FieldOperator { get { return fieldOp; } }

        public override string ToString()
        {
            StringBuilder r = new StringBuilder();
            r.Append("(");
            FieldNamesToString(r);
            r.Append(q.ToString());
            r.Append(")");
            return r.ToString();
        }

        protected virtual void FieldNamesToString(StringBuilder r)
        {
            foreach (var fieldName in FieldNames)
            {
                r.Append(fieldName);
                r.Append(FieldOperator);
            }
        }
    }
}
