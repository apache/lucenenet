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
using Pattern = System.Text.RegularExpressions.Regex;

namespace Lucene.Net.Index
{
	
	/// <summary> A utility class (used by both IndexReader and
	/// IndexWriter) to keep track of files that need to be
	/// deleted because they are no longer referenced by the
	/// index.
	/// </summary>
	sealed public class IndexFileDeleter
	{
		private System.Collections.ArrayList deletable;
		private System.Collections.Hashtable pending;
		private Directory directory;
		private SegmentInfos segmentInfos;
		private System.IO.TextWriter infoStream;
		
		public IndexFileDeleter(SegmentInfos segmentInfos, Directory directory)
		{
			this.segmentInfos = segmentInfos;
			this.directory = directory;
		}
		internal void  SetSegmentInfos(SegmentInfos segmentInfos)
		{
			this.segmentInfos = segmentInfos;
		}
		internal SegmentInfos GetSegmentInfos()
		{
			return segmentInfos;
		}
		
		internal void  SetInfoStream(System.IO.TextWriter infoStream)
		{
			this.infoStream = infoStream;
		}
		
		/// <summary>Determine index files that are no longer referenced
		/// and therefore should be deleted.  This is called once
		/// (by the writer), and then subsequently we add onto
		/// deletable any files that are no longer needed at the
		/// point that we create the unused file (eg when merging
		/// segments), and we only remove from deletable when a
		/// file is successfully deleted.
		/// </summary>
		
		public void  FindDeletableFiles()
		{
			
			// Gather all "current" segments:
			System.Collections.Hashtable current = new System.Collections.Hashtable();
			for (int j = 0; j < segmentInfos.Count; j++)
			{
				SegmentInfo segmentInfo = (SegmentInfo) segmentInfos[j];
				current[segmentInfo.name] = segmentInfo;
			}
			
			// Then go through all files in the Directory that are
			// Lucene index files, and add to deletable if they are
			// not referenced by the current segments info:
			
			System.String segmentsInfosFileName = segmentInfos.GetCurrentSegmentFileName();
			IndexFileNameFilter filter = IndexFileNameFilter.GetFilter();
			
			System.String[] files = directory.List();
			
			for (int i = 0; i < files.Length; i++)
			{
				
				if (filter.Accept(null, files[i]) && !files[i].Equals(segmentsInfosFileName) && !files[i].Equals(IndexFileNames.SEGMENTS_GEN))
				{
					
					System.String segmentName;
					System.String extension;
					
					// First remove any extension:
					int loc = files[i].IndexOf((System.Char) '.');
					if (loc != - 1)
					{
						extension = files[i].Substring(1 + loc);
						segmentName = files[i].Substring(0, (loc) - (0));
					}
					else
					{
						extension = null;
						segmentName = files[i];
					}
					
					// Then, remove any generation count:
					loc = segmentName.IndexOf((System.Char) '_', 1);
					if (loc != - 1)
					{
						segmentName = segmentName.Substring(0, (loc) - (0));
					}
					
					// Delete this file if it's not a "current" segment,
					// or, it is a single index file but there is now a
					// corresponding compound file:
					bool doDelete = false;
					
					if (!current.ContainsKey(segmentName))
					{
						// Delete if segment is not referenced:
						doDelete = true;
					}
					else
					{
						// OK, segment is referenced, but file may still
						// be orphan'd:
						SegmentInfo info = (SegmentInfo) current[segmentName];
						
						if (filter.IsCFSFile(files[i]) && info.GetUseCompoundFile())
						{
							// This file is in fact stored in a CFS file for
							// this segment:
							doDelete = true;
						}
						else
						{
                            Pattern p = new System.Text.RegularExpressions.Regex("s\\d+");
							
							if ("del".Equals(extension))
							{
								// This is a _segmentName_N.del file:
								if (!files[i].Equals(info.GetDelFileName()))
								{
									// If this is a seperate .del file, but it
									// doesn't match the current del filename for
									// this segment, then delete it:
									doDelete = true;
								}
							}
							else if (extension != null && extension.StartsWith("s") && p.Match(extension).Success)
							{

								int field = System.Int32.Parse(extension.Substring(1));
								// This is a _segmentName_N.sX file:
								if (!files[i].Equals(info.GetNormFileName(field)))
								{
									// This is an orphan'd separate norms file:
									doDelete = true;
								}
							}
							else if ("cfs".Equals(extension) && !info.GetUseCompoundFile())
							{
								// This is a partially written
								// _segmentName.cfs:
								doDelete = true;
							}
						}
					}
					
					if (doDelete)
					{
						AddDeletableFile(files[i]);
						if (infoStream != null)
						{
							infoStream.WriteLine("IndexFileDeleter: file \"" + files[i] + "\" is unreferenced in index and will be deleted on next commit");
						}
					}
				}
			}
		}
		
		/*
		* Some operating systems (e.g. Windows) don't permit a file to be deleted
		* while it is opened for read (e.g. by another process or thread). So we
		* assume that when a delete fails it is because the file is open in another
		* process, and queue the file for subsequent deletion.
		*/
		
		internal void  DeleteSegments(System.Collections.ArrayList segments)
		{
			
			DeleteFiles(); // try to delete files that we couldn't before
			
			for (int i = 0; i < segments.Count; i++)
			{
				SegmentReader reader = (SegmentReader) segments[i];
				if (reader.Directory() == this.directory)
					DeleteFiles(reader.Files());
				// try to delete our files
				else
					DeleteFiles(reader.Files(), reader.Directory()); // delete other files
			}
		}
		
		/// <summary> Delete these segments, as long as they are not listed
		/// in protectedSegments.  If they are, then, instead, add
		/// them to the pending set.
		/// </summary>
		
		internal void  DeleteSegments(System.Collections.ArrayList segments, System.Collections.Hashtable protectedSegments)
		{
			
			DeleteFiles(); // try to delete files that we couldn't before
			
			for (int i = 0; i < segments.Count; i++)
			{
				SegmentReader reader = (SegmentReader) segments[i];
				if (reader.Directory() == this.directory)
				{
					if (protectedSegments.Contains(reader.GetSegmentName()))
					{
						AddPendingFiles(reader.Files()); // record these for deletion on commit
					}
					else
					{
						DeleteFiles(reader.Files()); // try to delete our files
					}
				}
				else
				{
					DeleteFiles(reader.Files(), reader.Directory()); // delete other files
				}
			}
		}
		
		internal void  DeleteFiles(System.Collections.ArrayList files, Directory directory)
		{
			for (int i = 0; i < files.Count; i++)
				directory.DeleteFile((System.String) files[i]);
		}
		
		internal void  DeleteFiles(System.Collections.ArrayList files)
		{
			DeleteFiles(); // try to delete files that we couldn't before
			for (int i = 0; i < files.Count; i++)
			{
				DeleteFile((System.String) files[i]);
			}
		}
		
		internal void  DeleteFile(System.String file)
		{
			try
			{
				directory.DeleteFile(file); // try to delete each file
			}
			catch (System.IO.IOException e)
			{
				// if delete fails
				if (directory.FileExists(file))
				{
					if (infoStream != null)
					{
						infoStream.WriteLine("IndexFileDeleter: unable to remove file \"" + file + "\": " + e.ToString() + "; Will re-try later.");
					}
					AddDeletableFile(file); // add to deletable
				}
			}
		}
		
		internal void  ClearPendingFiles()
		{
			pending = null;
		}
		
		/*
		Record that the files for these segments should be
		deleted, once the pending deletes are committed.
		*/
		internal void  AddPendingSegments(System.Collections.ArrayList segments)
		{
			for (int i = 0; i < segments.Count; i++)
			{
				SegmentReader reader = (SegmentReader) segments[i];
				if (reader.Directory() == this.directory)
				{
					AddPendingFiles(reader.Files());
				}
			}
		}
		
		/*
		Record list of files for deletion, but do not delete
		them until commitPendingFiles is called.
		*/
		internal void  AddPendingFiles(System.Collections.ArrayList files)
		{
			for (int i = 0; i < files.Count; i++)
			{
				AddPendingFile((System.String) files[i]);
			}
		}
		
		/*
		Record a file for deletion, but do not delete it until
		commitPendingFiles is called.
		*/
		internal void  AddPendingFile(System.String fileName)
		{
			if (pending == null)
			{
				pending = new System.Collections.Hashtable();
			}
            if (pending.ContainsKey(fileName) == false)
            {
                pending.Add(fileName, fileName);
            }
		}
		
		internal void  CommitPendingFiles()
		{
			if (pending != null)
			{
				if (deletable == null)
				{
					deletable = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
				}
				System.Collections.IEnumerator it = pending.GetEnumerator();
				while (it.MoveNext())
				{
					deletable.Add(((System.Collections.DictionaryEntry)(it.Current)).Value);
				}
				pending = null;
				DeleteFiles();
			}
		}
		
		internal void  AddDeletableFile(System.String fileName)
		{
			if (deletable == null)
			{
				deletable = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
			}
			deletable.Add(fileName);
		}
		
		public void  DeleteFiles()
		{
			if (deletable != null)
			{
				System.Collections.ArrayList oldDeletable = deletable;
				deletable = null;
				DeleteFiles(oldDeletable); // try to delete deletable
			}
		}
	}
}