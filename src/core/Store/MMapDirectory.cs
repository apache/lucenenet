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

using Constants = Lucene.Net.Util.Constants;

namespace Lucene.Net.Store
{
	
	/// <summary>File-based {@link Directory} implementation that uses
	/// mmap for reading, and {@link
	/// SimpleFSDirectory.SimpleFSIndexOutput} for writing.
	/// 
	/// <p/><b>NOTE</b>: memory mapping uses up a portion of the
	/// virtual memory address space in your process equal to the
	/// size of the file being mapped.  Before using this class,
	/// be sure your have plenty of virtual address space, e.g. by
	/// using a 64 bit JRE, or a 32 bit JRE with indexes that are
	/// guaranteed to fit within the address space.
	/// On 32 bit platforms also consult {@link #setMaxChunkSize}
	/// if you have problems with mmap failing because of fragmented
	/// address space. If you get an OutOfMemoryException, it is recommened
	/// to reduce the chunk size, until it works.
	/// 
	/// <p/>Due to <a href="http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=4724038">
	/// this bug</a> in Sun's JRE, MMapDirectory's {@link IndexInput#close}
	/// is unable to close the underlying OS file handle.  Only when GC
	/// finally collects the underlying objects, which could be quite
	/// some time later, will the file handle be closed.
	/// 
	/// <p/>This will consume additional transient disk usage: on Windows,
	/// attempts to delete or overwrite the files will result in an
	/// exception; on other platforms, which typically have a &quot;delete on
	/// last close&quot; semantics, while such operations will succeed, the bytes
	/// are still consuming space on disk.  For many applications this
	/// limitation is not a problem (e.g. if you have plenty of disk space,
	/// and you don't rely on overwriting files on Windows) but it's still
	/// an important limitation to be aware of.
	/// 
	/// <p/>This class supplies the workaround mentioned in the bug report
	/// (disabled by default, see {@link #setUseUnmap}), which may fail on
	/// non-Sun JVMs. It forcefully unmaps the buffer on close by using
	/// an undocumented internal cleanup functionality.
	/// {@link #UNMAP_SUPPORTED} is <code>true</code>, if the workaround
	/// can be enabled (with no guarantees).
	/// </summary>
	public class MMapDirectory:Lucene.Net.Support.MemoryMappedDirectory
	{
		/// <summary>Create a new MMapDirectory for the named location.
		/// 
		/// </summary>
		/// <param name="path">the path of the directory
		/// </param>
		/// <param name="lockFactory">the lock factory to use, or null for the default.
		/// </param>
		/// <throws>  IOException </throws>
		[System.Obsolete("Use the constructor that takes a DirectoryInfo, this will be removed in the 3.0 release")]
		public MMapDirectory(System.IO.FileInfo path, LockFactory lockFactory):base(new System.IO.DirectoryInfo(path.FullName), lockFactory)
		{
            throw new System.NotImplementedException("Use FSDirectory (https://issues.apache.org/jira/browse/LUCENENET-425)");
		}
		
        /// <summary>Create a new MMapDirectory for the named location.
        /// 
        /// </summary>
        /// <param name="path">the path of the directory
        /// </param>
        /// <param name="lockFactory">the lock factory to use, or null for the default.
        /// </param>
        /// <throws>  IOException </throws>
        public MMapDirectory(System.IO.DirectoryInfo path, LockFactory lockFactory) : base(path, lockFactory)
        {
            throw new System.NotImplementedException("Use FSDirectory (https://issues.apache.org/jira/browse/LUCENENET-425)");
        }
		
		/// <summary>Create a new MMapDirectory for the named location and the default lock factory.
		/// 
		/// </summary>
		/// <param name="path">the path of the directory
		/// </param>
		/// <throws>  IOException </throws>
		[System.Obsolete("Use the constructor that takes a DirectoryInfo, this will be removed in the 3.0 release")]
		public MMapDirectory(System.IO.FileInfo path):base(new System.IO.DirectoryInfo(path.FullName), null)
		{
            throw new System.NotImplementedException("Use FSDirectory (https://issues.apache.org/jira/browse/LUCENENET-425)");
		}
		
        /// <summary>Create a new MMapDirectory for the named location and the default lock factory.
        /// 
        /// </summary>
        /// <param name="path">the path of the directory
        /// </param>
        /// <throws>  IOException </throws>
        public MMapDirectory(System.IO.DirectoryInfo path) : base(path, null)
        {
            throw new System.NotImplementedException("Use FSDirectory (https://issues.apache.org/jira/browse/LUCENENET-425)");
        }
	}
}