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

namespace Lucene.Net.Index
{
	
	/*
	* This class keeps track of each SegmentInfos instance that
	* is still "live", either because it corresponds to a 
	* segments_N file in the Directory (a "commit", i.e. a 
	* committed SegmentInfos) or because it's the in-memory SegmentInfos 
	* that a writer is actively updating but has not yet committed 
	* (currently this only applies when autoCommit=false in IndexWriter).
	* This class uses simple reference counting to map the live
	* SegmentInfos instances to individual files in the Directory. 
	* 
	* The same directory file may be referenced by more than
	* one IndexCommitPoints, i.e. more than one SegmentInfos.
	* Therefore we count how many commits reference each file.
	* When all the commits referencing a certain file have been
	* deleted, the refcount for that file becomes zero, and the
	* file is deleted.
	*
	* A separate deletion policy interface
	* (IndexDeletionPolicy) is consulted on creation (onInit)
	* and once per commit (onCommit), to decide when a commit
	* should be removed.
	* 
	* It is the business of the IndexDeletionPolicy to choose
	* when to delete commit points.  The actual mechanics of
	* file deletion, retrying, etc, derived from the deletion
	* of commit points is the business of the IndexFileDeleter.
	* 
	* The current default deletion policy is {@link
	* KeepOnlyLastCommitDeletionPolicy}, which removes all
	* prior commits when a new commit has completed.  This
	* matches the behavior before 2.2.
	*
	* Note that you must hold the write.lock before
	* instantiating this class.  It opens segments_N file(s)
	* directly with no retry logic.
	*/
	
	sealed public class IndexFileDeleter
	{
		
		/* Files that we tried to delete but failed (likely
		* because they are open and we are running on Windows),
		* so we will retry them again later: */
		private System.Collections.IList deletable;
		
		/* Reference count for all files in the index.  
		* Counts how many existing commits reference a file.
		* Maps String to RefCount (class below) instances: */
		private System.Collections.IDictionary refCounts = new System.Collections.Hashtable();
		
		/* Holds all commits (segments_N) currently in the index.
		* This will have just 1 commit if you are using the
		* default delete policy (KeepOnlyLastCommitDeletionPolicy).
		* Other policies may leave commit points live for longer
		* in which case this list would be longer than 1: */
		private System.Collections.ArrayList commits = new System.Collections.ArrayList();
		
		/* Holds files we had incref'd from the previous
		* non-commit checkpoint: */
		private System.Collections.IList lastFiles = new System.Collections.ArrayList();
		
		/* Commits that the IndexDeletionPolicy have decided to delete: */
		private System.Collections.IList commitsToDelete = new System.Collections.ArrayList();

        private System.IO.TextWriter infoStream;
		private Directory directory;
		private IndexDeletionPolicy policy;
		private DocumentsWriter docWriter;
		
		/// <summary>Change to true to see details of reference counts when
		/// infoStream != null 
		/// </summary>
		public static bool VERBOSE_REF_COUNTS = false;
		
		internal void  SetInfoStream(System.IO.TextWriter infoStream)
		{
			this.infoStream = infoStream;
			if (infoStream != null)
			{
				Message("setInfoStream deletionPolicy=" + policy);
			}
		}
		
		private void  Message(System.String message)
		{
			infoStream.WriteLine("IFD [" + SupportClass.ThreadClass.Current().Name + "]: " + message);
		}
		
		/// <summary> Initialize the deleter: find all previous commits in
		/// the Directory, incref the files they reference, call
		/// the policy to let it delete commits.  The incoming
		/// segmentInfos must have been loaded from a commit point
		/// and not yet modified.  This will remove any files not
		/// referenced by any of the commits.
		/// </summary>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public IndexFileDeleter(Directory directory, IndexDeletionPolicy policy, SegmentInfos segmentInfos, System.IO.TextWriter infoStream, DocumentsWriter docWriter)
		{
			
			this.docWriter = docWriter;
			this.infoStream = infoStream;
			
			if (infoStream != null)
			{
				Message("init: current segments file is \"" + segmentInfos.GetCurrentSegmentFileName() + "\"; deletionPolicy=" + policy);
			}
			
			this.policy = policy;
			this.directory = directory;
			
			// First pass: walk the files and initialize our ref
			// counts:
			long currentGen = segmentInfos.GetGeneration();
			IndexFileNameFilter filter = IndexFileNameFilter.GetFilter();
			
			System.String[] files = directory.List();
			if (files == null)
			{
				throw new System.IO.IOException("cannot read directory " + directory + ": list() returned null");
			}
			
			CommitPoint currentCommitPoint = null;
			
			for (int i = 0; i < files.Length; i++)
			{
				
				System.String fileName = files[i];
				
				if (filter.Accept(null, fileName) && !fileName.Equals(IndexFileNames.SEGMENTS_GEN))
				{
					
					// Add this file to refCounts with initial count 0:
					GetRefCount(fileName);
					
					if (fileName.StartsWith(IndexFileNames.SEGMENTS))
					{
						
						// This is a commit (segments or segments_N), and
						// it's valid (<= the max gen).  Load it, then
						// incref all files it refers to:
						if (SegmentInfos.GenerationFromSegmentsFileName(fileName) <= currentGen)
						{
							if (infoStream != null)
							{
								Message("init: load commit \"" + fileName + "\"");
							}
							SegmentInfos sis = new SegmentInfos();
							try
							{
								sis.Read(directory, fileName);
							}
							catch (System.IO.FileNotFoundException e)
							{
								// LUCENE-948: on NFS (and maybe others), if
								// you have writers switching back and forth
								// between machines, it's very likely that the
								// dir listing will be stale and will claim a
								// file segments_X exists when in fact it
								// doesn't.  So, we catch this and handle it
								// as if the file does not exist
								if (infoStream != null)
								{
									Message("init: hit FileNotFoundException when loading commit \"" + fileName + "\"; skipping this commit point");
								}
								sis = null;
							}
							if (sis != null)
							{
								CommitPoint commitPoint = new CommitPoint(this, sis);
								if (sis.GetGeneration() == segmentInfos.GetGeneration())
								{
									currentCommitPoint = commitPoint;
								}
								commits.Add(commitPoint);
								IncRef(sis, true);
							}
						}
					}
				}
			}
			
			if (currentCommitPoint == null)
			{
				// We did not in fact see the segments_N file
				// corresponding to the segmentInfos that was passed
				// in.  Yet, it must exist, because our caller holds
				// the write lock.  This can happen when the directory
				// listing was stale (eg when index accessed via NFS
				// client with stale directory listing cache).  So we
				// try now to explicitly open this commit point:
				SegmentInfos sis = new SegmentInfos();
				try
				{
					sis.Read(directory, segmentInfos.GetCurrentSegmentFileName());
				}
				catch (System.IO.IOException e)
				{
					throw new CorruptIndexException("failed to locate current segments_N file");
				}
				if (infoStream != null)
					Message("forced open of current segments file " + segmentInfos.GetCurrentSegmentFileName());
				currentCommitPoint = new CommitPoint(this, sis);
				commits.Add(currentCommitPoint);
				IncRef(sis, true);
			}
			
			// We keep commits list in sorted order (oldest to newest):
			commits.Sort();
			
			// Now delete anything with ref count at 0.  These are
			// presumably abandoned files eg due to crash of
			// IndexWriter.
			System.Collections.IEnumerator it = refCounts.Keys.GetEnumerator();
			while (it.MoveNext())
			{
				System.String fileName = (System.String) it.Current;
				RefCount rc = (RefCount) refCounts[fileName];
				if (0 == rc.count)
				{
					if (infoStream != null)
					{
						Message("init: removing unreferenced file \"" + fileName + "\"");
					}
					DeleteFile(fileName);
				}
			}
			
			// Finally, give policy a chance to remove things on
			// startup:
			policy.OnInit(commits);
			
			// It's OK for the onInit to remove the current commit
			// point; we just have to checkpoint our in-memory
			// SegmentInfos to protect those files that it uses:
			if (currentCommitPoint.deleted)
			{
				Checkpoint(segmentInfos, false);
			}
			
			DeleteCommits();
		}
		
		/// <summary> Remove the CommitPoints in the commitsToDelete List by
		/// DecRef'ing all files from each SegmentInfos.
		/// </summary>
		private void  DeleteCommits()
		{
			
			int size = commitsToDelete.Count;
			
			if (size > 0)
			{
				
				// First decref all files that had been referred to by
				// the now-deleted commits:
				for (int i = 0; i < size; i++)
				{
					CommitPoint commit = (CommitPoint) commitsToDelete[i];
					if (infoStream != null)
					{
						Message("deleteCommits: now remove commit \"" + commit.GetSegmentsFileName() + "\"");
					}
					int size2 = commit.files.Count;
					for (int j = 0; j < size2; j++)
					{
						DecRef((System.String) commit.files[j]);
					}
				}
				commitsToDelete.Clear();
				
				// Now compact commits to remove deleted ones (preserving the sort):
				size = commits.Count;
				int readFrom = 0;
				int writeTo = 0;
				while (readFrom < size)
				{
					CommitPoint commit = (CommitPoint) commits[readFrom];
					if (!commit.deleted)
					{
						if (writeTo != readFrom)
						{
							commits[writeTo] = commits[readFrom];
						}
						writeTo++;
					}
					readFrom++;
				}
				
				while (size > writeTo)
				{
					commits.RemoveAt(size - 1);
					size--;
				}
			}
		}
		
		/// <summary> Writer calls this when it has hit an error and had to
		/// roll back, to tell us that there may now be
		/// unreferenced files in the filesystem.  So we re-list
		/// the filesystem and delete such files.  If segmentName
		/// is non-null, we will only delete files corresponding to
		/// that segment.
		/// </summary>
		public void  Refresh(System.String segmentName)
		{
			System.String[] files = directory.List();
			if (files == null)
			{
				throw new System.IO.IOException("cannot read directory " + directory + ": list() returned null");
			}
			IndexFileNameFilter filter = IndexFileNameFilter.GetFilter();
			System.String segmentPrefix1;
			System.String segmentPrefix2;
			if (segmentName != null)
			{
				segmentPrefix1 = segmentName + ".";
				segmentPrefix2 = segmentName + "_";
			}
			else
			{
				segmentPrefix1 = null;
				segmentPrefix2 = null;
			}
			
			for (int i = 0; i < files.Length; i++)
			{
				System.String fileName = files[i];
				if (filter.Accept(null, fileName) && (segmentName == null || fileName.StartsWith(segmentPrefix1) || fileName.StartsWith(segmentPrefix2)) && !refCounts.Contains(fileName) && !fileName.Equals(IndexFileNames.SEGMENTS_GEN))
				{
					// Unreferenced file, so remove it
					if (infoStream != null)
					{
						Message("refresh [prefix=" + segmentName + "]: removing newly created unreferenced file \"" + fileName + "\"");
					}
					DeleteFile(fileName);
				}
			}
		}
		
		public void  Refresh()
		{
			Refresh(null);
		}
		
		public void  Close()
		{
			DeletePendingFiles();
		}
		
		private void  DeletePendingFiles()
		{
			if (deletable != null)
			{
				System.Collections.IList oldDeletable = deletable;
				deletable = null;
				int size = oldDeletable.Count;
				for (int i = 0; i < size; i++)
				{
					if (infoStream != null)
					{
						Message("delete pending file " + oldDeletable[i]);
					}
					DeleteFile((System.String) oldDeletable[i]);
				}
			}
		}
		
		/// <summary> For definition of "check point" see IndexWriter comments:
		/// "Clarification: Check Points (and commits)".
		/// 
		/// Writer calls this when it has made a "consistent
		/// change" to the index, meaning new files are written to
		/// the index and the in-memory SegmentInfos have been
		/// modified to point to those files.
		/// 
		/// This may or may not be a commit (segments_N may or may
		/// not have been written).
		/// 
		/// We simply incref the files referenced by the new
		/// SegmentInfos and decref the files we had previously
		/// seen (if any).
		/// 
		/// If this is a commit, we also call the policy to give it
		/// a chance to remove other commits.  If any commits are
		/// removed, we decref their files as well.
		/// </summary>
		public void  Checkpoint(SegmentInfos segmentInfos, bool isCommit)
		{
			
			if (infoStream != null)
			{
				Message("now checkpoint \"" + segmentInfos.GetCurrentSegmentFileName() + "\" [" + segmentInfos.Count + " segments " + "; isCommit = " + isCommit + "]");
			}
			
			// Try again now to delete any previously un-deletable
			// files (because they were in use, on Windows):
			DeletePendingFiles();
			
			// Incref the files:
			IncRef(segmentInfos, isCommit);
			System.Collections.IList docWriterFiles;
			if (docWriter != null)
			{
				docWriterFiles = docWriter.Files();
				if (docWriterFiles != null)
					IncRef(docWriterFiles);
			}
			else
				docWriterFiles = null;
			
			if (isCommit)
			{
				// Append to our commits list:
				commits.Add(new CommitPoint(this, segmentInfos));
				
				// Tell policy so it can remove commits:
				policy.OnCommit(commits);
				
				// Decref files for commits that were deleted by the policy:
				DeleteCommits();
			}
			
			// DecRef old files from the last checkpoint, if any:
			int size = lastFiles.Count;
			if (size > 0)
			{
				for (int i = 0; i < size; i++)
					DecRef((System.Collections.IList) lastFiles[i]);
				lastFiles.Clear();
			}
			
			if (!isCommit)
			{
				// Save files so we can decr on next checkpoint/commit:
				size = segmentInfos.Count;
				for (int i = 0; i < size; i++)
				{
					SegmentInfo segmentInfo = segmentInfos.Info(i);
					if (segmentInfo.dir == directory)
					{
						lastFiles.Add(segmentInfo.Files());
					}
				}
			}
			if (docWriterFiles != null)
				lastFiles.Add(docWriterFiles);
		}
		
		internal void  IncRef(SegmentInfos segmentInfos, bool isCommit)
		{
			int size = segmentInfos.Count;
			for (int i = 0; i < size; i++)
			{
				SegmentInfo segmentInfo = segmentInfos.Info(i);
				if (segmentInfo.dir == directory)
				{
					IncRef(segmentInfo.Files());
				}
			}
			
			if (isCommit)
			{
				// Since this is a commit point, also incref its
				// segments_N file:
				GetRefCount(segmentInfos.GetCurrentSegmentFileName()).IncRef();
			}
		}
		
		internal void  IncRef(System.Collections.IList files)
		{
			int size = files.Count;
			for (int i = 0; i < size; i++)
			{
				System.String fileName = (System.String) files[i];
				RefCount rc = GetRefCount(fileName);
				if (infoStream != null && VERBOSE_REF_COUNTS)
				{
					Message("  IncRef \"" + fileName + "\": pre-incr count is " + rc.count);
				}
				rc.IncRef();
			}
		}
		
		internal void  DecRef(System.Collections.IList files)
		{
			int size = files.Count;
			for (int i = 0; i < size; i++)
			{
				DecRef((System.String) files[i]);
			}
		}
		
		private void  DecRef(System.String fileName)
		{
			RefCount rc = GetRefCount(fileName);
			if (infoStream != null && VERBOSE_REF_COUNTS)
			{
				Message("  DecRef \"" + fileName + "\": pre-decr count is " + rc.count);
			}
			if (0 == rc.DecRef())
			{
				// This file is no longer referenced by any past
				// commit points nor by the in-memory SegmentInfos:
				DeleteFile(fileName);
				refCounts.Remove(fileName);
			}
		}
		
		internal void  DecRef(SegmentInfos segmentInfos)
		{
			int size = segmentInfos.Count;
			for (int i = 0; i < size; i++)
			{
				SegmentInfo segmentInfo = segmentInfos.Info(i);
				if (segmentInfo.dir == directory)
				{
					DecRef(segmentInfo.Files());
				}
			}
		}
		
		private RefCount GetRefCount(System.String fileName)
		{
			RefCount rc;
			if (!refCounts.Contains(fileName))
			{
				rc = new RefCount();
				refCounts[fileName] = rc;
			}
			else
			{
				rc = (RefCount) refCounts[fileName];
			}
			return rc;
		}
		
		internal void  DeleteFiles(System.Collections.IList files)
		{
			int size = files.Count;
			for (int i = 0; i < size; i++)
				DeleteFile((System.String) files[i]);
		}
		
		/// <summary>Delets the specified files, but only if they are new
		/// (have not yet been incref'd). 
		/// </summary>
		internal void  DeleteNewFiles(System.Collections.IList files)
		{
			int size = files.Count;
			for (int i = 0; i < size; i++)
				if (!refCounts.Contains(files[i]))
					DeleteFile((System.String) files[i]);
		}
		
		internal void  DeleteFile(System.String fileName)
		{
			try
			{
				if (infoStream != null)
				{
					Message("delete \"" + fileName + "\"");
				}
				directory.DeleteFile(fileName);
			}
			catch (System.IO.IOException e)
			{
				// if delete fails
				if (directory.FileExists(fileName))
				{
					
					// Some operating systems (e.g. Windows) don't
					// permit a file to be deleted while it is opened
					// for read (e.g. by another process or thread). So
					// we assume that when a delete fails it is because
					// the file is open in another process, and queue
					// the file for subsequent deletion.
					
					if (infoStream != null)
					{
						Message("IndexFileDeleter: unable to remove file \"" + fileName + "\": " + e.ToString() + "; Will re-try later.");
					}
					if (deletable == null)
					{
						deletable = new System.Collections.ArrayList();
					}
					deletable.Add(fileName); // add to deletable
				}
			}
		}
		
		/// <summary> Tracks the reference count for a single index file:</summary>
		sealed private class RefCount
		{
			
			internal int count;
			
			public int IncRef()
			{
				return ++count;
			}
			
			public int DecRef()
			{
				System.Diagnostics.Debug.Assert(count > 0);
				return --count;
			}
		}
		
		/// <summary> Holds details for each commit point.  This class is
		/// also passed to the deletion policy.  Note: this class
		/// has a natural ordering that is inconsistent with
		/// equals.
		/// </summary>
		
		sealed private class CommitPoint : System.IComparable, IndexCommitPoint
		{
			private void  InitBlock(IndexFileDeleter enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private IndexFileDeleter enclosingInstance;
			public IndexFileDeleter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			internal long gen;
			internal System.Collections.IList files;
			internal System.String segmentsFileName;
			internal bool deleted;
			
			public CommitPoint(IndexFileDeleter enclosingInstance, SegmentInfos segmentInfos)
			{
				InitBlock(enclosingInstance);
				segmentsFileName = segmentInfos.GetCurrentSegmentFileName();
				int size = segmentInfos.Count;
				files = new System.Collections.ArrayList(size);
				files.Add(segmentsFileName);
				gen = segmentInfos.GetGeneration();
				for (int i = 0; i < size; i++)
				{
					SegmentInfo segmentInfo = segmentInfos.Info(i);
					if (segmentInfo.dir == Enclosing_Instance.directory)
					{
                        System.Collections.IEnumerator filesEnum = segmentInfo.Files().GetEnumerator();
                        while (filesEnum.MoveNext())
                        {
                            files.Add(filesEnum.Current);
                        }
					}
				}
			}
			
			/// <summary> Get the segments_N file for this commit point.</summary>
			public System.String GetSegmentsFileName()
			{
				return segmentsFileName;
			}
			
			public System.Collections.ICollection GetFileNames()
			{
				return System.Collections.ArrayList.ReadOnly(new System.Collections.ArrayList(files));
			}
			
			/// <summary> Called only be the deletion policy, to remove this
			/// commit point from the index.
			/// </summary>
			public void  Delete()
			{
				if (!deleted)
				{
					deleted = true;
					Enclosing_Instance.commitsToDelete.Add(this);
				}
			}
			
			public int CompareTo(System.Object obj)
			{
				CommitPoint commit = (CommitPoint) obj;
				if (gen < commit.gen)
				{
					return - 1;
				}
				else if (gen > commit.gen)
				{
					return 1;
				}
				else
				{
					return 0;
				}
			}
		}
	}
}