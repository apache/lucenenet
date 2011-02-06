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

using Directory = Lucene.Net.Store.Directory;
using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;

namespace Lucene.Net.Index
{
	
	[Serializable]
	sealed public class SegmentInfos : System.Collections.ArrayList
	{
		private class AnonymousClassFindSegmentsFile : FindSegmentsFile
		{
			private void  InitBlock(SegmentInfos enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private SegmentInfos enclosingInstance;
			public SegmentInfos Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal AnonymousClassFindSegmentsFile(SegmentInfos enclosingInstance, Lucene.Net.Store.Directory Param1) : base(Param1)
			{
				InitBlock(enclosingInstance);
			}
			
			protected internal override System.Object DoBody(System.String segmentFileName)
			{
				Enclosing_Instance.Read(directory, segmentFileName);
				return null;
			}
		}

		private class AnonymousClassFindSegmentsFile1 : FindSegmentsFile
		{
			internal AnonymousClassFindSegmentsFile1(Lucene.Net.Store.Directory Param1) : base(Param1)
			{
			}
			protected internal override System.Object DoBody(System.String segmentFileName)
			{
				
				IndexInput input = directory.OpenInput(segmentFileName);
				
				int format = 0;
				long version = 0;
				try
				{
					format = input.ReadInt();
					if (format < 0)
					{
						if (format < Lucene.Net.Index.SegmentInfos.CURRENT_FORMAT)
							throw new CorruptIndexException("Unknown format version: " + format);
						version = input.ReadLong(); // read version
					}
				}
				finally
				{
					input.Close();
				}
				
				if (format < 0)
					return (long) version;
				
				// We cannot be sure about the format of the file.
				// Therefore we have to read the whole file and cannot simply seek to the version entry.
				SegmentInfos sis = new SegmentInfos();
				sis.Read(directory, segmentFileName);
				return (long) sis.GetVersion();
			}
		}
		
		
		/// <summary>The file format version, a negative number. </summary>
		/* Works since counter, the old 1st entry, is always >= 0 */
		public const int FORMAT = - 1;
		
		/// <summary>This format adds details used for lockless commits.  It differs
		/// slightly from the previous format in that file names
		/// are never re-used (write once).  Instead, each file is
		/// written to the next generation.  For example,
		/// segments_1, segments_2, etc.  This allows us to not use
		/// a commit lock.  See <a
		/// href="http://lucene.apache.org/java/docs/fileformats.html">file
		/// formats</a> for details.
		/// </summary>
		public const int FORMAT_LOCKLESS = - 2;
		
		/// <summary>This format adds a "hasSingleNormFile" flag into each segment info.
		/// See <a href="http://issues.apache.org/jira/browse/LUCENE-756">LUCENE-756</a>
		/// for details.
		/// </summary>
		public const int FORMAT_SINGLE_NORM_FILE = - 3;
		
		/// <summary>This format allows multiple segments to share a single
		/// vectors and stored fields file. 
		/// </summary>
		public const int FORMAT_SHARED_DOC_STORE = - 4;
		
		/* This must always point to the most recent file format. */
		private static readonly int CURRENT_FORMAT = FORMAT_SHARED_DOC_STORE;
		
		public int counter = 0; // used to name new segments
		/// <summary> counts how often the index has been changed by adding or deleting docs.
		/// starting with the current time in milliseconds forces to create unique version numbers.
		/// </summary>
		private long version = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
		
		private long generation = 0; // generation of the "segments_N" for the next commit
		private long lastGeneration = 0; // generation of the "segments_N" file we last successfully read
		// or wrote; this is normally the same as generation except if
		// there was an IOException that had interrupted a commit
		
		/// <summary> If non-null, information about loading segments_N files</summary>
		/// <seealso cref="setInfoStream.">
		/// </seealso>
		private static System.IO.StreamWriter infoStream;
		
		public SegmentInfo Info(int i)
		{
			return (SegmentInfo) this[i];
		}
		
		/// <summary> Get the generation (N) of the current segments_N file
		/// from a list of files.
		/// 
		/// </summary>
		/// <param name="files">-- array of file names to check
		/// </param>
		public static long GetCurrentSegmentGeneration(System.String[] files)
		{
			if (files == null)
			{
				return - 1;
			}
			long max = - 1;
			for (int i = 0; i < files.Length; i++)
			{
				System.String file = System.IO.Path.GetFileName(files[i]);
				if (file.StartsWith(IndexFileNames.SEGMENTS) && !file.Equals(IndexFileNames.SEGMENTS_GEN))
				{
					long gen = GenerationFromSegmentsFileName(file);
					if (gen > max)
					{
						max = gen;
					}
				}
			}
			return max;
		}
		
		/// <summary> Get the generation (N) of the current segments_N file
		/// in the directory.
		/// 
		/// </summary>
		/// <param name="directory">-- directory to search for the latest segments_N file
		/// </param>
		public static long GetCurrentSegmentGeneration(Directory directory)
		{
			System.String[] files = directory.List();
			if (files == null)
			{
				throw new System.IO.IOException("cannot read directory " + directory + ": list() returned null");
			}
			return GetCurrentSegmentGeneration(files);
		}
		
		/// <summary> Get the filename of the current segments_N file
		/// from a list of files.
		/// 
		/// </summary>
		/// <param name="files">-- array of file names to check
		/// </param>
		
		public static System.String GetCurrentSegmentFileName(System.String[] files)
		{
			return IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", GetCurrentSegmentGeneration(files));
		}
		
		/// <summary> Get the filename of the current segments_N file
		/// in the directory.
		/// 
		/// </summary>
		/// <param name="directory">-- directory to search for the latest segments_N file
		/// </param>
		public static System.String GetCurrentSegmentFileName(Directory directory)
		{
			return IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", GetCurrentSegmentGeneration(directory));
		}
		
		/// <summary> Get the segments_N filename in use by this segment infos.</summary>
		public System.String GetCurrentSegmentFileName()
		{
			return IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", lastGeneration);
		}
		
		/// <summary> Parse the generation off the segments file name and
		/// return it.
		/// </summary>
		public static long GenerationFromSegmentsFileName(System.String fileName)
		{
			if (fileName.Equals(IndexFileNames.SEGMENTS))
			{
				return 0;
			}
			else if (fileName.StartsWith(IndexFileNames.SEGMENTS))
			{
				return SupportClass.Number.ToInt64(fileName.Substring(1 + IndexFileNames.SEGMENTS.Length));
			}
			else
			{
				throw new System.ArgumentException("fileName \"" + fileName + "\" is not a segments file");
			}
		}
		
		
		/// <summary> Get the next segments_N filename that will be written.</summary>
		public System.String GetNextSegmentFileName()
		{
			long nextGeneration;
			
			if (generation == - 1)
			{
				nextGeneration = 1;
			}
			else
			{
				nextGeneration = generation + 1;
			}
			return IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", nextGeneration);
		}
		
		/// <summary> Read a particular segmentFileName.  Note that this may
		/// throw an IOException if a commit is in process.
		/// 
		/// </summary>
		/// <param name="directory">-- directory containing the segments file
		/// </param>
		/// <param name="segmentFileName">-- segment file to load
		/// </param>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public void  Read(Directory directory, System.String segmentFileName)
		{
			bool success = false;
			
			// Clear any previous segments:
			Clear();
			
			IndexInput input = directory.OpenInput(segmentFileName);
			
			generation = GenerationFromSegmentsFileName(segmentFileName);
			
			lastGeneration = generation;
			
			try
			{
				int format = input.ReadInt();
				if (format < 0)
				{
					// file contains explicit format info
					// check that it is a format we can understand
					if (format < CURRENT_FORMAT)
						throw new CorruptIndexException("Unknown format version: " + format);
					version = input.ReadLong(); // read version
					counter = input.ReadInt(); // read counter
				}
				else
				{
					// file is in old format without explicit format info
					counter = format;
				}
				
				for (int i = input.ReadInt(); i > 0; i--)
				{
					// read segmentInfos
					Add(new SegmentInfo(directory, format, input));
				}
				
				if (format >= 0)
				{
					// in old format the version number may be at the end of the file
					if (input.GetFilePointer() >= input.Length())
						version = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
					// old file format without version number
					else
						version = input.ReadLong(); // read version
				}
				success = true;
			}
			finally
			{
				input.Close();
				if (!success)
				{
					// Clear any segment infos we had loaded so we
					// have a clean slate on retry:
					Clear();
				}
			}
		}
		
		/// <summary> This version of read uses the retry logic (for lock-less
		/// commits) to find the right segments file to load.
		/// </summary>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public void  Read(Directory directory)
		{
			
			generation = lastGeneration = - 1;
			
			new AnonymousClassFindSegmentsFile(this, directory).Run();
		}
		
		public void  Write(Directory directory)
		{
			
			System.String segmentFileName = GetNextSegmentFileName();
			
			// Always advance the generation on write:
			if (generation == - 1)
			{
				generation = 1;
			}
			else
			{
				generation++;
			}
			
			IndexOutput output = directory.CreateOutput(segmentFileName);
			
			bool success = false;
			
			try
			{
				output.WriteInt(CURRENT_FORMAT); // write FORMAT
				output.WriteLong(++version); // every write changes
				// the index
				output.WriteInt(counter); // write counter
				output.WriteInt(Count); // write infos
				for (int i = 0; i < Count; i++)
				{
					Info(i).Write(output);
				}
			}
			finally
			{
				try
				{
					output.Close();
					success = true;
				}
				finally
				{
					if (!success)
					{
						// Try not to leave a truncated segments_N file in
						// the index:
						directory.DeleteFile(segmentFileName);
					}
				}
			}
			
			try
			{
				output = directory.CreateOutput(IndexFileNames.SEGMENTS_GEN);
				try
				{
					output.WriteInt(FORMAT_LOCKLESS);
					output.WriteLong(generation);
					output.WriteLong(generation);
				}
				finally
				{
					output.Close();
				}
			}
			catch (System.IO.IOException e)
			{
				// It's OK if we fail to write this file since it's
				// used only as one of the retry fallbacks.
			}
			
			lastGeneration = generation;
		}
		
		/// <summary> Returns a copy of this instance, also copying each
		/// SegmentInfo.
		/// </summary>
		
		public override System.Object Clone()
		{
			SegmentInfos si = new SegmentInfos();
			for (int i = 0; i < base.Count; i++)
			{
				si.Add(((SegmentInfo) base[i]).Clone());
			}
			si.generation = this.generation;
			si.lastGeneration = this.lastGeneration;
			return si;
		}

        private SegmentInfos(SegmentInfos si) : base(si)
        {
        }

        public SegmentInfos()
        {
        }
		
		/// <summary> version number when this SegmentInfos was generated.</summary>
		public long GetVersion()
		{
			return version;
		}
		public long GetGeneration()
		{
			return generation;
		}
		public long GetLastGeneration()
		{
			return lastGeneration;
		}
		
		/// <summary> Current version number from segments file.</summary>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public static long ReadCurrentVersion(Directory directory)
		{
			
			return (long) ((System.Int64) new AnonymousClassFindSegmentsFile1(directory).Run());
		}
		
		/// <summary>If non-null, information about retries when loading
		/// the segments file will be printed to this.
		/// </summary>
		public static void  SetInfoStream(System.IO.StreamWriter infoStream)
		{
			SegmentInfos.infoStream = infoStream;
		}
		
		/* Advanced configuration of retry logic in loading
		segments_N file */
		private static int defaultGenFileRetryCount = 10;
		private static int defaultGenFileRetryPauseMsec = 50;
		private static int defaultGenLookaheadCount = 10;
		
		/// <summary> Advanced: set how many times to try loading the
		/// segments.gen file contents to determine current segment
		/// generation.  This file is only referenced when the
		/// primary method (listing the directory) fails.
		/// </summary>
		public static void  SetDefaultGenFileRetryCount(int count)
		{
			defaultGenFileRetryCount = count;
		}
		
		/// <seealso cref="setDefaultGenFileRetryCount">
		/// </seealso>
		public static int GetDefaultGenFileRetryCount()
		{
			return defaultGenFileRetryCount;
		}
		
		/// <summary> Advanced: set how many milliseconds to pause in between
		/// attempts to load the segments.gen file.
		/// </summary>
		public static void  SetDefaultGenFileRetryPauseMsec(int msec)
		{
			defaultGenFileRetryPauseMsec = msec;
		}
		
		/// <seealso cref="setDefaultGenFileRetryPauseMsec">
		/// </seealso>
		public static int GetDefaultGenFileRetryPauseMsec()
		{
			return defaultGenFileRetryPauseMsec;
		}
		
		/// <summary> Advanced: set how many times to try incrementing the
		/// gen when loading the segments file.  This only runs if
		/// the primary (listing directory) and secondary (opening
		/// segments.gen file) methods fail to find the segments
		/// file.
		/// </summary>
		public static void  SetDefaultGenLookaheadCount(int count)
		{
			defaultGenLookaheadCount = count;
		}
		/// <seealso cref="setDefaultGenLookaheadCount">
		/// </seealso>
		public static int GetDefaultGenLookahedCount()
		{
			return defaultGenLookaheadCount;
		}
		
		/// <seealso cref="setInfoStream">
		/// </seealso>
		public static System.IO.StreamWriter GetInfoStream()
		{
			return infoStream;
		}
		
		private static void  Message(System.String message)
		{
			if (infoStream != null)
			{
				infoStream.WriteLine("SIS [" + SupportClass.ThreadClass.Current().Name + "]: " + message);
			}
		}
		
		/// <summary> Utility class for executing code that needs to do
		/// something with the current segments file.  This is
		/// necessary with lock-less commits because from the time
		/// you locate the current segments file name, until you
		/// actually open it, read its contents, or check modified
		/// time, etc., it could have been deleted due to a writer
		/// commit finishing.
		/// </summary>
		public abstract class FindSegmentsFile
		{
			
			internal System.IO.FileInfo fileDirectory;
			internal Directory directory;
			
			public FindSegmentsFile(System.IO.FileInfo directory)
			{
				this.fileDirectory = directory;
			}
			
			public FindSegmentsFile(Directory directory)
			{
				this.directory = directory;
			}
			
			public System.Object Run()
			{
				System.String segmentFileName = null;
				long lastGen = - 1;
				long gen = 0;
				int genLookaheadCount = 0;
				System.IO.IOException exc = null;
				bool retry = false;
				
				int method = 0;
				
				// Loop until we succeed in calling doBody() without
				// hitting an IOException.  An IOException most likely
				// means a commit was in process and has finished, in
				// the time it took us to load the now-old infos files
				// (and segments files).  It's also possible it's a
				// true error (corrupt index).  To distinguish these,
				// on each retry we must see "forward progress" on
				// which generation we are trying to load.  If we
				// don't, then the original error is real and we throw
				// it.
				
				// We have three methods for determining the current
				// generation.  We try the first two in parallel, and
				// fall back to the third when necessary.
				
				while (true)
				{
					
					if (0 == method)
					{
						
						// Method 1: list the directory and use the highest
						// segments_N file.  This method works well as long
						// as there is no stale caching on the directory
						// contents (NOTE: NFS clients often have such stale
						// caching):
						System.String[] files = null;
						
						long genA = - 1;
						
						if (directory != null)
							files = directory.List();
						else
						{
							files = System.IO.Directory.GetFileSystemEntries(fileDirectory.FullName);
						}
						
						if (files != null)
							genA = Lucene.Net.Index.SegmentInfos.GetCurrentSegmentGeneration(files);
						
						Lucene.Net.Index.SegmentInfos.Message("directory listing genA=" + genA);
						
						// Method 2: open segments.gen and read its
						// contents.  Then we take the larger of the two
						// gen's.  This way, if either approach is hitting
						// a stale cache (NFS) we have a better chance of
						// getting the right generation.
						long genB = - 1;
						if (directory != null)
						{
							for (int i = 0; i < Lucene.Net.Index.SegmentInfos.defaultGenFileRetryCount; i++)
							{
								IndexInput genInput = null;
                                try
                                {
                                    genInput = directory.OpenInput(IndexFileNames.SEGMENTS_GEN);
                                }
                                catch (System.IO.FileNotFoundException e)
                                {
                                    Lucene.Net.Index.SegmentInfos.Message("segments.gen open: FileNotFoundException " + e);
                                    break;
                                }
                                catch (System.IO.IOException e)
                                {
                                    Lucene.Net.Index.SegmentInfos.Message("segments.gen open: IOException " + e);
                                }
                                catch (UnauthorizedAccessException e)
                                {
                                    Lucene.Net.Index.SegmentInfos.Message("segments.gen open: UnauthorizedAccessException " + e);
                                    System.Threading.Thread.Sleep(10);
                                }
								
								if (genInput != null)
								{
									try
									{
										int version = genInput.ReadInt();
										if (version == Lucene.Net.Index.SegmentInfos.FORMAT_LOCKLESS)
										{
											long gen0 = genInput.ReadLong();
											long gen1 = genInput.ReadLong();
											Lucene.Net.Index.SegmentInfos.Message("fallback check: " + gen0 + "; " + gen1);
											if (gen0 == gen1)
											{
												// The file is consistent.
												genB = gen0;
												break;
											}
										}
									}
									catch (System.IO.IOException err2)
									{
										// will retry
									}
									finally
									{
										genInput.Close();
									}
								}
								try
								{
									System.Threading.Thread.Sleep(new System.TimeSpan((System.Int64) 10000 * Lucene.Net.Index.SegmentInfos.defaultGenFileRetryPauseMsec));
								}
								catch (System.Threading.ThreadInterruptedException e)
								{
									// will retry
								}
							}
						}
						
						Lucene.Net.Index.SegmentInfos.Message(IndexFileNames.SEGMENTS_GEN + " check: genB=" + genB);
						
						// Pick the larger of the two gen's:
						if (genA > genB)
							gen = genA;
						else
							gen = genB;
						
						if (gen == - 1)
						{
							// Neither approach found a generation
							System.String s;
							if (files != null)
							{
								s = "";
								for (int i = 0; i < files.Length; i++)
									s += (" " + files[i]);
							}
							else
								s = " null";
							throw new System.IO.FileNotFoundException("no segments* file found in " + directory + ": files:" + s);
						}
					}
					
					// Third method (fallback if first & second methods
					// are not reliable): since both directory cache and
					// file contents cache seem to be stale, just
					// advance the generation.
					if (1 == method || (0 == method && lastGen == gen && retry))
					{
						
						method = 1;
						
						if (genLookaheadCount < Lucene.Net.Index.SegmentInfos.defaultGenLookaheadCount)
						{
							gen++;
							genLookaheadCount++;
							Lucene.Net.Index.SegmentInfos.Message("look ahead increment gen to " + gen);
						}
					}
					
					if (lastGen == gen)
					{
						
						// This means we're about to try the same
						// segments_N last tried.  This is allowed,
						// exactly once, because writer could have been in
						// the process of writing segments_N last time.
						
						if (retry)
						{
							// OK, we've tried the same segments_N file
							// twice in a row, so this must be a real
							// error.  We throw the original exception we
							// got.
							throw exc;
						}
						else
						{
							retry = true;
						}
					}
					else
					{
						// Segment file has advanced since our last loop, so
						// reset retry:
						retry = false;
					}
					
					lastGen = gen;
					
					segmentFileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen);
					
					try
					{
						System.Object v = DoBody(segmentFileName);
						if (exc != null)
						{
							Lucene.Net.Index.SegmentInfos.Message("success on " + segmentFileName);
						}
						return v;
					}
					catch (System.IO.IOException err)
					{
						
						// Save the original root cause:
						if (exc == null)
						{
							exc = err;
						}
						
						Lucene.Net.Index.SegmentInfos.Message("primary Exception on '" + segmentFileName + "': " + err + "'; will retry: retry=" + retry + "; gen = " + gen);
						
						if (!retry && gen > 1)
						{
							
							// This is our first time trying this segments
							// file (because retry is false), and, there is
							// possibly a segments_(N-1) (because gen > 1).
							// So, check if the segments_(N-1) exists and
							// try it if so:
							System.String prevSegmentFileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen - 1);
							
							bool prevExists;
							if (directory != null)
								prevExists = directory.FileExists(prevSegmentFileName);
							else
							{
								bool tmpBool;
                                if (System.IO.File.Exists(new System.IO.FileInfo(fileDirectory.FullName + System.IO.Path.DirectorySeparatorChar + prevSegmentFileName).FullName))
									tmpBool = true;
								else
                                    tmpBool = System.IO.Directory.Exists(new System.IO.FileInfo(fileDirectory.FullName + System.IO.Path.DirectorySeparatorChar + prevSegmentFileName).FullName);
								prevExists = tmpBool;
							}
							
							if (prevExists)
							{
								Lucene.Net.Index.SegmentInfos.Message("fallback to prior segment file '" + prevSegmentFileName + "'");
								try
								{
									System.Object v = DoBody(prevSegmentFileName);
									if (exc != null)
									{
										Lucene.Net.Index.SegmentInfos.Message("success on fallback " + prevSegmentFileName);
									}
									return v;
								}
								catch (System.IO.IOException err2)
								{
									Lucene.Net.Index.SegmentInfos.Message("secondary Exception on '" + prevSegmentFileName + "': " + err2 + "'; will retry");
								}
							}
						}
					}
				}
			}
			
			/// <summary> Subclass must implement this.  The assumption is an
			/// IOException will be thrown if something goes wrong
			/// during the processing that could have been caused by
			/// a writer committing.
			/// </summary>
			protected internal abstract System.Object DoBody(System.String segmentFileName);
		}
		
		/// <summary> Returns a new SegmentInfos containg the SegmentInfo
		/// instances in the specified range first (inclusive) to
		/// last (exclusive), so total number of segments returned
		/// is last-first.
		/// </summary>
		public SegmentInfos Range(int first, int last)
		{
			SegmentInfos infos = new SegmentInfos();
			infos.AddRange((System.Collections.IList) ((System.Collections.ArrayList) this).GetRange(first, last - first));
			return infos;
		}
	}
}
