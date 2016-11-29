using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace Lucene.Net.QueryParsers.Flexible.Core.Processors
{
    public class QueryNodeProcessorPipeline : IQueryNodeProcessor, IList<IQueryNodeProcessor>
    {
        //private LinkedList<IQueryNodeProcessor> processors = new LinkedList<IQueryNodeProcessor>();
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
         * @see List#add(int, Object)
         */

        //public virtual void Add(int index, IQueryNodeProcessor processor)
        //{
        //    this.processors.Insert(index, processor);
        //    processor.SetQueryConfigHandler(this.queryConfig);

        //}

        //      /**
        //       * @see List#addAll(Collection)
        //       */

        //public virtual bool AddAll(Collection<? extends QueryNodeProcessor> c)
        //      {
        //          boolean anyAdded = this.processors.addAll(c);

        //          for (QueryNodeProcessor processor : c)
        //          {
        //              processor.setQueryConfigHandler(this.queryConfig);
        //          }

        //          return anyAdded;

        //      }

        //      /**
        //       * @see List#addAll(int, Collection)
        //       */
        //      @Override
        //public boolean addAll(int index, Collection<? extends QueryNodeProcessor> c)
        //      {
        //          boolean anyAdded = this.processors.addAll(index, c);

        //          for (QueryNodeProcessor processor : c)
        //          {
        //              processor.setQueryConfigHandler(this.queryConfig);
        //          }

        //          return anyAdded;

        //      }

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

        //      /**
        //       * @see List#containsAll(Collection)
        //       */
        //      @Override
        //public boolean containsAll(Collection<?> c)
        //      {
        //          return this.processors.containsAll(c);
        //      }

        //      /**
        //       * @see List#get(int)
        //       */
        //      @Override
        //public QueryNodeProcessor Get(int index)
        //      {
        //          return this.processors.get(index);
        //      }

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

                //return oldProcessor;
            }
        }

        /**
         * @see List#indexOf(Object)
         */

        public virtual int IndexOf(object o)
        {
            return this.processors.IndexOf(o as IQueryNodeProcessor);
        }

        public virtual int IndexOf(IQueryNodeProcessor o)
        {
            return this.processors.IndexOf(o);
        }

        //      /**
        //       * @see List#isEmpty()
        //       */
        //      @Override
        //public boolean isEmpty()
        //      {
        //          return this.processors.isEmpty();
        //      }

        //      /**
        //       * @see List#iterator()
        //       */
        //      @Override
        //public Iterator<QueryNodeProcessor> iterator()
        //      {
        //          return this.processors.iterator();
        //      }

        public virtual IEnumerator<IQueryNodeProcessor> GetEnumerator()
        {
            return this.processors.GetEnumerator();
        }

        //      /**
        //       * @see List#lastIndexOf(Object)
        //       */
        //      @Override
        //public int lastIndexOf(Object o)
        //      {
        //          return this.processors.lastIndexOf(o);
        //      }

        //      /**
        //       * @see List#listIterator()
        //       */
        //      @Override
        //public ListIterator<QueryNodeProcessor> listIterator()
        //      {
        //          return this.processors.listIterator();
        //      }

        //      /**
        //       * @see List#listIterator(int)
        //       */
        //      @Override
        //public ListIterator<QueryNodeProcessor> listIterator(int index)
        //      {
        //          return this.processors.listIterator(index);
        //      }

        /**
         * @see List#remove(Object)
         */

        //public virtual bool Remove(object o)
        //{
        //    return this.processors.Remove(o as IQueryNodeProcessor);
        //}

        public virtual bool Remove(IQueryNodeProcessor o)
        {
            return this.processors.Remove(o);
        }

        //      /**
        //       * @see List#remove(int)
        //       */
        //      @Override
        //public QueryNodeProcessor remove(int index)
        //      {
        //          return this.processors.remove(index);
        //      }

        public virtual void RemoveAt(int index)
        {
            this.processors.RemoveAt(index);
        }

        //      /**
        //       * @see List#removeAll(Collection)
        //       */
        //      @Override
        //public boolean removeAll(Collection<?> c)
        //      {
        //          return this.processors.removeAll(c);
        //      }

        public virtual void RemoveRange(int index, int count)
        {
            this.processors.RemoveRange(index, count);
        }

        //      /**
        //       * @see List#retainAll(Collection)
        //       */
        //      @Override
        //public boolean retainAll(Collection<?> c)
        //      {
        //          return this.processors.retainAll(c);
        //      }

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

        //      /**
        //       * @see List#size()
        //       */
        //      @Override
        //public int size()
        //      {
        //          return this.processors.size();
        //      }

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

        //      /**
        //       * @see List#subList(int, int)
        //       */
        //      @Override
        //public List<QueryNodeProcessor> subList(int fromIndex, int toIndex)
        //      {
        //          return this.processors.subList(fromIndex, toIndex);
        //      }

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

        //      /**
        //       * @see List#toArray(Object[])
        //       */
        //      @Override
        //public <T> T[] toArray(T[] array)
        //      {
        //          return this.processors.toArray(array);
        //      }

        //      /**
        //       * @see List#toArray()
        //       */
        //      @Override
        //public Object[] toArray()
        //      {
        //          return this.processors.toArray();
        //      }
    }
}
