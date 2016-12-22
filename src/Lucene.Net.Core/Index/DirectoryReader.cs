using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
    using NoSuchDirectoryException = Lucene.Net.Store.NoSuchDirectoryException;

    /// <summary>
    /// DirectoryReader is an implementation of <seealso cref="CompositeReader"/>
    /// that can read indexes in a <seealso cref="Directory"/>.
    ///
    /// <p>DirectoryReader instances are usually constructed with a call to
    /// one of the static <code>open()</code> methods, e.g. {@link
    /// #open(Directory)}.
    ///
    /// <p> For efficiency, in this API documents are often referred to via
    /// <i>document numbers</i>, non-negative integers which each name a unique
    /// document in the index.  These document numbers are ephemeral -- they may change
    /// as documents are added to and deleted from an index.  Clients should thus not
    /// rely on a given document having the same number between sessions.
    ///
    /// <p>
    /// <a name="thread-safety"></a><p><b>NOTE</b>: {@link
    /// IndexReader} instances are completely thread
    /// safe, meaning multiple threads can call any of its methods,
    /// concurrently.  If your application requires external
    /// synchronization, you should <b>not</b> synchronize on the
    /// <code>IndexReader</code> instance; use your own
    /// (non-Lucene) objects instead.
    /// </summary>
    public abstract class DirectoryReader : BaseCompositeReader<AtomicReader>
    {
        /// <summary>
        /// Default termInfosIndexDivisor. </summary>
        public static readonly int DEFAULT_TERMS_INDEX_DIVISOR = 1;

        /// <summary>
        /// The index directory. </summary>
        protected internal readonly Directory directory;

        /// <summary>
        /// Returns a IndexReader reading the index in the given
        ///  Directory </summary>
        /// <param name="directory"> the index directory </param>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        new public static DirectoryReader Open(Directory directory)
        {
            return StandardDirectoryReader.Open(directory, null, DEFAULT_TERMS_INDEX_DIVISOR);
        }

        /// <summary>
        /// Expert: Returns a IndexReader reading the index in the given
        ///  Directory with the given termInfosIndexDivisor. </summary>
        /// <param name="directory"> the index directory </param>
        /// <param name="termInfosIndexDivisor"> Subsamples which indexed
        ///  terms are loaded into RAM. this has the same effect as {@link
        ///  IndexWriterConfig#setTermIndexInterval} except that setting
        ///  must be done at indexing time while this setting can be
        ///  set per reader.  When set to N, then one in every
        ///  N*termIndexInterval terms in the index is loaded into
        ///  memory.  By setting this to a value > 1 you can reduce
        ///  memory usage, at the expense of higher latency when
        ///  loading a TermInfo.  The default value is 1.  Set this
        ///  to -1 to skip loading the terms index entirely.
        ///  <b>NOTE:</b> divisor settings &gt; 1 do not apply to all PostingsFormat
        ///  implementations, including the default one in this release. It only makes
        ///  sense for terms indexes that can efficiently re-sample terms at load time. </param>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        new public static DirectoryReader Open(Directory directory, int termInfosIndexDivisor)
        {
            return StandardDirectoryReader.Open(directory, null, termInfosIndexDivisor);
        }

        /// <summary>
        /// Open a near real time IndexReader from the <seealso cref="Lucene.Net.Index.IndexWriter"/>.
        /// </summary>
        /// <param name="writer"> The IndexWriter to open from </param>
        /// <param name="applyAllDeletes"> If true, all buffered deletes will
        /// be applied (made visible) in the returned reader.  If
        /// false, the deletes are not applied but remain buffered
        /// (in IndexWriter) so that they will be applied in the
        /// future.  Applying deletes can be costly, so if your app
        /// can tolerate deleted documents being returned you might
        /// gain some performance by passing false. </param>
        /// <returns> The new IndexReader </returns>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error
        /// </exception>
        /// <seealso cref= #openIfChanged(DirectoryReader,IndexWriter,boolean)
        ///
        /// @lucene.experimental </seealso>
        new public static DirectoryReader Open(IndexWriter writer, bool applyAllDeletes)
        {
            return writer.GetReader(applyAllDeletes);
        }

        /// <summary>
        /// Expert: returns an IndexReader reading the index in the given
        ///  <seealso cref="IndexCommit"/>. </summary>
        /// <param name="commit"> the commit point to open </param>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        new public static DirectoryReader Open(IndexCommit commit)
        {
            return StandardDirectoryReader.Open(commit.Directory, commit, DEFAULT_TERMS_INDEX_DIVISOR);
        }

        /// <summary>
        /// Expert: returns an IndexReader reading the index in the given
        ///  <seealso cref="IndexCommit"/> and termInfosIndexDivisor. </summary>
        /// <param name="commit"> the commit point to open </param>
        /// <param name="termInfosIndexDivisor"> Subsamples which indexed
        ///  terms are loaded into RAM. this has the same effect as {@link
        ///  IndexWriterConfig#setTermIndexInterval} except that setting
        ///  must be done at indexing time while this setting can be
        ///  set per reader.  When set to N, then one in every
        ///  N*termIndexInterval terms in the index is loaded into
        ///  memory.  By setting this to a value > 1 you can reduce
        ///  memory usage, at the expense of higher latency when
        ///  loading a TermInfo.  The default value is 1.  Set this
        ///  to -1 to skip loading the terms index entirely.
        ///  <b>NOTE:</b> divisor settings &gt; 1 do not apply to all PostingsFormat
        ///  implementations, including the default one in this release. It only makes
        ///  sense for terms indexes that can efficiently re-sample terms at load time. </param>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        new public static DirectoryReader Open(IndexCommit commit, int termInfosIndexDivisor)
        {
            return StandardDirectoryReader.Open(commit.Directory, commit, termInfosIndexDivisor);
        }

        /// <summary>
        /// If the index has changed since the provided reader was
        /// opened, open and return a new reader; else, return
        /// null.  The new reader, if not null, will be the same
        /// type of reader as the previous one, ie an NRT reader
        /// will open a new NRT reader, a MultiReader will open a
        /// new MultiReader,  etc.
        ///
        /// <p>this method is typically far less costly than opening a
        /// fully new <code>DirectoryReader</code> as it shares
        /// resources (for example sub-readers) with the provided
        /// <code>DirectoryReader</code>, when possible.
        ///
        /// <p>The provided reader is not closed (you are responsible
        /// for doing so); if a new reader is returned you also
        /// must eventually close it.  Be sure to never close a
        /// reader while other threads are still using it; see
        /// <seealso cref="SearcherManager"/> to simplify managing this.
        /// </summary>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        /// <returns> null if there are no changes; else, a new
        /// DirectoryReader instance which you must eventually close </returns>
        public static DirectoryReader OpenIfChanged(DirectoryReader oldReader)
        {
            DirectoryReader newReader = oldReader.DoOpenIfChanged();
            Debug.Assert(newReader != oldReader);
            return newReader;
        }

        /// <summary>
        /// If the IndexCommit differs from what the
        /// provided reader is searching, open and return a new
        /// reader; else, return null.
        /// </summary>
        /// <seealso cref= #openIfChanged(DirectoryReader) </seealso>
        public static DirectoryReader OpenIfChanged(DirectoryReader oldReader, IndexCommit commit)
        {
            DirectoryReader newReader = oldReader.DoOpenIfChanged(commit);
            Debug.Assert(newReader != oldReader);
            return newReader;
        }

        /// <summary>
        /// Expert: If there changes (committed or not) in the
        /// <seealso cref="IndexWriter"/> versus what the provided reader is
        /// searching, then open and return a new
        /// IndexReader searching both committed and uncommitted
        /// changes from the writer; else, return null (though, the
        /// current implementation never returns null).
        ///
        /// <p>this provides "near real-time" searching, in that
        /// changes made during an <seealso cref="IndexWriter"/> session can be
        /// quickly made available for searching without closing
        /// the writer nor calling <seealso cref="IndexWriter#commit"/>.
        ///
        /// <p>It's <i>near</i> real-time because there is no hard
        /// guarantee on how quickly you can get a new reader after
        /// making changes with IndexWriter.  You'll have to
        /// experiment in your situation to determine if it's
        /// fast enough.  As this is a new and experimental
        /// feature, please report back on your findings so we can
        /// learn, improve and iterate.</p>
        ///
        /// <p>The very first time this method is called, this
        /// writer instance will make every effort to pool the
        /// readers that it opens for doing merges, applying
        /// deletes, etc.  this means additional resources (RAM,
        /// file descriptors, CPU time) will be consumed.</p>
        ///
        /// <p>For lower latency on reopening a reader, you should
        /// call <seealso cref="IndexWriterConfig#setMergedSegmentWarmer"/> to
        /// pre-warm a newly merged segment before it's committed
        /// to the index.  this is important for minimizing
        /// index-to-search delay after a large merge.  </p>
        ///
        /// <p>If an addIndexes* call is running in another thread,
        /// then this reader will only search those segments from
        /// the foreign index that have been successfully copied
        /// over, so far.</p>
        ///
        /// <p><b>NOTE</b>: Once the writer is closed, any
        /// outstanding readers may continue to be used.  However,
        /// if you attempt to reopen any of those readers, you'll
        /// hit an <seealso cref="Lucene.Net.Store.AlreadyClosedException"/>.</p>
        /// </summary>
        /// <returns> DirectoryReader that covers entire index plus all
        /// changes made so far by this IndexWriter instance, or
        /// null if there are no new changes
        /// </returns>
        /// <param name="writer"> The IndexWriter to open from
        /// </param>
        /// <param name="applyAllDeletes"> If true, all buffered deletes will
        /// be applied (made visible) in the returned reader.  If
        /// false, the deletes are not applied but remain buffered
        /// (in IndexWriter) so that they will be applied in the
        /// future.  Applying deletes can be costly, so if your app
        /// can tolerate deleted documents being returned you might
        /// gain some performance by passing false.
        /// </param>
        /// <exception cref="IOException"> if there is a low-level IO error
        ///
        /// @lucene.experimental </exception>
        public static DirectoryReader OpenIfChanged(DirectoryReader oldReader, IndexWriter writer, bool applyAllDeletes)
        {
            DirectoryReader newReader = oldReader.DoOpenIfChanged(writer, applyAllDeletes);
            Debug.Assert(newReader != oldReader);
            return newReader;
        }

        /// <summary>
        /// Returns all commit points that exist in the Directory.
        ///  Normally, because the default is {@link
        ///  KeepOnlyLastCommitDeletionPolicy}, there would be only
        ///  one commit point.  But if you're using a custom {@link
        ///  IndexDeletionPolicy} then there could be many commits.
        ///  Once you have a given commit, you can open a reader on
        ///  it by calling <seealso cref="DirectoryReader#open(IndexCommit)"/>
        ///  There must be at least one commit in
        ///  the Directory, else this method throws {@link
        ///  IndexNotFoundException}.  Note that if a commit is in
        ///  progress while this method is running, that commit
        ///  may or may not be returned.
        /// </summary>
        ///  <returns> a sorted list of <seealso cref="IndexCommit"/>s, from oldest
        ///  to latest.  </returns>
        public static IList<IndexCommit> ListCommits(Directory dir)
        {
            string[] files = dir.ListAll();

            List<IndexCommit> commits = new List<IndexCommit>();

            SegmentInfos latest = new SegmentInfos();
            latest.Read(dir);
            long currentGen = latest.Generation;

            commits.Add(new StandardDirectoryReader.ReaderCommit(latest, dir));

            for (int i = 0; i < files.Length; i++)
            {
                string fileName = files[i];

                if (fileName.StartsWith(IndexFileNames.SEGMENTS) && !fileName.Equals(IndexFileNames.SEGMENTS_GEN) && SegmentInfos.GenerationFromSegmentsFileName(fileName) < currentGen)
                {
                    SegmentInfos sis = new SegmentInfos();
                    try
                    {
                        // IOException allowed to throw there, in case
                        // segments_N is corrupt
                        sis.Read(dir, fileName);
                    }
                    catch (FileNotFoundException/* | NoSuchFileException fnfe*/)
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
        /// Returns <code>true</code> if an index likely exists at
        /// the specified directory.  Note that if a corrupt index
        /// exists, or if an index in the process of committing </summary>
        /// <param name="directory"> the directory to check for an index </param>
        /// <returns> <code>true</code> if an index exists; <code>false</code> otherwise </returns>
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
            catch (NoSuchDirectoryException nsde)
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
                    if (file.StartsWith(prefix) || file.Equals(IndexFileNames.SEGMENTS_GEN))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Expert: Constructs a {@code DirectoryReader} on the given subReaders. </summary>
        /// <param name="segmentReaders"> the wrapped atomic index segment readers. this array is
        /// returned by <seealso cref="#getSequentialSubReaders"/> and used to resolve the correct
        /// subreader for docID-based methods. <b>Please note:</b> this array is <b>not</b>
        /// cloned and not protected for modification outside of this reader.
        /// Subclasses of {@code DirectoryReader} should take care to not allow
        /// modification of this internal array, e.g. <seealso cref="#doOpenIfChanged()"/>. </param>
        protected DirectoryReader(Directory directory, AtomicReader[] segmentReaders)
            : base(segmentReaders)
        {
            this.directory = directory;
        }

        /// <summary>
        /// Returns the directory this index resides in. </summary>
        public Directory Directory
        {
            get
            {
                // Don't ensureOpen here -- in certain cases, when a
                // cloned/reopened reader needs to commit, it may call
                // this method on the closed original reader
                return directory;
            }
        }

        /// <summary>
        /// Implement this method to support <seealso cref="#openIfChanged(DirectoryReader)"/>.
        /// If this reader does not support reopen, return {@code null}, so
        /// client code is happy. this should be consistent with <seealso cref="#isCurrent"/>
        /// (should always return {@code true}) if reopen is not supported. </summary>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        /// <returns> null if there are no changes; else, a new
        /// DirectoryReader instance. </returns>
        protected internal abstract DirectoryReader DoOpenIfChanged();

        /// <summary>
        /// Implement this method to support <seealso cref="#openIfChanged(DirectoryReader,IndexCommit)"/>.
        /// If this reader does not support reopen from a specific <seealso cref="IndexCommit"/>,
        /// throw <seealso cref="UnsupportedOperationException"/>. </summary>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        /// <returns> null if there are no changes; else, a new
        /// DirectoryReader instance. </returns>
        protected internal abstract DirectoryReader DoOpenIfChanged(IndexCommit commit);

        /// <summary>
        /// Implement this method to support <seealso cref="#openIfChanged(DirectoryReader,IndexWriter,boolean)"/>.
        /// If this reader does not support reopen from <seealso cref="IndexWriter"/>,
        /// throw <seealso cref="UnsupportedOperationException"/>. </summary>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        /// <returns> null if there are no changes; else, a new
        /// DirectoryReader instance. </returns>
        protected internal abstract DirectoryReader DoOpenIfChanged(IndexWriter writer, bool applyAllDeletes);

        /// <summary>
        /// Version number when this IndexReader was opened.
        ///
        /// <p>this method
        /// returns the version recorded in the commit that the
        /// reader opened.  this version is advanced every time
        /// a change is made with <seealso cref="IndexWriter"/>.</p>
        /// </summary>
        public abstract long Version { get; }

        /// <summary>
        /// Check whether any new changes have occurred to the
        /// index since this reader was opened.
        ///
        /// <p>If this reader was created by calling <seealso cref="#open"/>,
        /// then this method checks if any further commits
        /// (see <seealso cref="IndexWriter#commit"/>) have occurred in the
        /// directory.</p>
        ///
        /// <p>If instead this reader is a near real-time reader
        /// (ie, obtained by a call to {@link
        /// DirectoryReader#open(IndexWriter,boolean)}, or by calling <seealso cref="#openIfChanged"/>
        /// on a near real-time reader), then this method checks if
        /// either a new commit has occurred, or any new
        /// uncommitted changes have taken place via the writer.
        /// Note that even if the writer has only performed
        /// merging, this method will still return false.</p>
        ///
        /// <p>In any event, if this returns false, you should call
        /// <seealso cref="#openIfChanged"/> to get a new reader that sees the
        /// changes.</p>
        /// </summary>
        /// <exception cref="IOException">           if there is a low-level IO error </exception>
        public abstract bool IsCurrent { get; }

        /// <summary>
        /// Expert: return the IndexCommit that this reader has opened.
        /// <p/>
        /// @lucene.experimental
        /// </summary>
        public abstract IndexCommit IndexCommit { get; }
    }
}