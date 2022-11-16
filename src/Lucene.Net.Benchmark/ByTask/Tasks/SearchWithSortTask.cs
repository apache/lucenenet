using J2N.Text;
using Lucene.Net.Benchmarks.ByTask.Feeds;
using Lucene.Net.Search;
using Lucene.Net.Support;
using System;

namespace Lucene.Net.Benchmarks.ByTask.Tasks
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
    /// Does sort search on specified field.
    /// </summary>
    public class SearchWithSortTask : ReadTask
    {
        private bool doScore = true;
        private bool doMaxScore = true;
        private Sort sort;

        public SearchWithSortTask(PerfRunData runData)
            : base(runData)
        {
        }

        /// <summary>
        /// SortFields: field:type,field:type[,noscore][,nomaxscore]
        /// <para/>
        /// If noscore is present, then we turn off score tracking
        /// in <see cref="TopFieldCollector"/>.
        /// If nomaxscore is present, then we turn off maxScore tracking
        /// in <see cref="TopFieldCollector"/>.
        /// <para/>
        /// name:string,page:int,subject:string
        /// </summary>
        public override void SetParams(string sortField)
        {
            base.SetParams(sortField);
            string[] fields = sortField.Split(',').TrimEnd();
            SortField[] sortFields = new SortField[fields.Length];
            int upto = 0;
            for (int i = 0; i < fields.Length; i++)
            {
                string field = fields[i];
                SortField sortField0;
                if (field.Equals("doc", StringComparison.Ordinal))
                {
                    sortField0 = SortField.FIELD_DOC;
                }
                else if (field.Equals("score", StringComparison.Ordinal))
                {
                    sortField0 = SortField.FIELD_SCORE;
                }
                else if (field.Equals("noscore", StringComparison.Ordinal))
                {
                    doScore = false;
                    continue;
                }
                else if (field.Equals("nomaxscore", StringComparison.Ordinal))
                {
                    doMaxScore = false;
                    continue;
                }
                else
                {
                    int index = field.LastIndexOf(':');
                    string fieldName;
                    string typeString;
                    if (index != -1)
                    {
                        fieldName = field.Substring(0, index - 0);
                        typeString = field.Substring(1 + index, field.Length - (1 + index));
                    }
                    else
                    {
                        throw RuntimeException.Create("You must specify the sort type ie page:int,subject:string");
                    }
                    sortField0 = new SortField(fieldName, (SortFieldType)Enum.Parse(typeof(SortFieldType), typeString, true));
                }
                sortFields[upto++] = sortField0;
            }

            if (upto < sortFields.Length)
            {
                SortField[] newSortFields = new SortField[upto];
                Arrays.Copy(sortFields, 0, newSortFields, 0, upto);
                sortFields = newSortFields;
            }
            this.sort = new Sort(sortFields);
        }

        public override bool SupportsParams => true;

        public override IQueryMaker GetQueryMaker()
        {
            return RunData.GetQueryMaker(this);
        }

        public override bool WithRetrieve => false;

        public override bool WithSearch => true;

        public override bool WithTraverse => false;

        public override bool WithWarm => false;

        public override bool WithScore => doScore;

        public override bool WithMaxScore => doMaxScore;

        public override Sort Sort
        {
            get
            {
                if (sort is null)
                {
                    throw IllegalStateException.Create("No sort field was set");
                }
                return sort;
            }
        }
    }
}
