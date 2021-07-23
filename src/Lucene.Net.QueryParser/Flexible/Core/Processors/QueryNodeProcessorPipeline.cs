using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System.Collections;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Core.Processors
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
    /// A <see cref="QueryNodeProcessorPipeline"/> class should be used to build a query
    /// node processor pipeline.
    /// <para/>
    /// When a query node tree is processed using this class, it passes the query
    /// node tree to each processor on the pipeline and the result from each
    /// processor is passed to the next one, always following the order the
    /// processors were on the pipeline.
    /// <para/>
    /// When a <see cref="QueryConfigHandler"/> object is set on a
    /// <see cref="QueryNodeProcessorPipeline"/>, it also takes care of setting this
    /// <see cref="QueryConfigHandler"/> on all processor on pipeline.
    /// </summary>
    public class QueryNodeProcessorPipeline : IQueryNodeProcessor, IList<IQueryNodeProcessor>
    {
        private readonly JCG.List<IQueryNodeProcessor> processors = new JCG.List<IQueryNodeProcessor>();

        private QueryConfigHandler queryConfig;

        /// <summary>
        /// Constructs an empty query node processor pipeline.
        /// </summary>
        public QueryNodeProcessorPipeline()
        {
            // empty constructor
        }

        /// <summary>
        /// Constructs with a <see cref="QueryConfigHandler"/> object.
        /// </summary>
        public QueryNodeProcessorPipeline(QueryConfigHandler queryConfigHandler)
        {
            this.queryConfig = queryConfigHandler;
        }

        /// <summary>
        /// For reference about this method check:
        /// <see cref="IQueryNodeProcessor.GetQueryConfigHandler()"/>.
        /// </summary>
        /// <returns><see cref="QueryConfigHandler"/> the query configuration handler to be set.</returns>
        /// <seealso cref="IQueryNodeProcessor.SetQueryConfigHandler(QueryConfigHandler)"/>
        /// <seealso cref="QueryConfigHandler"/>
        public virtual QueryConfigHandler GetQueryConfigHandler()
        {
            return this.queryConfig;
        }

        /// <summary>
        /// For reference about this method check:
        /// <see cref="IQueryNodeProcessor.Process(IQueryNode)"/>.
        /// </summary>
        /// <param name="queryTree">the query node tree to be processed</param>
        /// <exception cref="QueryNodeException">if something goes wrong during the query node processing</exception>
        /// <seealso cref="IQueryNode"/>
        public virtual IQueryNode Process(IQueryNode queryTree)
        {
            foreach (IQueryNodeProcessor processor in this.processors)
            {
                queryTree = processor.Process(queryTree);
            }

            return queryTree;
        }

        /// <summary>
        /// For reference about this method check:
        /// <see cref="IQueryNodeProcessor.SetQueryConfigHandler(QueryConfigHandler)"/>.
        /// </summary>
        /// <param name="queryConfigHandler">the query configuration handler to be set.</param>
        /// <seealso cref="IQueryNodeProcessor.GetQueryConfigHandler()"/>
        /// <seealso cref="QueryConfigHandler"/>
        public virtual void SetQueryConfigHandler(QueryConfigHandler queryConfigHandler)
        {
            this.queryConfig = queryConfigHandler;

            foreach (IQueryNodeProcessor processor in this.processors)
            {
                processor.SetQueryConfigHandler(this.queryConfig);
            }
        }

        /// <summary>
        /// <see cref="ICollection{IQueryNodeProcessor}.Add(IQueryNodeProcessor)"/> 
        /// </summary>
        public virtual bool Add(IQueryNodeProcessor processor)
        {
            this.processors.Add(processor);
            bool added = processors.Contains(processor);

            if (added)
            {
                processor.SetQueryConfigHandler(this.queryConfig);
            }

            return added;
        }

        /// <summary>
        /// <see cref="ICollection{IQueryNodeProcessor}.Clear()"/> 
        /// </summary>
        public virtual void Clear()
        {
            this.processors.Clear();
        }

        public virtual bool Contains(object o)
        {
            // LUCENENET specific - cast required to get from object to IQueryNodeProcessor
            if (o is IQueryNodeProcessor other)
                return this.Contains(other);
            return false;
        }

        /// <summary>
        /// <see cref="IList{IQueryNodeProcessor}.this[int]"/> 
        /// </summary>
        public virtual IQueryNodeProcessor this[int index]
        {
            get => this.processors[index];
            set
            {
                IQueryNodeProcessor oldProcessor = this.processors[index];
                this.processors[index] = value;

                if (oldProcessor != value)
                {
                    value.SetQueryConfigHandler(this.queryConfig);
                }
            }
        }

        /// <summary>
        /// <see cref="IList{IQueryNodeProcessor}.IndexOf(IQueryNodeProcessor)"/>
        /// </summary>
        public virtual int IndexOf(IQueryNodeProcessor o)
        {
            return this.processors.IndexOf(o);
        }

        /// <summary>
        /// <see cref="IEnumerable{IQueryNodeProcessor}.GetEnumerator()"/>
        /// </summary>
        public virtual IEnumerator<IQueryNodeProcessor> GetEnumerator()
        {
            return this.processors.GetEnumerator();
        }

        /// <summary>
        /// <see cref="ICollection{IQueryNodeProcessor}.Remove(IQueryNodeProcessor)"/> 
        /// </summary>
        public virtual bool Remove(IQueryNodeProcessor o)
        {
            return this.processors.Remove(o);
        }

        /// <summary>
        /// <see cref="IList{IQueryNodeProcessor}.RemoveAt(int)"/> 
        /// </summary>
        /// <param name="index"></param>
        public virtual void RemoveAt(int index)
        {
            this.processors.RemoveAt(index);
        }

        public virtual void RemoveRange(int index, int count)
        {
            this.processors.RemoveRange(index, count);
        }

        public virtual IQueryNodeProcessor Set(int index, IQueryNodeProcessor processor)
        {
            IQueryNodeProcessor oldProcessor = this.processors[index];
            this.processors[index] = processor;

            if (oldProcessor != processor)
            {
                processor.SetQueryConfigHandler(this.queryConfig);
            }

            return oldProcessor;
        }

        /// <summary>
        /// <see cref="ICollection{IQueryNodeProcessor}.Count"/> 
        /// </summary>
        public virtual int Count => this.processors.Count;

        /// <summary>
        /// <see cref="ICollection{IQueryNodeProcessor}.IsReadOnly"/> 
        /// </summary>
        public virtual bool IsReadOnly => false;

        public virtual IList<IQueryNodeProcessor> GetRange(int index, int count)
        {
            return this.processors.GetRange(index, count);
        }

        /// <summary>
        /// <see cref="JCG.List{T}.GetView(int, int)"/>
        /// </summary>
        public virtual IList<IQueryNodeProcessor> GetView(int index, int count)
        {
            return this.processors.GetView(index, count);
        }

        public virtual void Insert(int index, IQueryNodeProcessor item)
        {
            this.processors.Insert(index, item);
            item.SetQueryConfigHandler(this.queryConfig);
        }

        /// <summary>
        /// <see cref="ICollection{IQueryNodeProcessor}.Add(IQueryNodeProcessor)"/> 
        /// </summary>
        void ICollection<IQueryNodeProcessor>.Add(IQueryNodeProcessor item)
        {
            this.Add(item);
        }

        /// <summary>
        /// <see cref="ICollection{IQueryNodeProcessor}.Contains(IQueryNodeProcessor)"/> 
        /// </summary>
        public virtual bool Contains(IQueryNodeProcessor item)
        {
            return this.processors.Contains(item);
        }

        /// <summary>
        /// <see cref="ICollection{IQueryNodeProcessor}.CopyTo(IQueryNodeProcessor[], int)"/> 
        /// </summary>
        public virtual void CopyTo(IQueryNodeProcessor[] array, int arrayIndex)
        {
            this.processors.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// <see cref="IEnumerable.GetEnumerator()"/> 
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
