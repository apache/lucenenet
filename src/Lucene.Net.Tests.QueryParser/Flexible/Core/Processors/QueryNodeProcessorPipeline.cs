/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Config;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Processors;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Core.Processors
{
	/// <summary>
	/// A
	/// <see cref="QueryNodeProcessorPipeline">QueryNodeProcessorPipeline</see>
	/// class should be used to build a query
	/// node processor pipeline.
	/// When a query node tree is processed using this class, it passes the query
	/// node tree to each processor on the pipeline and the result from each
	/// processor is passed to the next one, always following the order the
	/// processors were on the pipeline.
	/// When a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler"
	/// 	>Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
	/// object is set on a
	/// <see cref="QueryNodeProcessorPipeline">QueryNodeProcessorPipeline</see>
	/// , it also takes care of setting this
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler"
	/// 	>Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
	/// on all processor on pipeline.
	/// </summary>
	public class QueryNodeProcessorPipeline : QueryNodeProcessor, IList<QueryNodeProcessor
		>
	{
		private List<QueryNodeProcessor> processors = new List<QueryNodeProcessor>();

		private QueryConfigHandler queryConfig;

		/// <summary>Constructs an empty query node processor pipeline.</summary>
		/// <remarks>Constructs an empty query node processor pipeline.</remarks>
		public QueryNodeProcessorPipeline()
		{
		}

		/// <summary>
		/// Constructs with a
		/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler"
		/// 	>Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
		/// object.
		/// </summary>
		public QueryNodeProcessorPipeline(QueryConfigHandler queryConfigHandler)
		{
			// empty constructor
			this.queryConfig = queryConfigHandler;
		}

		/// <summary>
		/// For reference about this method check:
		/// <see cref="QueryNodeProcessor.GetQueryConfigHandler()">QueryNodeProcessor.GetQueryConfigHandler()
		/// 	</see>
		/// .
		/// </summary>
		/// <returns>QueryConfigHandler the query configuration handler to be set.</returns>
		/// <seealso cref="QueryNodeProcessor.SetQueryConfigHandler(Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler)
		/// 	">QueryNodeProcessor.SetQueryConfigHandler(Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler)
		/// 	</seealso>
		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler
		/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler</seealso>
		public virtual QueryConfigHandler GetQueryConfigHandler()
		{
			return this.queryConfig;
		}

		/// <summary>
		/// For reference about this method check:
		/// <see cref="QueryNodeProcessor.Process(Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.QueryNode)
		/// 	">QueryNodeProcessor.Process(Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.QueryNode)
		/// 	</see>
		/// .
		/// </summary>
		/// <param name="queryTree">the query node tree to be processed</param>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// if something goes wrong during the query node
		/// processing
		/// </exception>
		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.QueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.QueryNode
		/// 	</seealso>
		public virtual QueryNode Process(QueryNode queryTree)
		{
			foreach (QueryNodeProcessor processor in this.processors)
			{
				queryTree = processor.Process(queryTree);
			}
			return queryTree;
		}

		/// <summary>
		/// For reference about this method check:
		/// <see cref="QueryNodeProcessor.SetQueryConfigHandler(Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler)
		/// 	">QueryNodeProcessor.SetQueryConfigHandler(Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler)
		/// 	</see>
		/// .
		/// </summary>
		/// <param name="queryConfigHandler">the query configuration handler to be set.</param>
		/// <seealso cref="QueryNodeProcessor.GetQueryConfigHandler()">QueryNodeProcessor.GetQueryConfigHandler()
		/// 	</seealso>
		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler
		/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler</seealso>
		public virtual void SetQueryConfigHandler(QueryConfigHandler queryConfigHandler)
		{
			this.queryConfig = queryConfigHandler;
			foreach (QueryNodeProcessor processor in this.processors)
			{
				processor.SetQueryConfigHandler(this.queryConfig);
			}
		}

		/// <seealso cref="System.Collections.IList{E}.AddItem(object)">System.Collections.IList&lt;E&gt;.AddItem(object)
		/// 	</seealso>
		public virtual bool AddItem(QueryNodeProcessor processor)
		{
			bool added = this.processors.AddItem(processor);
			if (added)
			{
				processor.SetQueryConfigHandler(this.queryConfig);
			}
			return added;
		}

		/// <seealso cref="System.Collections.IList{E}.Add(int, object)">System.Collections.IList&lt;E&gt;.Add(int, object)
		/// 	</seealso>
		public virtual void Add(int index, QueryNodeProcessor processor)
		{
			this.processors.Add(index, processor);
			processor.SetQueryConfigHandler(this.queryConfig);
		}

		/// <seealso cref="Sharpen.Collections.AddAll(System.Collections.ICollection{E})">Sharpen.Collections.AddAll(System.Collections.ICollection&lt;E&gt;)
		/// 	</seealso>
		public virtual bool AddAll<_T0>(ICollection<_T0> c) where _T0:QueryNodeProcessor
		{
			bool anyAdded = Sharpen.Collections.AddAll(this.processors, c);
			foreach (QueryNodeProcessor processor in c)
			{
				processor.SetQueryConfigHandler(this.queryConfig);
			}
			return anyAdded;
		}

		/// <seealso cref="System.Collections.IList{E}.AddRange(int, System.Collections.ICollection{E})
		/// 	">System.Collections.IList&lt;E&gt;.AddRange(int, System.Collections.ICollection&lt;E&gt;)
		/// 	</seealso>
		public virtual bool AddRange<_T0>(int index, ICollection<_T0> c) where _T0:QueryNodeProcessor
		{
			bool anyAdded = this.processors.AddRange(index, c);
			foreach (QueryNodeProcessor processor in c)
			{
				processor.SetQueryConfigHandler(this.queryConfig);
			}
			return anyAdded;
		}

		/// <seealso cref="System.Collections.IList{E}.Clear()">System.Collections.IList&lt;E&gt;.Clear()
		/// 	</seealso>
		public virtual void Clear()
		{
			this.processors.Clear();
		}

		/// <seealso cref="System.Collections.IList{E}.Contains(object)">System.Collections.IList&lt;E&gt;.Contains(object)
		/// 	</seealso>
		public virtual bool Contains(object o)
		{
			return this.processors.Contains(o);
		}

		/// <seealso cref="System.Collections.IList{E}.ContainsAll(System.Collections.ICollection{E})
		/// 	">System.Collections.IList&lt;E&gt;.ContainsAll(System.Collections.ICollection&lt;E&gt;)
		/// 	</seealso>
		public virtual bool ContainsAll<_T0>(ICollection<_T0> c)
		{
			return this.processors.ContainsAll(c);
		}

		/// <seealso cref="System.Collections.IList{E}.Get(int)">System.Collections.IList&lt;E&gt;.Get(int)
		/// 	</seealso>
		public virtual QueryNodeProcessor Get(int index)
		{
			return this.processors[index];
		}

		/// <seealso cref="System.Collections.IList{E}.IndexOf(object)">System.Collections.IList&lt;E&gt;.IndexOf(object)
		/// 	</seealso>
		public virtual int IndexOf(object o)
		{
			return this.processors.IndexOf(o);
		}

		/// <seealso cref="System.Collections.IList{E}.IsEmpty()">System.Collections.IList&lt;E&gt;.IsEmpty()
		/// 	</seealso>
		public virtual bool IsEmpty()
		{
			return this.processors.IsEmpty();
		}

		/// <seealso cref="System.Collections.IList{E}.Iterator()">System.Collections.IList&lt;E&gt;.Iterator()
		/// 	</seealso>
		public virtual Sharpen.Iterator<QueryNodeProcessor> Iterator()
		{
			return this.processors.Iterator();
		}

		/// <seealso cref="System.Collections.IList{E}.LastIndexOf(object)">System.Collections.IList&lt;E&gt;.LastIndexOf(object)
		/// 	</seealso>
		public virtual int LastIndexOf(object o)
		{
			return this.processors.LastIndexOf(o);
		}

		/// <seealso cref="System.Collections.IList{E}.ListIterator()">System.Collections.IList&lt;E&gt;.ListIterator()
		/// 	</seealso>
		public virtual Sharpen.ListIterator<QueryNodeProcessor> ListIterator()
		{
			return this.processors.ListIterator();
		}

		/// <seealso cref="System.Collections.IList{E}.ListIterator(int)">System.Collections.IList&lt;E&gt;.ListIterator(int)
		/// 	</seealso>
		public virtual Sharpen.ListIterator<QueryNodeProcessor> ListIterator(int index)
		{
			return this.processors.ListIterator(index);
		}

		/// <seealso cref="System.Collections.IList{E}.Remove(object)">System.Collections.IList&lt;E&gt;.Remove(object)
		/// 	</seealso>
		public virtual bool Remove(object o)
		{
			return this.processors.Remove(o);
		}

		/// <seealso cref="System.Collections.IList{E}.Remove(int)">System.Collections.IList&lt;E&gt;.Remove(int)
		/// 	</seealso>
		public virtual QueryNodeProcessor Remove(int index)
		{
			return this.processors.Remove(index);
		}

		/// <seealso cref="System.Collections.IList{E}.RemoveAll(System.Collections.ICollection{E})
		/// 	">System.Collections.IList&lt;E&gt;.RemoveAll(System.Collections.ICollection&lt;E&gt;)
		/// 	</seealso>
		public virtual bool RemoveAll<_T0>(ICollection<_T0> c)
		{
			return this.processors.RemoveAll(c);
		}

		/// <seealso cref="System.Collections.IList{E}.RetainAll(System.Collections.ICollection{E})
		/// 	">System.Collections.IList&lt;E&gt;.RetainAll(System.Collections.ICollection&lt;E&gt;)
		/// 	</seealso>
		public virtual bool RetainAll<_T0>(ICollection<_T0> c)
		{
			return this.processors.RetainAll(c);
		}

		/// <seealso cref="System.Collections.IList{E}.Set(int, object)">System.Collections.IList&lt;E&gt;.Set(int, object)
		/// 	</seealso>
		public virtual QueryNodeProcessor Set(int index, QueryNodeProcessor processor)
		{
			QueryNodeProcessor oldProcessor = this.processors.Set(index, processor);
			if (oldProcessor != processor)
			{
				processor.SetQueryConfigHandler(this.queryConfig);
			}
			return oldProcessor;
		}

		/// <seealso cref="System.Collections.IList{E}.Count()">System.Collections.IList&lt;E&gt;.Count()
		/// 	</seealso>
		public virtual int Count
		{
			get
			{
				return this.processors.Count;
			}
		}

		/// <seealso cref="System.Collections.IList{E}.SubList(int, int)">System.Collections.IList&lt;E&gt;.SubList(int, int)
		/// 	</seealso>
		public virtual IList<QueryNodeProcessor> SubList(int fromIndex, int toIndex)
		{
			return this.processors.SubList(fromIndex, toIndex);
		}

		/// <seealso cref="Sharpen.Collections.ToArray{T}(object[])">Sharpen.Collections.ToArray&lt;T&gt;(object[])
		/// 	</seealso>
		public virtual T[] ToArray<T>(T[] array)
		{
			return Sharpen.Collections.ToArray(this.processors, array);
		}

		/// <seealso cref="Sharpen.Collections.ToArray()">Sharpen.Collections.ToArray()</seealso>
		public virtual object[] ToArray()
		{
			return Sharpen.Collections.ToArray(this.processors);
		}
	}
}
