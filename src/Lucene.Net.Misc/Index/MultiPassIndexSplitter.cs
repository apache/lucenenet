using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace org.apache.lucene.index
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


	using OpenMode = org.apache.lucene.index.IndexWriterConfig.OpenMode;
	using Directory = org.apache.lucene.store.Directory;
	using FSDirectory = org.apache.lucene.store.FSDirectory;
	using FixedBitSet = org.apache.lucene.util.FixedBitSet;
	using Bits = org.apache.lucene.util.Bits;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// This tool splits input index into multiple equal parts. The method employed
	/// here uses <seealso cref="IndexWriter#addIndexes(IndexReader[])"/> where the input data
	/// comes from the input index with artificially applied deletes to the document
	/// id-s that fall outside the selected partition.
	/// <para>Note 1: Deletes are only applied to a buffered list of deleted docs and
	/// don't affect the source index - this tool works also with read-only indexes.
	/// </para>
	/// <para>Note 2: the disadvantage of this tool is that source index needs to be
	/// read as many times as there are parts to be created, hence the name of this
	/// tool.
	/// 
	/// </para>
	/// <para><b>NOTE</b>: this tool is unaware of documents added
	/// atomically via <seealso cref="IndexWriter#addDocuments"/> or {@link
	/// IndexWriter#updateDocuments}, which means it can easily
	/// break up such document groups.
	/// </para>
	/// </summary>
	public class MultiPassIndexSplitter
	{

	  /// <summary>
	  /// Split source index into multiple parts. </summary>
	  /// <param name="in"> source index, can have deletions, can have
	  /// multiple segments (or multiple readers). </param>
	  /// <param name="outputs"> list of directories where the output parts will be stored. </param>
	  /// <param name="seq"> if true, then the source index will be split into equal
	  /// increasing ranges of document id-s. If false, source document id-s will be
	  /// assigned in a deterministic round-robin fashion to one of the output splits. </param>
	  /// <exception cref="IOException"> If there is a low-level I/O error </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void split(org.apache.lucene.util.Version version, IndexReader in, org.apache.lucene.store.Directory[] outputs, boolean seq) throws java.io.IOException
	  public virtual void Split(Version version, IndexReader @in, Directory[] outputs, bool seq)
	  {
		if (outputs == null || outputs.Length < 2)
		{
		  throw new IOException("Invalid number of outputs.");
		}
		if (@in == null || @in.numDocs() < 2)
		{
		  throw new IOException("Not enough documents for splitting");
		}
		int numParts = outputs.Length;
		// wrap a potentially read-only input
		// this way we don't have to preserve original deletions because neither
		// deleteDocument(int) or undeleteAll() is applied to the wrapped input index.
		FakeDeleteIndexReader input = new FakeDeleteIndexReader(@in);
		int maxDoc = input.maxDoc();
		int partLen = maxDoc / numParts;
		for (int i = 0; i < numParts; i++)
		{
		  input.undeleteAll();
		  if (seq) // sequential range
		  {
			int lo = partLen * i;
			int hi = lo + partLen;
			// below range
			for (int j = 0; j < lo; j++)
			{
			  input.deleteDocument(j);
			}
			// above range - last part collects all id-s that remained due to
			// integer rounding errors
			if (i < numParts - 1)
			{
			  for (int j = hi; j < maxDoc; j++)
			  {
				input.deleteDocument(j);
			  }
			}
		  }
		  else
		  {
			// round-robin
			for (int j = 0; j < maxDoc; j++)
			{
			  if ((j + numParts - i) % numParts != 0)
			  {
				input.deleteDocument(j);
			  }
			}
		  }
		  IndexWriter w = new IndexWriter(outputs[i], new IndexWriterConfig(version, null)
			 .setOpenMode(OpenMode.CREATE));
		  Console.Error.WriteLine("Writing part " + (i + 1) + " ...");
		  // pass the subreaders directly, as our wrapper's numDocs/hasDeletetions are not up-to-date
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<? extends FakeDeleteAtomicIndexReader> sr = input.getSequentialSubReaders();
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
		  IList<?> sr = input.SequentialSubReaders;
		  w.addIndexes(sr.ToArray()); // TODO: maybe take List<IR> here?
		  w.close();
		}
		Console.Error.WriteLine("Done.");
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("deprecation") public static void main(String[] args) throws Exception
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public static void Main(string[] args)
	  {
		if (args.Length < 5)
		{
		  Console.Error.WriteLine("Usage: MultiPassIndexSplitter -out <outputDir> -num <numParts> [-seq] <inputIndex1> [<inputIndex2 ...]");
		  Console.Error.WriteLine("\tinputIndex\tpath to input index, multiple values are ok");
		  Console.Error.WriteLine("\t-out ouputDir\tpath to output directory to contain partial indexes");
		  Console.Error.WriteLine("\t-num numParts\tnumber of parts to produce");
		  Console.Error.WriteLine("\t-seq\tsequential docid-range split (default is round-robin)");
		  Environment.Exit(-1);
		}
		List<IndexReader> indexes = new List<IndexReader>();
		string outDir = null;
		int numParts = -1;
		bool seq = false;
		for (int i = 0; i < args.Length; i++)
		{
		  if (args[i].Equals("-out"))
		  {
			outDir = args[++i];
		  }
		  else if (args[i].Equals("-num"))
		  {
			numParts = Convert.ToInt32(args[++i]);
		  }
		  else if (args[i].Equals("-seq"))
		  {
			seq = true;
		  }
		  else
		  {
			File file = new File(args[i]);
			if (!file.exists() || !file.Directory)
			{
			  Console.Error.WriteLine("Invalid input path - skipping: " + file);
			  continue;
			}
			Directory dir = FSDirectory.open(new File(args[i]));
			try
			{
			  if (!DirectoryReader.indexExists(dir))
			  {
				Console.Error.WriteLine("Invalid input index - skipping: " + file);
				continue;
			  }
			}
			catch (Exception)
			{
			  Console.Error.WriteLine("Invalid input index - skipping: " + file);
			  continue;
			}
			indexes.Add(DirectoryReader.open(dir));
		  }
		}
		if (outDir == null)
		{
		  throw new Exception("Required argument missing: -out outputDir");
		}
		if (numParts < 2)
		{
		  throw new Exception("Invalid value of required argument: -num numParts");
		}
		if (indexes.Count == 0)
		{
		  throw new Exception("No input indexes to process");
		}
		File @out = new File(outDir);
		if (!@out.mkdirs())
		{
		  throw new Exception("Can't create output directory: " + @out);
		}
		Directory[] dirs = new Directory[numParts];
		for (int i = 0; i < numParts; i++)
		{
		  dirs[i] = FSDirectory.open(new File(@out, "part-" + i));
		}
		MultiPassIndexSplitter splitter = new MultiPassIndexSplitter();
		IndexReader input;
		if (indexes.Count == 1)
		{
		  input = indexes[0];
		}
		else
		{
		  input = new MultiReader(indexes.ToArray());
		}
		splitter.Split(Version.LUCENE_CURRENT, input, dirs, seq);
	  }

	  /// <summary>
	  /// This class emulates deletions on the underlying index.
	  /// </summary>
	  private sealed class FakeDeleteIndexReader : BaseCompositeReader<FakeDeleteAtomicIndexReader>
	  {

		public FakeDeleteIndexReader(IndexReader reader) : base(initSubReaders(reader))
		{
		}

		internal static FakeDeleteAtomicIndexReader[] initSubReaders(IndexReader reader)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<AtomicReaderContext> leaves = reader.leaves();
		  IList<AtomicReaderContext> leaves = reader.leaves();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final FakeDeleteAtomicIndexReader[] subs = new FakeDeleteAtomicIndexReader[leaves.size()];
		  FakeDeleteAtomicIndexReader[] subs = new FakeDeleteAtomicIndexReader[leaves.Count];
		  int i = 0;
		  foreach (AtomicReaderContext ctx in leaves)
		  {
			subs[i++] = new FakeDeleteAtomicIndexReader(ctx.reader());
		  }
		  return subs;
		}

		public void deleteDocument(int docID)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int i = readerIndex(docID);
		  int i = readerIndex(docID);
		  SequentialSubReaders.get(i).deleteDocument(docID - readerBase(i));
		}

		public void undeleteAll()
		{
		  foreach (FakeDeleteAtomicIndexReader r in SequentialSubReaders)
		  {
			r.undeleteAll();
		  }
		}

		protected internal override void doClose()
		{
		}

		// no need to override numDocs/hasDeletions,
		// as we pass the subreaders directly to IW.addIndexes().
	  }

	  private sealed class FakeDeleteAtomicIndexReader : FilterAtomicReader
	  {
		internal FixedBitSet liveDocs;

		public FakeDeleteAtomicIndexReader(AtomicReader reader) : base(reader)
		{
		  undeleteAll(); // initialize main bitset
		}

		public override int numDocs()
		{
		  return liveDocs.cardinality();
		}

		public void undeleteAll()
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int maxDoc = in.maxDoc();
		  int maxDoc = @in.maxDoc();
		  liveDocs = new FixedBitSet(@in.maxDoc());
		  if (@in.hasDeletions())
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.Bits oldLiveDocs = in.getLiveDocs();
			Bits oldLiveDocs = @in.LiveDocs;
			Debug.Assert(oldLiveDocs != null);
			// this loop is a little bit ineffective, as Bits has no nextSetBit():
			for (int i = 0; i < maxDoc; i++)
			{
			  if (oldLiveDocs.get(i))
			  {
				  liveDocs.set(i);
			  }
			}
		  }
		  else
		  {
			// mark all docs as valid
			liveDocs.set(0, maxDoc);
		  }
		}

		public void deleteDocument(int n)
		{
		  liveDocs.clear(n);
		}

		public override Bits LiveDocs
		{
			get
			{
			  return liveDocs;
			}
		}
	  }
	}

}