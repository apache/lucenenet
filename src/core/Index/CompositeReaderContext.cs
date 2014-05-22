using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Index
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
	/// <seealso cref="IndexReaderContext"/> for <seealso cref="CompositeReader"/> instance.
	/// </summary>
	public sealed class CompositeReaderContext : IndexReaderContext
	{
	  private readonly IList<IndexReaderContext> Children_Renamed;
	  private readonly IList<AtomicReaderContext> Leaves_Renamed;
	  private readonly CompositeReader Reader_Renamed;

	  internal static CompositeReaderContext Create(CompositeReader reader)
	  {
		return (new Builder(reader)).Build();
	  }

	  /// <summary>
	  /// Creates a <seealso cref="CompositeReaderContext"/> for intermediate readers that aren't
	  /// not top-level readers in the current context
	  /// </summary>
	  internal CompositeReaderContext(CompositeReaderContext parent, CompositeReader reader, int ordInParent, int docbaseInParent, IList<IndexReaderContext> children) : this(parent, reader, ordInParent, docbaseInParent, children, null)
	  {
	  }

	  /// <summary>
	  /// Creates a <seealso cref="CompositeReaderContext"/> for top-level readers with parent set to <code>null</code>
	  /// </summary>
	  internal CompositeReaderContext(CompositeReader reader, IList<IndexReaderContext> children, IList<AtomicReaderContext> leaves) : this(null, reader, 0, 0, children, leaves)
	  {
	  }

	  private CompositeReaderContext(CompositeReaderContext parent, CompositeReader reader, int ordInParent, int docbaseInParent, IList<IndexReaderContext> children, IList<AtomicReaderContext> leaves) : base(parent, ordInParent, docbaseInParent)
	  {
		this.Children_Renamed = Collections.unmodifiableList(children);
		this.Leaves_Renamed = leaves == null ? null : Collections.unmodifiableList(leaves);
		this.Reader_Renamed = reader;
	  }

	  public override IList<AtomicReaderContext> Leaves()
	  {
		if (!IsTopLevel)
		{
		  throw new System.NotSupportedException("this is not a top-level context.");
		}
		Debug.Assert(Leaves_Renamed != null);
		return Leaves_Renamed;
	  }


	  public override IList<IndexReaderContext> Children()
	  {
		return Children_Renamed;
	  }

	  public override CompositeReader Reader()
	  {
		return Reader_Renamed;
	  }

	  private sealed class Builder
	  {
		internal readonly CompositeReader Reader;
		internal readonly IList<AtomicReaderContext> Leaves = new List<AtomicReaderContext>();
		internal int LeafDocBase = 0;

		public Builder(CompositeReader reader)
		{
		  this.Reader = reader;
		}

		public CompositeReaderContext Build()
		{
		  return (CompositeReaderContext) Build(null, Reader, 0, 0);
		}

		internal IndexReaderContext Build(CompositeReaderContext parent, IndexReader reader, int ord, int docBase)
		{
		  if (reader is AtomicReader)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final AtomicReader ar = (AtomicReader) reader;
			AtomicReader ar = (AtomicReader) reader;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final AtomicReaderContext atomic = new AtomicReaderContext(parent, ar, ord, docBase, leaves.size(), leafDocBase);
			AtomicReaderContext atomic = new AtomicReaderContext(parent, ar, ord, docBase, Leaves.Count, LeafDocBase);
			Leaves.Add(atomic);
			LeafDocBase += reader.MaxDoc();
			return atomic;
		  }
		  else
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final CompositeReader cr = (CompositeReader) reader;
			CompositeReader cr = (CompositeReader) reader;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<? extends IndexReader> sequentialSubReaders = cr.getSequentialSubReaders();
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
			IList<?> sequentialSubReaders = cr.SequentialSubReaders;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<IndexReaderContext> children = java.util.Arrays.asList(new IndexReaderContext[sequentialSubReaders.size()]);
			IList<IndexReaderContext> children = Arrays.asList(new IndexReaderContext[sequentialSubReaders.Count]);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final CompositeReaderContext newParent;
			CompositeReaderContext newParent;
			if (parent == null)
			{
			  newParent = new CompositeReaderContext(cr, children, Leaves);
			}
			else
			{
			  newParent = new CompositeReaderContext(parent, cr, ord, docBase, children);
			}
			int newDocBase = 0;
			for (int i = 0, c = sequentialSubReaders.Count; i < c; i++)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final IndexReader r = sequentialSubReaders.get(i);
			  IndexReader r = sequentialSubReaders[i];
			  children[i] = Build(newParent, r, i, newDocBase);
			  newDocBase += r.MaxDoc();
			}
			Debug.Assert(newDocBase == cr.MaxDoc());
			return newParent;
		  }
		}
	  }

	}
}