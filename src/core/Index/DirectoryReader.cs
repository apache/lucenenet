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

using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using Directory = Lucene.Net.Store.Directory;
using Document = Lucene.Net.Documents.Document;
using Lock = Lucene.Net.Store.Lock;
using LockObtainFailedException = Lucene.Net.Store.LockObtainFailedException;

namespace Lucene.Net.Index
{

    /// <summary> An IndexReader which reads indexes with multiple segments.</summary>
    public abstract class DirectoryReader : BaseCompositeReader<AtomicReader>
    {
        public const int DEFAULT_TERMS_INDEX_DIVISOR = 1;

        protected readonly Directory directory;

        public static DirectoryReader Open(Directory directory)
        {
            return StandardDirectoryReader.Open(directory, null, DEFAULT_TERMS_INDEX_DIVISOR);
        }

        public static DirectoryReader Open(Directory directory, int termInfosIndexDivisor)
        {
            return StandardDirectoryReader.Open(directory, null, termInfosIndexDivisor);
        }

        public static DirectoryReader Open(IndexWriter writer, bool applyAllDeletes)
        {
            return writer.GetReader(applyAllDeletes);
        }

        public static DirectoryReader Open(IndexCommit commit)
        {
            return StandardDirectoryReader.Open(commit.Directory, commit, DEFAULT_TERMS_INDEX_DIVISOR);
        }

        public static DirectoryReader Open(IndexCommit commit, int termInfosIndexDivisor)
        {
            return StandardDirectoryReader.Open(commit.Directory, commit, termInfosIndexDivisor);
        }

        public static DirectoryReader OpenIfChanged(DirectoryReader oldReader)
        {
            DirectoryReader newReader = oldReader.DoOpenIfChanged();
            //assert newReader != oldReader;
            return newReader;
        }

        public static DirectoryReader OpenIfChanged(DirectoryReader oldReader, IndexCommit commit)
        {
            DirectoryReader newReader = oldReader.DoOpenIfChanged(commit);
            //assert newReader != oldReader;
            return newReader;
        }

        public static DirectoryReader OpenIfChanged(DirectoryReader oldReader, IndexWriter writer, bool applyAllDeletes)
        {
            DirectoryReader newReader = oldReader.DoOpenIfChanged(writer, applyAllDeletes);
            //assert newReader != oldReader;
            return newReader;
        }

        public static IList<IndexCommit> ListCommits(Directory dir)
        {
            String[] files = dir.ListAll();

            // .NET Port: using declared variable type of List<T> instead of IList<T> for .Sort() support later
            List<IndexCommit> commits = new List<IndexCommit>();

            SegmentInfos latest = new SegmentInfos();
            latest.Read(dir);
            long currentGen = latest.Generation;

            commits.Add(new StandardDirectoryReader.ReaderCommit(latest, dir));

            for (int i = 0; i < files.Length; i++)
            {

                String fileName = files[i];

                if (fileName.StartsWith(IndexFileNames.SEGMENTS) &&
                    !fileName.Equals(IndexFileNames.SEGMENTS_GEN) &&
                    SegmentInfos.GenerationFromSegmentsFileName(fileName) < currentGen)
                {

                    SegmentInfos sis = new SegmentInfos();
                    try
                    {
                        // IOException allowed to throw there, in case
                        // segments_N is corrupt
                        sis.Read(dir, fileName);
                    }
                    catch (System.IO.FileNotFoundException fnfe)
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
                        commits.Add(new StandardDirectoryReader.ReaderCommit(sis, dir));
                }
            }

            // Ensure that the commit points are sorted in ascending order.
            commits.Sort();

            return commits;
        }

        public static bool IndexExists(Directory directory)
        {
            // LUCENE-2812, LUCENE-2727, LUCENE-4738: this logic will
            // return true in cases that should arguably be false,
            // such as only IW.prepareCommit has been called, or a
            // corrupt first commit, but it's too deadly to make
            // this logic "smarter" and risk accidentally returning
            // false due to various cases like file description
            // exhaustion, access denited, etc., because in that
            // case IndexWriter may delete the entire index.  It's
            // safer to err towards "index exists" than try to be
            // smart about detecting not-yet-fully-committed or
            // corrupt indices.  This means that IndexWriter will
            // throw an exception on such indices and the app must
            // resolve the situation manually:
            String[] files;
            try
            {
                files = directory.ListAll();
            }
            catch (NoSuchDirectoryException)
            {
                // Directory does not exist --> no index exists
                return false;
            }

            // Defensive: maybe a Directory impl returns null
            // instead of throwing NoSuchDirectoryException:
            if (files != null)
            {
                String prefix = IndexFileNames.SEGMENTS + "_";
                foreach (String file in files)
                {
                    if (file.StartsWith(prefix) || file.Equals(IndexFileNames.SEGMENTS_GEN))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected DirectoryReader(Directory directory, AtomicReader[] segmentReaders)
            : base(segmentReaders)
        {
            this.directory = directory;
        }

        public Directory Directory
        {
            get { return directory; }
        }

        protected internal abstract DirectoryReader DoOpenIfChanged();

        protected internal abstract DirectoryReader DoOpenIfChanged(IndexCommit commit);

        protected internal abstract DirectoryReader DoOpenIfChanged(IndexWriter writer, bool applyAllDeletes);

        public abstract long Version { get; }

        public abstract bool IsCurrent { get; }

        public abstract IndexCommit IndexCommit { get; }
    }
}