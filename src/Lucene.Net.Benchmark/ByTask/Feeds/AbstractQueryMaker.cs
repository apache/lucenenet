using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Search;
using Lucene.Net.Support.Threading;
using System;
using System.Text;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
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
    /// Abstract base query maker. 
    /// Each query maker should just implement the <see cref="PrepareQueries()"/> method.
    /// </summary>
    public abstract class AbstractQueryMaker : IQueryMaker
    {
        protected int m_qnum = 0;
        protected Query[] m_queries;
        protected Config m_config;

        public virtual void ResetInputs()
        {
            m_qnum = 0;
        }

        protected abstract Query[] PrepareQueries();

        public virtual void SetConfig(Config config)
        {
            this.m_config = config;
            m_queries = PrepareQueries();
        }

        public virtual string PrintQueries()
        {
            string newline = Environment.NewLine;
            StringBuilder sb = new StringBuilder();
            if (m_queries != null)
            {
                for (int i = 0; i < m_queries.Length; i++)
                {
                    sb.Append(i + ". " + m_queries[i].GetType().Name + " - " + m_queries[i].ToString());
                    sb.Append(newline);
                }
            }
            return sb.ToString();
        }

        public virtual Query MakeQuery()
        {
            return m_queries[NextQnum()];
        }

        // return next qnum
        protected virtual int NextQnum()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                int res = m_qnum;
                m_qnum = (m_qnum + 1) % m_queries.Length;
                return res;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <seealso cref="IQueryMaker.MakeQuery(int)"/>
        public virtual Query MakeQuery(int size)
        {
            throw new Exception(this + ".MakeQuery(int size) is not supported!");
        }
    }
}
