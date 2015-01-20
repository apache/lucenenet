/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Index
{
	/// <summary>This tool splits input index into multiple equal parts.</summary>
	/// <remarks>
	/// This tool splits input index into multiple equal parts. The method employed
	/// here uses
	/// <see cref="IndexWriter.AddIndexes(IndexReader[])">IndexWriter.AddIndexes(IndexReader[])
	/// 	</see>
	/// where the input data
	/// comes from the input index with artificially applied deletes to the document
	/// id-s that fall outside the selected partition.
	/// <p>Note 1: Deletes are only applied to a buffered list of deleted docs and
	/// don't affect the source index - this tool works also with read-only indexes.
	/// <p>Note 2: the disadvantage of this tool is that source index needs to be
	/// read as many times as there are parts to be created, hence the name of this
	/// tool.
	/// <p><b>NOTE</b>: this tool is unaware of documents added
	/// atomically via
	/// <see cref="IndexWriter.AddDocuments(Sharpen.Iterable{T})">IndexWriter.AddDocuments(Sharpen.Iterable&lt;T&gt;)
	/// 	</see>
	/// or
	/// <see cref="IndexWriter.UpdateDocuments(Term, Sharpen.Iterable{T})">IndexWriter.UpdateDocuments(Term, Sharpen.Iterable&lt;T&gt;)
	/// 	</see>
	/// , which means it can easily
	/// break up such document groups.
	/// </remarks>
	public class MultiPassIndexSplitter
	{
		/// <summary>Split source index into multiple parts.</summary>
		/// <remarks>Split source index into multiple parts.</remarks>
		/// <param name="in">
		/// source index, can have deletions, can have
		/// multiple segments (or multiple readers).
		/// </param>
		/// <param name="outputs">list of directories where the output parts will be stored.</param>
		/// <param name="seq">
		/// if true, then the source index will be split into equal
		/// increasing ranges of document id-s. If false, source document id-s will be
		/// assigned in a deterministic round-robin fashion to one of the output splits.
		/// </param>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		public virtual void Split(Version version, IndexReader @in, Directory[] outputs, 
			bool seq)
		{
			if (outputs == null || outputs.Length < 2)
			{
				throw new IOException("Invalid number of outputs.");
			}
			if (@in == null || @in.NumDocs() < 2)
			{
				throw new IOException("Not enough documents for splitting");
			}
			int numParts = outputs.Length;
			// wrap a potentially read-only input
			// this way we don't have to preserve original deletions because neither
			// deleteDocument(int) or undeleteAll() is applied to the wrapped input index.
			MultiPassIndexSplitter.FakeDeleteIndexReader input = new MultiPassIndexSplitter.FakeDeleteIndexReader
				(@in);
			int maxDoc = input.MaxDoc();
			int partLen = maxDoc / numParts;
			for (int i = 0; i < numParts; i++)
			{
				input.UndeleteAll();
				if (seq)
				{
					// sequential range
					int lo = partLen * i;
					int hi = lo + partLen;
					// below range
					for (int j = 0; j < lo; j++)
					{
						input.DeleteDocument(j);
					}
					// above range - last part collects all id-s that remained due to
					// integer rounding errors
					if (i < numParts - 1)
					{
						for (int j_1 = hi; j_1 < maxDoc; j_1++)
						{
							input.DeleteDocument(j_1);
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
							input.DeleteDocument(j);
						}
					}
				}
				IndexWriter w = new IndexWriter(outputs[i], new IndexWriterConfig(version, null).
					SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
				System.Console.Error.WriteLine("Writing part " + (i + 1) + " ...");
				// pass the subreaders directly, as our wrapper's numDocs/hasDeletetions are not up-to-date
				IList<MultiPassIndexSplitter.FakeDeleteAtomicIndexReader> sr = ((IList<MultiPassIndexSplitter.FakeDeleteAtomicIndexReader
					>)input.GetSequentialSubReaders());
				w.AddIndexes(Sharpen.Collections.ToArray(sr, new IndexReader[sr.Count]));
				// TODO: maybe take List<IR> here?
				w.Close();
			}
			System.Console.Error.WriteLine("Done.");
		}

		/// <exception cref="System.Exception"></exception>
		public static void Main(string[] args)
		{
			if (args.Length < 5)
			{
				System.Console.Error.WriteLine("Usage: MultiPassIndexSplitter -out <outputDir> -num <numParts> [-seq] <inputIndex1> [<inputIndex2 ...]"
					);
				System.Console.Error.WriteLine("\tinputIndex\tpath to input index, multiple values are ok"
					);
				System.Console.Error.WriteLine("\t-out ouputDir\tpath to output directory to contain partial indexes"
					);
				System.Console.Error.WriteLine("\t-num numParts\tnumber of parts to produce");
				System.Console.Error.WriteLine("\t-seq\tsequential docid-range split (default is round-robin)"
					);
				System.Environment.Exit(-1);
			}
			AList<IndexReader> indexes = new AList<IndexReader>();
			string outDir = null;
			int numParts = -1;
			bool seq = false;
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].Equals("-out"))
				{
					outDir = args[++i];
				}
				else
				{
					if (args[i].Equals("-num"))
					{
						numParts = System.Convert.ToInt32(args[++i]);
					}
					else
					{
						if (args[i].Equals("-seq"))
						{
							seq = true;
						}
						else
						{
							FilePath file = new FilePath(args[i]);
							if (!file.Exists() || !file.IsDirectory())
							{
								System.Console.Error.WriteLine("Invalid input path - skipping: " + file);
								continue;
							}
							Directory dir = FSDirectory.Open(new FilePath(args[i]));
							try
							{
								if (!DirectoryReader.IndexExists(dir))
								{
									System.Console.Error.WriteLine("Invalid input index - skipping: " + file);
									continue;
								}
							}
							catch (Exception)
							{
								System.Console.Error.WriteLine("Invalid input index - skipping: " + file);
								continue;
							}
							indexes.AddItem(DirectoryReader.Open(dir));
						}
					}
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
			FilePath @out = new FilePath(outDir);
			if (!@out.Mkdirs())
			{
				throw new Exception("Can't create output directory: " + @out);
			}
			Directory[] dirs = new Directory[numParts];
			for (int i_1 = 0; i_1 < numParts; i_1++)
			{
				dirs[i_1] = FSDirectory.Open(new FilePath(@out, "part-" + i_1));
			}
			MultiPassIndexSplitter splitter = new MultiPassIndexSplitter();
			IndexReader input;
			if (indexes.Count == 1)
			{
				input = indexes[0];
			}
			else
			{
				input = new MultiReader(Sharpen.Collections.ToArray(indexes, new IndexReader[indexes
					.Count]));
			}
			splitter.Split(Version.LUCENE_CURRENT, input, dirs, seq);
		}

		/// <summary>This class emulates deletions on the underlying index.</summary>
		/// <remarks>This class emulates deletions on the underlying index.</remarks>
		private sealed class FakeDeleteIndexReader : BaseCompositeReader<MultiPassIndexSplitter.FakeDeleteAtomicIndexReader
			>
		{
			public FakeDeleteIndexReader(IndexReader reader) : base(InitSubReaders(reader))
			{
			}

			private static MultiPassIndexSplitter.FakeDeleteAtomicIndexReader[] InitSubReaders
				(IndexReader reader)
			{
				IList<AtomicReaderContext> leaves = reader.Leaves();
				MultiPassIndexSplitter.FakeDeleteAtomicIndexReader[] subs = new MultiPassIndexSplitter.FakeDeleteAtomicIndexReader
					[leaves.Count];
				int i = 0;
				foreach (AtomicReaderContext ctx in leaves)
				{
					subs[i++] = new MultiPassIndexSplitter.FakeDeleteAtomicIndexReader(((AtomicReader
						)ctx.Reader()));
				}
				return subs;
			}

			public void DeleteDocument(int docID)
			{
				int i = ReaderIndex(docID);
				((IList<MultiPassIndexSplitter.FakeDeleteAtomicIndexReader>)GetSequentialSubReaders
					())[i].DeleteDocument(docID - ReaderBase(i));
			}

			public void UndeleteAll()
			{
				foreach (MultiPassIndexSplitter.FakeDeleteAtomicIndexReader r in ((IList<MultiPassIndexSplitter.FakeDeleteAtomicIndexReader
					>)GetSequentialSubReaders()))
				{
					r.UndeleteAll();
				}
			}

			protected override void DoClose()
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
				UndeleteAll();
			}

			// initialize main bitset
			public override int NumDocs()
			{
				return liveDocs.Cardinality();
			}

			public void UndeleteAll()
			{
				int maxDoc = @in.MaxDoc();
				liveDocs = new FixedBitSet(@in.MaxDoc());
				if (@in.HasDeletions())
				{
					Bits oldLiveDocs = @in.GetLiveDocs();
					//HM:revisit
					//assert oldLiveDocs != null;
					// this loop is a little bit ineffective, as Bits has no nextSetBit():
					for (int i = 0; i < maxDoc; i++)
					{
						if (oldLiveDocs.Get(i))
						{
							liveDocs.Set(i);
						}
					}
				}
				else
				{
					// mark all docs as valid
					liveDocs.Set(0, maxDoc);
				}
			}

			public void DeleteDocument(int n)
			{
				liveDocs.Clear(n);
			}

			public override Bits GetLiveDocs()
			{
				return liveDocs;
			}
		}
	}
}
