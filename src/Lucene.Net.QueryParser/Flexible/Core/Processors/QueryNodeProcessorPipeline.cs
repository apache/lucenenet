using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.QueryParsers.Flexible.Core.Processors
{
    public class QueryNodeProcessorPipeline : IQueryNodeProcessor, IList<IQueryNodeProcessor>
    {
        private List<IQueryNodeProcessor> processors = new List<IQueryNodeProcessor>();

        private QueryConfigHandler queryConfig;

        /**
         * Constructs an empty query node processor pipeline.
         */
        public QueryNodeProcessorPipeline()
        {
            // empty constructor
        }

        /**
         * Constructs with a {@link QueryConfigHandler} object.
         */
        public QueryNodeProcessorPipeline(QueryConfigHandler queryConfigHandler)
        {
            this.queryConfig = queryConfigHandler;
        }

        /**
         * For reference about this method check:
         * {@link QueryNodeProcessor#getQueryConfigHandler()}.
         * 
         * @return QueryConfigHandler the query configuration handler to be set.
         * 
         * @see QueryNodeProcessor#setQueryConfigHandler(QueryConfigHandler)
         * @see QueryConfigHandler
         */
        public virtual QueryConfigHandler GetQueryConfigHandler()
        {
            return this.queryConfig;
        }

        /**
         * For reference about this method check:
         * {@link QueryNodeProcessor#process(QueryNode)}.
         * 
         * @param queryTree the query node tree to be processed
         * 
         * @throws QueryNodeException if something goes wrong during the query node
         *         processing
         * 
         * @see QueryNode
         */

        public virtual IQueryNode Process(IQueryNode queryTree)
        {
            foreach (IQueryNodeProcessor processor in this.processors)
            {
                queryTree = processor.Process(queryTree);
            }

            return queryTree;
        }

        /**
         * For reference about this method check:
         * {@link QueryNodeProcessor#setQueryConfigHandler(QueryConfigHandler)}.
         * 
         * @param queryConfigHandler the query configuration handler to be set.
         * 
         * @see QueryNodeProcessor#getQueryConfigHandler()
         * @see QueryConfigHandler
         */
        public virtual void SetQueryConfigHandler(QueryConfigHandler queryConfigHandler)
        {
            this.queryConfig = queryConfigHandler;

            foreach (IQueryNodeProcessor processor in this.processors)
            {
                processor.SetQueryConfigHandler(this.queryConfig);
            }
        }

        /**
         * @see List#add(Object)
         */
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

        /**
         * @see List#clear()
         */
        public virtual void Clear()
        {
            this.processors.Clear();
        }

        /**
         * @see List#contains(Object)
         */
        public virtual bool Contains(object o)
        {
            return this.processors.Contains(o);
        }

        public virtual IQueryNodeProcessor this[int index]
        {
            get
            {
                return this.processors[index];
            }
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

        /**
        * @see List#indexOf(Object)
        */
        public virtual int IndexOf(IQueryNodeProcessor o)
        {
            return this.processors.IndexOf(o);
        }

        /**
         * @see List#iterator()
         */
        public virtual IEnumerator<IQueryNodeProcessor> GetEnumerator()
        {
            return this.processors.GetEnumerator();
        }

        /**
         * @see List#remove(Object)
         */
        public virtual bool Remove(IQueryNodeProcessor o)
        {
            return this.processors.Remove(o);
        }

        /**
         * @see List#remove(int)
         */
        public virtual void RemoveAt(int index)
        {
            this.processors.RemoveAt(index);
        }

        public virtual void RemoveRange(int index, int count)
        {
            this.processors.RemoveRange(index, count);
        }

        /**
         * @see List#set(int, Object)
         */
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

        public virtual int Count
        {
            get { return this.processors.Count; }
        }

        public virtual bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public virtual List<IQueryNodeProcessor> GetRange(int index, int count)
        {
            return this.processors.GetRange(index, count);
        }

        public virtual void Insert(int index, IQueryNodeProcessor item)
        {
            this.processors.Insert(index, item);
            item.SetQueryConfigHandler(this.queryConfig);
        }

        void ICollection<IQueryNodeProcessor>.Add(IQueryNodeProcessor item)
        {
            this.Add(item);
        }

        public virtual bool Contains(IQueryNodeProcessor item)
        {
            return this.processors.Contains(item);
        }

        public virtual void CopyTo(IQueryNodeProcessor[] array, int arrayIndex)
        {
            this.processors.CopyTo(array, arrayIndex);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
