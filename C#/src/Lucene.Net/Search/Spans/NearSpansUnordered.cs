/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using IndexReader = Lucene.Net.Index.IndexReader;
using PriorityQueue = Lucene.Net.Util.PriorityQueue;

namespace Lucene.Net.Search.Spans
{
	
	class NearSpansUnordered : Spans
	{
		private SpanNearQuery query;
		
		private System.Collections.IList ordered = new System.Collections.ArrayList(); // spans in query order
		private int slop; // from query
		
		private SpansCell first; // linked list of spans
		private SpansCell last; // sorted by doc only
		
		private int totalLength; // sum of current lengths
		
		private CellQueue queue; // sorted queue of spans
		private SpansCell max; // max element in queue
		
		private bool more = true; // true iff not done
		private bool firstTime = true; // true before first next()
		
		private class CellQueue : PriorityQueue
		{
			private void  InitBlock(NearSpansUnordered enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private NearSpansUnordered enclosingInstance;
			public NearSpansUnordered Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public CellQueue(NearSpansUnordered enclosingInstance, int size)
			{
				InitBlock(enclosingInstance);
				Initialize(size);
			}
			
			public override bool LessThan(System.Object o1, System.Object o2)
			{
				SpansCell spans1 = (SpansCell) o1;
				SpansCell spans2 = (SpansCell) o2;
				if (spans1.Doc() == spans2.Doc())
				{
					return NearSpansOrdered.DocSpansOrdered(spans1, spans2);
				}
				else
				{
					return spans1.Doc() < spans2.Doc();
				}
			}
		}
		
		
		/// <summary>Wraps a Spans, and can be used to form a linked list. </summary>
		private class SpansCell : Spans
		{
			private void  InitBlock(NearSpansUnordered enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private NearSpansUnordered enclosingInstance;
			public NearSpansUnordered Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private Spans spans;
			internal SpansCell next;
			private int length = - 1;
			private int index;
			
			public SpansCell(NearSpansUnordered enclosingInstance, Spans spans, int index)
			{
				InitBlock(enclosingInstance);
				this.spans = spans;
				this.index = index;
			}
			
			public virtual bool Next()
			{
				return Adjust(spans.Next());
			}
			
			public virtual bool SkipTo(int target)
			{
				return Adjust(spans.SkipTo(target));
			}
			
			private bool Adjust(bool condition)
			{
				if (length != - 1)
				{
					Enclosing_Instance.totalLength -= length; // subtract old length
				}
				if (condition)
				{
					length = End() - Start();
					Enclosing_Instance.totalLength += length; // add new length
					
					if (Enclosing_Instance.max == null || Doc() > Enclosing_Instance.max.Doc() || (Doc() == Enclosing_Instance.max.Doc()) && (End() > Enclosing_Instance.max.End()))
					{
						Enclosing_Instance.max = this;
					}
				}
				Enclosing_Instance.more = condition;
				return condition;
			}
			
			public virtual int Doc()
			{
				return spans.Doc();
			}
			public virtual int Start()
			{
				return spans.Start();
			}
			public virtual int End()
			{
				return spans.End();
			}
			
			public override System.String ToString()
			{
				return spans.ToString() + "#" + index;
			}
		}
		
		
		public NearSpansUnordered(SpanNearQuery query, IndexReader reader)
		{
			this.query = query;
			this.slop = query.GetSlop();
			
			SpanQuery[] clauses = query.GetClauses();
			queue = new CellQueue(this, clauses.Length);
			for (int i = 0; i < clauses.Length; i++)
			{
				SpansCell cell = new SpansCell(this, clauses[i].GetSpans(reader), i);
				ordered.Add(cell);
			}
		}
		
		public virtual bool Next()
		{
			if (firstTime)
			{
				InitList(true);
				ListToQueue(); // initialize queue
				firstTime = false;
			}
			else if (more)
			{
				if (Min().Next())
				{
					// trigger further scanning
					queue.AdjustTop(); // maintain queue
				}
				else
				{
					more = false;
				}
			}
			
			while (more)
			{
				
				bool queueStale = false;
				
				if (Min().Doc() != max.Doc())
				{
					// maintain list
					QueueToList();
					queueStale = true;
				}
				
				// skip to doc w/ all clauses
				
				while (more && first.Doc() < last.Doc())
				{
					more = first.SkipTo(last.Doc()); // skip first upto last
					FirstToLast(); // and move it to the end
					queueStale = true;
				}
				
				if (!more)
					return false;
				
				// found doc w/ all clauses
				
				if (queueStale)
				{
					// maintain the queue
					ListToQueue();
					queueStale = false;
				}
				
				if (AtMatch())
				{
					return true;
				}
				
				more = Min().Next();
				if (more)
				{
					queue.AdjustTop(); // maintain queue
				}
			}
			return false; // no more matches
		}
		
		public virtual bool SkipTo(int target)
		{
			if (firstTime)
			{
				// initialize
				InitList(false);
				for (SpansCell cell = first; more && cell != null; cell = cell.next)
				{
					more = cell.SkipTo(target); // skip all
				}
				if (more)
				{
					ListToQueue();
				}
				firstTime = false;
			}
			else
			{
				// normal case
				while (more && Min().Doc() < target)
				{
					// skip as needed
					if (Min().SkipTo(target))
					{
						queue.AdjustTop();
					}
					else
					{
						more = false;
					}
				}
			}
			return more && (AtMatch() || Next());
		}
		
		private SpansCell Min()
		{
			return (SpansCell) queue.Top();
		}
		
		public virtual int Doc()
		{
			return Min().Doc();
		}
		public virtual int Start()
		{
			return Min().Start();
		}
		public virtual int End()
		{
			return max.End();
		}
		
		
		public override System.String ToString()
		{
			return GetType().FullName + "(" + query.ToString() + ")@" + (firstTime ? "START" : (more ? (Doc() + ":" + Start() + "-" + End()) : "END"));
		}
		
		private void  InitList(bool next)
		{
			for (int i = 0; more && i < ordered.Count; i++)
			{
				SpansCell cell = (SpansCell) ordered[i];
				if (next)
					more = cell.Next(); // move to first entry
				if (more)
				{
					AddToList(cell); // add to list
				}
			}
		}
		
		private void  AddToList(SpansCell cell)
		{
			if (last != null)
			{
				// add next to end of list
				last.next = cell;
			}
			else
				first = cell;
			last = cell;
			cell.next = null;
		}
		
		private void  FirstToLast()
		{
			last.next = first; // move first to end of list
			last = first;
			first = first.next;
			last.next = null;
		}
		
		private void  QueueToList()
		{
			last = first = null;
			while (queue.Top() != null)
			{
				AddToList((SpansCell) queue.Pop());
			}
		}
		
		private void  ListToQueue()
		{
			queue.Clear(); // rebuild queue
			for (SpansCell cell = first; cell != null; cell = cell.next)
			{
				queue.Put(cell); // add to queue from list
			}
		}
		
		private bool AtMatch()
		{
			return (Min().Doc() == max.Doc()) && ((max.End() - Min().Start() - totalLength) <= slop);
		}
	}
}