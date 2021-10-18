using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;

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

    // javadocs
    using Directory = Lucene.Net.Store.Directory;

    /// <summary>
    /// <see cref="DirectoryReader"/> is an implementation of <see cref="CompositeReader"/>
    /// that can read indexes in a <see cref="Store.Directory"/>.
    ///
    /// <para/><see cref="DirectoryReader"/> instances are usually constructed with a call to
    /// one of the static <c>Open()</c> methods, e.g. <see cref="Open(Directory)"/>.
    ///
    /// <para/> For efficiency, in this API documents are often referred to via
    /// <i>document numbers</i>, non-negative integers which each name a unique
    /// document in the index.  These document numbers are ephemeral -- they may change
    /// as documents are added to and deleted from an index.  Clients should thus not
    /// rely on a given document having the same number between sessions.
    ///
    /// <para/><b>NOTE</b>:
    /// <see cref="IndexReader"/> instances are completely thread
    /// safe, meaning multiple threads can call any of its methods,
    /// concurrently.  If your application requires external
    /// synchronization, you should <b>not</b> synchronize on the
    /// <see cref="IndexReader"/> instance; use your own
    /// (non-Lucene) objects instead.
    /// </summary>
    public abstract class DirectoryReader : BaseCompositeReader<AtomicReader>
    {
        /// <summary>
        /// Default termInfosIndexDivisor. </summary>
        public static readonly int DEFAULT_TERMS_INDEX_DIVISOR = 1;

        /// <summary>
        /// The index directory. </summary>
        protected readonly Directory m_directory;

        /// <summary>
        /// Returns a <see cref="IndexReader"/> reading the index in the given
        /// <see cref="Store.Directory"/> </summary>
        /// <param name="directory"> the index directory </param>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        new public static DirectoryReader Open(Directory directory)
        {
            return StandardDirectoryReader.Open(directory, null, DEFAULT_TERMS_INDEX_DIVISOR);
        }

        /// <summary>
        /// Expert: Returns a <see cref="IndexReader"/> reading the index in the given
        /// <see cref="Store.Directory"/> with the given termInfosIndexDivisor. </summary>
        /// <param name="directory"> the index directory </param>
        /// <param name="termInfosIndexDivisor"> Subsamples which indexed
        /// terms are loaded into RAM. this has the same effect as setting
        /// <see cref="LiveIndexWriterConfig.TermIndexInterval"/> (on <see cref="IndexWriterConfig"/>) except that setting
        /// must be done at indexing time while this setting can be
        /// set per reader.  When set to N, then one in every
        /// N*termIndexInterval terms in the index is loaded into
        /// memory.  By setting this to a value &gt; 1 you can reduce
        /// memory usage, at the expense of higher latency when
        /// loading a TermInfo.  The default value is 1.  Set this
        /// to -1 to skip loading the terms index entirely.
        /// <b>NOTE:</b> divisor settings &gt; 1 do not apply to all <see cref="Codecs.PostingsFormat"/>
        /// implementations, including the default one in this release. It only makes
        /// sense for terms indexes that can efficiently re-sample terms at load time. </param>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        new public static DirectoryReader Open(Directory directory, int termInfosIndexDivisor)
        {
            return StandardDirectoryReader.Open(directory, null, termInfosIndexDivisor);
        }

        /// <summary>
        /// Open a near real time <see cref="IndexReader"/> from the <see cref="IndexWriter"/>.
        /// <para/>
        /// @lucene.experimental 
        /// </summary>
        /// <param name="writer"> The <see cref="IndexWriter"/> to open from </param>
        /// <param name="applyAllDeletes"> If <c>true</c>, all buffered deletes will
        /// be applied (made visible) in the returned reader.  If
        /// <c>false</c>, the deletes are not applied but remain buffered
        /// (in IndexWriter) so that they will be applied in the
        /// future.  Applying deletes can be costly, so if your app
        /// can tolerate deleted documents being returned you might
        /// gain some performance by passing <c>false</c>. </param>
        /// <returns> The new <see cref="IndexReader"/> </returns>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error
        /// </exception>
        /// <seealso cref="OpenIfChanged(DirectoryReader, IndexWriter, bool)"/>
        new public static DirectoryReader Open(IndexWriter writer, bool applyAllDeletes)
        {
            return writer.GetReader(applyAllDeletes);
        }

        /// <summary>
        /// Expert: returns an <see cref="IndexReader"/> reading the index in the given
        /// <see cref="Index.IndexCommit"/>. </summary>
        /// <param name="commit"> the commit point to open </param>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        new public static DirectoryReader Open(IndexCommit commit)
        {
            return StandardDirectoryReader.Open(commit.Directory, commit, DEFAULT_TERMS_INDEX_DIVISOR);
        }

        /// <summary>
        /// Expert: returns an <see cref="IndexReader"/> reading the index in the given
        /// <seealso cref="Index.IndexCommit"/> and <paramref name="termInfosIndexDivisor"/>. </summary>
        /// <param name="commit"> the commit point to open </param>
        /// <param name="termInfosIndexDivisor"> Subsamples which indexed
        /// terms are loaded into RAM. this has the same effect as setting
        /// <see cref="LiveIndexWriterConfig.TermIndexInterval"/> (on <see cref="IndexWriterConfig"/>) except that setting
        /// must be done at indexing time while this setting can be
        /// set per reader.  When set to N, then one in every
        /// N*termIndexInterval terms in the index is loaded into
        /// memory.  By setting this to a value &gt; 1 you can reduce
        /// memory usage, at the expense of higher latency when
        /// loading a TermInfo.  The default value is 1.  Set this
        /// to -1 to skip loading the terms index entirely.
        /// <b>NOTE:</b> divisor settings &gt; 1 do not apply to all <see cref="Codecs.PostingsFormat"/>
        /// implementations, including the default one in this release. It only makes
        /// sense for terms indexes that can efficiently re-sample terms at load time. </param>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        new public static DirectoryReader Open(IndexCommit commit, int termInfosIndexDivisor)
        {
            return StandardDirectoryReader.Open(commit.Directory, commit, termInfosIndexDivisor);
        }

        /// <summary>
        /// If the index has changed since the provided reader was
        /// opened, open and return a new reader; else, return
        /// <c>null</c>.  The new reader, if not <c>null</c>, will be the same
        /// type of reader as the previous one, ie a near-real-time (NRT) reader
        /// will open a new NRT reader, a <see cref="MultiReader"/> will open a
        /// new <see cref="MultiReader"/>,  etc.
        ///
        /// <para/>This method is typically far less costly than opening a
        /// fully new <see cref="DirectoryReader"/> as it shares
        /// resources (for example sub-readers) with the provided
        /// <see cref="DirectoryReader"/>, when possible.
        ///
        /// <para/>The provided reader is not disposed (you are responsible
        /// for doing so); if a new reader is returned you also
        /// must eventually dispose it.  Be sure to never dispose a
        /// reader while other threads are still using it; see
        /// <see cref="Search.SearcherManager"/> to simplify managing this.
        /// </summary>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        /// <returns> <c>null</c> if there are no changes; else, a new
        /// <see cref="DirectoryReader"/> instance which you must eventually dispose </returns>
        public static DirectoryReader OpenIfChanged(DirectoryReader oldReader)
        {
            DirectoryReader newReader = oldReader.DoOpenIfChanged();
            if (Debugging.AssertsEnabled) Debugging.Assert(newReader != oldReader);
            return newReader;
        }

        /// <summary>
        /// If the <see cref="Index.IndexCommit"/> differs from what the
        /// provided reader is searching, open and return a new
        /// reader; else, return <c>null</c>.
        /// </summary>
        /// <seealso cref="OpenIfChanged(DirectoryReader)"/>
        public static DirectoryReader OpenIfChanged(DirectoryReader oldReader, IndexCommit commit)
        {
            DirectoryReader newReader = oldReader.DoOpenIfChanged(commit);
            if (Debugging.AssertsEnabled) Debugging.Assert(newReader != oldReader);
            return newReader;
        }

        /// <summary>
        /// Expert: If there changes (committed or not) in the
        /// <see cref="IndexWriter"/> versus what the provided reader is
        /// searching, then open and return a new
        /// <see cref="IndexReader"/> searching both committed and uncommitted
        /// changes from the writer; else, return <c>null</c> (though, the
        /// current implementation never returns <c>null</c>).
        ///
        /// <para/>This provides "near real-time" searching, in that
        /// changes made during an <see cref="IndexWriter"/> session can be
        /// quickly made available for searching without closing
        /// the writer nor calling <see cref="IndexWriter.Commit()"/>.
        ///
        /// <para>It's <i>near</i> real-time because there is no hard
        /// guarantee on how quickly you can get a new reader after
        /// making changes with <see cref="IndexWriter"/>.  You'll have to
        /// experiment in your situation to determine if it's
        /// fast enough.  As this is a new and experimental
        /// feature, please report back on your findings so we can
        /// learn, improve and iterate.</para>
        ///
        /// <para>The very first time this method is called, this
        /// writer instance will make every effort to pool the
        /// readers that it opens for doing merges, applying
        /// deletes, etc.  This means additional resources (RAM,
        /// file descriptors, CPU time) will be consumed.</para>
        ///
        /// <para>For lower latency on reopening a reader, you should
        /// call <see cref="LiveIndexWriterConfig.MergedSegmentWarmer"/> (on <see cref="IndexWriterConfig"/>) to
        /// pre-warm a newly merged segment before it's committed
        /// to the index.  This is important for minimizing
        /// index-to-search delay after a large merge.  </para>
        ///
        /// <para>If an AddIndexes* call is running in another thread,
        /// then this reader will only search those segments from
        /// the foreign index that have been successfully copied
        /// over, so far.</para>
        ///
        /// <para><b>NOTE</b>: Once the writer is disposed, any
        /// outstanding readers may continue to be used.  However,
        /// if you attempt to reopen any of those readers, you'll
        /// hit an <see cref="ObjectDisposedException"/>.</para>
        /// 
        /// @lucene.experimental
        /// </summary>
        /// <returns> <see cref="DirectoryReader"/> that covers entire index plus all
        /// changes made so far by this <see cref="IndexWriter"/> instance, or
        /// <c>null</c> if there are no new changes
        /// </returns>
        /// <param name="writer"> The <see cref="IndexWriter"/> to open from
        /// </param>
        /// <param name="applyAllDeletes"> If <c>true</c>, all buffered deletes will
        /// be applied (made visible) in the returned reader.  If
        /// <c>false</c>, the deletes are not applied but remain buffered
        /// (in <see cref="IndexWriter"/>) so that they will be applied in the
        /// future.  Applying deletes can be costly, so if your app
        /// can tolerate deleted documents being returned you might
        /// gain some performance by passing <c>false</c>.
        /// </param>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public static DirectoryReader OpenIfChanged(DirectoryReader oldReader, IndexWriter writer, bool applyAllDeletes)
        {
            DirectoryReader newReader = oldReader.DoOpenIfChanged(writer, applyAllDeletes);
            if (Debugging.AssertsEnabled) Debugging.Assert(newReader != oldReader);
            return newReader;
        }

        /// <summary>
        /// Returns all commit points that exist in the <see cref="Store.Directory"/>.
        /// Normally, because the default is 
        /// <see cref="KeepOnlyLastCommitDeletionPolicy"/>, there would be only
        /// one commit point.  But if you're using a custom
        /// <see cref="IndexDeletionPolicy"/> then there could be many commits.
        /// Once you have a given commit, you can open a reader on
        /// it by calling <see cref="DirectoryReader.Open(IndexCommit)"/>
        /// There must be at least one commit in
        /// the <see cref="Store.Directory"/>, else this method throws 
        /// <see cref="IndexNotFoundException"/>.  Note that if a commit is in
        /// progress while this method is running, that commit
        /// may or may not be returned.
        /// </summary>
        /// <returns> a sorted list of <see cref="Index.IndexCommit"/>s, from oldest
        /// to latest. </returns>
        public static IList<IndexCommit> ListCommits(Directory dir)
        {
            string[] files = dir.ListAll();

            JCG.List<IndexCommit> commits = new JCG.List<IndexCommit>();

            SegmentInfos latest = new SegmentInfos();
            latest.Read(dir);
            long currentGen = latest.Generation;

            commits.Add(new StandardDirectoryReader.ReaderCommit(latest, dir));

            for (int i = 0; i < files.Length; i++)
            {
                string fileName = files[i];

                if (fileName.StartsWith(IndexFileNames.SEGMENTS, StringComparison.Ordinal) && !fileName.Equals(IndexFileNames.SEGMENTS_GEN, StringComparison.Ordinal) && SegmentInfos.GenerationFromSegmentsFileName(fileName) < currentGen)
                {
                    SegmentInfos sis = new SegmentInfos();
                    try
                    {
                        // IOException allowed to throw there, in case
                        // segments_N is corrupt
                        sis.Read(dir, fileName);
                    }
                    catch (Exception fnfe) when (fnfe.IsNoSuchFileExceptionOrFileNotFoundException())
                    {
                        // LUCENE-948: on NFS (and maybe others), if
                        // you have writers switching back and forth
                        // between machines, it's very likely that the
                        // dir listing will be stale and will claim a
                        // file segments_X exists when in fact it
                        // doesn't.  So, we catch this and handle it
                        // as if the file does not exist
                        sis = null;
                    }

                    if (sis != null)
                    {
                        commits.Add(new StandardDirectoryReader.ReaderCommit(sis, dir));
                    }
                }
            }

            // Ensure that the commit points are sorted in ascending order.
            commits.Sort();

            return commits;
        }

        /// <summary>
        /// Returns <c>true</c> if an index likely exists at
        /// the specified directory.  Note that if a corrupt index
        /// exists, or if an index in the process of committing </summary>
        /// <param name="directory"> the directory to check for an index </param>
        /// <returns> <c>true</c> if an index exists; <c>false</c> otherwise </returns>
        public static bool IndexExists(Directory directory)
        {
            // LUCENE-2812, LUCENE-2727, LUCENE-4738: this logic will
            // return true in cases that should arguably be false,
            // such as only IW.prepareCommit has been called, or a
            // corrupt first commit, but it's too deadly to make
            // this logic "smarter" and risk accidentally returning
            // false due to various cases like file description
            // exhaustion, access denied, etc., because in that
            // case IndexWriter may delete the entire index.  It's
            // safer to err towards "index exists" than try to be
            // smart about detecting not-yet-fully-committed or
            // corrupt indices.  this means that IndexWriter will
            // throw an exception on such indices and the app must
            // resolve the situation manually:
            string[] files;
            try
            {
                files = directory.ListAll();
            }
            catch (Exception nsde) when (nsde.IsNoSuchDirectoryException())
            {
                // Directory does not exist --> no index exists
                return false;
            }

            // Defensive: maybe a Directory impl returns null
            // instead of throwing NoSuchDirectoryException:
            if (files != null)
            {
                string prefix = IndexFileNames.SEGMENTS + "_";
                foreach (string file in files)
                {
                    if (file.StartsWith(prefix, StringComparison.Ordinal) || file.Equals(IndexFileNames.SEGMENTS_GEN, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Expert: Constructs a <see cref="DirectoryReader"/> on the given <paramref name="segmentReaders"/>. </summary>
        /// <param name="segmentReaders"> the wrapped atomic index segment readers. This array is
        /// returned by <see cref="CompositeReader.GetSequentialSubReaders"/> and used to resolve the correct
        /// subreader for docID-based methods. <b>Please note:</b> this array is <b>not</b>
        /// cloned and not protected for modification outside of this reader.
        /// Subclasses of <see cref="DirectoryReader"/> should take care to not allow
        /// modification of this internal array, e.g. <see cref="DoOpenIfChanged()"/>. </param>
        protected DirectoryReader(Directory directory, AtomicReader[] segmentReaders)
            : base(segmentReaders)
        {
            this.m_directory = directory;
        }

        /// <summary>
        /// Returns the directory this index resides in. </summary>
        public Directory Directory =>
            // Don't ensureOpen here -- in certain cases, when a
            // cloned/reopened reader needs to commit, it may call
            // this method on the closed original reader
            m_directory;

        /// <summary>
        /// Implement this method to support <see cref="OpenIfChanged(DirectoryReader)"/>.
        /// If this reader does not support reopen, return <c>null</c>, so
        /// client code is happy. This should be consistent with <see cref="IsCurrent()"/>
        /// (should always return <c>true</c>) if reopen is not supported. </summary>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        /// <returns> <c>null</c> if there are no changes; else, a new
        /// <see cref="DirectoryReader"/> instance. </returns>
        protected internal abstract DirectoryReader DoOpenIfChanged();

        /// <summary>
        /// Implement this method to support <see cref="OpenIfChanged(DirectoryReader, IndexCommit)"/>.
        /// If this reader does not support reopen from a specific <see cref="Index.IndexCommit"/>,
        /// throw <see cref="NotSupportedException"/>. </summary>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        /// <returns> <c>null</c> if there are no changes; else, a new
        /// <see cref="DirectoryReader"/> instance. </returns>
        protected internal abstract DirectoryReader DoOpenIfChanged(IndexCommit commit);

        /// <summary>
        /// Implement this method to support <see cref="OpenIfChanged(DirectoryReader, IndexWriter, bool)"/>.
        /// If this reader does not support reopen from <see cref="IndexWriter"/>,
        /// throw <see cref="NotSupportedException"/>. </summary>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        /// <returns> <c>null</c> if there are no changes; else, a new
        /// <see cref="DirectoryReader"/> instance. </returns>
        protected internal abstract DirectoryReader DoOpenIfChanged(IndexWriter writer, bool applyAllDeletes);

        /// <summary>
        /// Version number when this <see cref="IndexReader"/> was opened.
        ///
        /// <para>This method
        /// returns the version recorded in the commit that the
        /// reader opened.  This version is advanced every time
        /// a change is made with <see cref="IndexWriter"/>.</para>
        /// </summary>
        public abstract long Version { get; }

        /// <summary>
        /// Check whether any new changes have occurred to the
        /// index since this reader was opened.
        ///
        /// <para>If this reader was created by calling an overload of <see cref="Open(Directory)"/>,
        /// then this method checks if any further commits
        /// (see <see cref="IndexWriter.Commit()"/>) have occurred in the
        /// directory.</para>
        ///
        /// <para>If instead this reader is a near real-time reader
        /// (ie, obtained by a call to 
        /// <see cref="DirectoryReader.Open(IndexWriter, bool)"/>, or by calling an overload of <see cref="OpenIfChanged(DirectoryReader)"/>
        /// on a near real-time reader), then this method checks if
        /// either a new commit has occurred, or any new
        /// uncommitted changes have taken place via the writer.
        /// Note that even if the writer has only performed
        /// merging, this method will still return <c>false</c>.</para>
        ///
        /// <para>In any event, if this returns <c>false</c>, you should call
        /// an overload of <see cref="OpenIfChanged(DirectoryReader)"/> to get a new reader that sees the
        /// changes.</para>
        /// </summary>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public abstract bool IsCurrent();

        /// <summary>
        /// Expert: return the <see cref="Index.IndexCommit"/> that this reader has opened.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public abstract IndexCommit IndexCommit { get; }
    }
}