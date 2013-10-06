using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Processors
{
    public class QueryNodeProcessorPipeline : IQueryNodeProcessor, IList<IQueryNodeProcessor>
    {
        private List<IQueryNodeProcessor> processors = new List<IQueryNodeProcessor>();

        private QueryConfigHandler queryConfig;

        public QueryNodeProcessorPipeline()
        {
            // empty constructor
        }

        public QueryNodeProcessorPipeline(QueryConfigHandler queryConfigHandler)
        {
            this.queryConfig = queryConfigHandler;
        }

        public QueryConfigHandler QueryConfigHandler
        {
            get
            {
                return this.queryConfig;
            }
            set
            {
                this.queryConfig = value;

                foreach (IQueryNodeProcessor processor in this.processors)
                {
                    processor.QueryConfigHandler = this.queryConfig;
                }
            }
        }

        public IQueryNode Process(IQueryNode queryTree)
        {
            foreach (IQueryNodeProcessor processor in this.processors)
            {
                queryTree = processor.Process(queryTree);
            }

            return queryTree;
        }

        public void Add(IQueryNodeProcessor processor)
        {
            this.processors.Add(processor);

            processor.QueryConfigHandler = this.queryConfig;
        }

        public void Insert(int index, IQueryNodeProcessor processor)
        {
            this.processors.Insert(index, processor);
            processor.QueryConfigHandler = this.queryConfig;
        }

        public bool AddAll(ICollection<IQueryNodeProcessor> c)
        {
            this.processors.AddRange(c);

            foreach (IQueryNodeProcessor processor in c)
            {
                processor.QueryConfigHandler = this.queryConfig;
            }

            return c.Count > 0;
        }

        public bool AddAll(int index, ICollection<IQueryNodeProcessor> c)
        {
            this.processors.InsertRange(index, c);

            foreach (IQueryNodeProcessor processor in c)
            {
                processor.QueryConfigHandler = this.queryConfig;
            }

            return c.Count > 0;
        }

        public void Clear()
        {
            this.processors.Clear();
        }

        public bool Contains(IQueryNodeProcessor item)
        {
            return this.processors.Contains(item);
        }

        public bool ContainsAll(ICollection<IQueryNodeProcessor> c)
        {
            foreach (var processor in c)
            {
                if (!this.processors.Contains(processor))
                    return false;
            }

            return true;
        }
        
        public IQueryNodeProcessor this[int index]
        {
            get
            {
                return this.processors[index];
            }
            set
            {
                IQueryNodeProcessor oldProcessor = (this.processors.Count > index) ? this.processors[index] : value;
                this.processors[index] = value;

                if (oldProcessor != value)
                {
                    value.QueryConfigHandler = this.queryConfig;
                }
            }
        }

        public int IndexOf(IQueryNodeProcessor item)
        {
            return this.processors.IndexOf(item);
        }

        public bool IsEmpty
        {
            get { return this.processors.Count == 0; }
        }
        
        public IEnumerator<IQueryNodeProcessor> GetEnumerator()
        {
            return this.processors.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int LastIndexOf(IQueryNodeProcessor o)
        {
            return this.processors.LastIndexOf(o);
        }

        public bool Remove(IQueryNodeProcessor item)
        {
            return this.processors.Remove(item);
        }

        public void RemoveAt(int index)
        {
            this.processors.RemoveAt(index);
        }

        public int Count
        {
            get { return this.processors.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void CopyTo(IQueryNodeProcessor[] array, int arrayIndex)
        {
            ((IList<IQueryNodeProcessor>)this.processors).CopyTo(array, arrayIndex);
        }
    }
}
