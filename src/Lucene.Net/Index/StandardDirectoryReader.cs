using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

    using Directory = Lucene.Net.Store.Directory;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;

    internal sealed class StandardDirectoryReader : DirectoryReader
    {
        private readonly IndexWriter writer;
        private readonly SegmentInfos segmentInfos;
        private readonly int termInfosIndexDivisor;
        private readonly bool applyAllDeletes;

        /// <summary>
        /// called only from static <c>Open()</c> methods </summary>
        internal StandardDirectoryReader(Directory directory, AtomicReader[] readers, IndexWriter writer, SegmentInfos sis, int termInfosIndexDivisor, bool applyAllDeletes)
            : base(directory, readers)
        {
            this.writer = writer;
            this.segmentInfos = sis;
            this.termInfosIndexDivisor = termInfosIndexDivisor;
            this.applyAllDeletes = applyAllDeletes;
        }

        /// <summary>
        /// called from <c>DirectoryReader.Open(...)</c> methods </summary>
        internal static DirectoryReader Open(Directory directory, IndexCommit commit, int termInfosIndexDivisor)
        {
            return (DirectoryReader)new FindSegmentsFileAnonymousClass(directory, termInfosIndexDivisor).Run(commit);
        }

        private sealed class FindSegmentsFileAnonymousClass : SegmentInfos.FindSegmentsFile
        {
            private readonly int termInfosIndexDivisor;

            public FindSegmentsFileAnonymousClass(Directory directory, int termInfosIndexDivisor)
                : base(directory)
            {
                this.termInfosIndexDivisor = termInfosIndexDivisor;
            }

            protected internal override object DoBody(string segmentFileName)
            {
                var sis = new SegmentInfos();
                sis.Read(directory, segmentFileName);
                var readers = new SegmentReader[sis.Count];
                // LUCENENET: Ported over changes from 4.8.1 to this method
                for (int i = sis.Count - 1; i >= 0; i--)
                {
                    //IOException prior = null; // LUCENENET: Not used
                    bool success = false;
                    try
                    {
                        readers[i] = new SegmentReader(sis[i], termInfosIndexDivisor, IOContext.READ);
                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            IOUtils.DisposeWhileHandlingException(readers);
                        }
                    }
                }
                return new StandardDirectoryReader(directory, readers, null, sis, termInfosIndexDivisor, false);
            }
        }

        /// <summary>
        /// Used by near real-time search </summary>
        internal static DirectoryReader Open(IndexWriter writer, SegmentInfos infos, bool applyAllDeletes)
        {
            // IndexWriter synchronizes externally before calling
            // us, which ensures infos will not change; so there's
            // no need to process segments in reverse order
            int numSegments = infos.Count;

            IList<SegmentReader> readers = new JCG.List<SegmentReader>();
            Directory dir = writer.Directory;

            SegmentInfos segmentInfos = (SegmentInfos)infos.Clone();
            int infosUpto = 0;
            bool success = false;
            try
            {
                for (int i = 0; i < numSegments; i++)
                {
                    // NOTE: important that we use infos not
                    // segmentInfos here, so that we are passing the
                    // actual instance of SegmentInfoPerCommit in
                    // IndexWriter's segmentInfos:
                    SegmentCommitInfo info = infos[i];
                    if (Debugging.AssertsEnabled) Debugging.Assert(info.Info.Dir == dir);
                    ReadersAndUpdates rld = writer.readerPool.Get(info, true);
                    try
                    {
                        SegmentReader reader = rld.GetReadOnlyClone(IOContext.READ);
                        if (reader.NumDocs > 0 || writer.KeepFullyDeletedSegments)
                        {
                            // Steal the ref:
                            readers.Add(reader);
                            infosUpto++;
                        }
                        else
                        {
                            reader.DecRef();
                            segmentInfos.Remove(infosUpto);
                        }
                    }
                    finally
                    {
                        writer.readerPool.Release(rld);
                    }
                }

                writer.IncRefDeleter(segmentInfos);

                StandardDirectoryReader result = new StandardDirectoryReader(dir, readers.ToArray(), writer, segmentInfos, writer.Config.ReaderTermsIndexDivisor, applyAllDeletes);
                success = true;
                return result;
            }
            finally
            {
                if (!success)
                {
                    foreach (SegmentReader r in readers)
                    {
                        try
                        {
                            r.DecRef();
                        }
                        catch (Exception th) when (th.IsThrowable())
                        {
                            // ignore any exception that is thrown here to not mask any original
                            // exception.
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This constructor is only used for <see cref="DoOpenIfChanged(SegmentInfos)"/> </summary>
        private static DirectoryReader Open(Directory directory, SegmentInfos infos, IList<IndexReader> oldReaders, int termInfosIndexDivisor) // LUCENENET: Changed from AtomicReader to IndexReader to eliminate casting from the 1 place this is called from
        {
            // we put the old SegmentReaders in a map, that allows us
            // to lookup a reader using its segment name
            IDictionary<string, int> segmentReaders = new Dictionary<string, int>();

            if (oldReaders != null)
            {
                // create a Map SegmentName->SegmentReader
                for (int i = 0, c = oldReaders.Count; i < c; i++)
                {
                    SegmentReader sr = (SegmentReader)oldReaders[i];
                    segmentReaders[sr.SegmentName] = i;
                }
            }

            SegmentReader[] newReaders = new SegmentReader[infos.Count];

            // remember which readers are shared between the old and the re-opened
            // DirectoryReader - we have to incRef those readers
            bool[] readerShared = new bool[infos.Count];

            for (int i = infos.Count - 1; i >= 0; i--)
            {
                // find SegmentReader for this segment
                if (!segmentReaders.TryGetValue(infos[i].Info.Name, out int oldReaderIndex))
                {
                    // this is a new segment, no old SegmentReader can be reused
                    newReaders[i] = null;
                }
                else
                {
                    // there is an old reader for this segment - we'll try to reopen it
                    newReaders[i] = (SegmentReader)oldReaders[oldReaderIndex];
                }

                bool success = false;
                Exception prior = null;
                try
                {
                    SegmentReader newReader;
                    if (newReaders[i] is null || infos[i].Info.UseCompoundFile != newReaders[i].SegmentInfo.Info.UseCompoundFile)
                    {
                        // this is a new reader; in case we hit an exception we can close it safely
                        newReader = new SegmentReader(infos[i], termInfosIndexDivisor, IOContext.READ);
                        readerShared[i] = false;
                        newReaders[i] = newReader;
                    }
                    else
                    {
                        if (newReaders[i].SegmentInfo.DelGen == infos[i].DelGen && newReaders[i].SegmentInfo.FieldInfosGen == infos[i].FieldInfosGen)
                        {
                            // No change; this reader will be shared between
                            // the old and the new one, so we must incRef
                            // it:
                            readerShared[i] = true;
                            newReaders[i].IncRef();
                        }
                        else
                        {
                            // there are changes to the reader, either liveDocs or DV updates
                            readerShared[i] = false;
                            // Steal the ref returned by SegmentReader ctor:
                            if (Debugging.AssertsEnabled)
                            {
                                Debugging.Assert(infos[i].Info.Dir == newReaders[i].SegmentInfo.Info.Dir);
                                Debugging.Assert(infos[i].HasDeletions || infos[i].HasFieldUpdates);
                            }
                            if (newReaders[i].SegmentInfo.DelGen == infos[i].DelGen)
                            {
                                // only DV updates
                                newReaders[i] = new SegmentReader(infos[i], newReaders[i], newReaders[i].LiveDocs, newReaders[i].NumDocs);
                            }
                            else
                            {
                                // both DV and liveDocs have changed
                                newReaders[i] = new SegmentReader(infos[i], newReaders[i]);
                            }
                        }
                    }
                    success = true;
                }
                catch (Exception ex) when (ex.IsThrowable())
                {
                    prior = ex;
                }
                finally
                {
                    if (!success)
                    {
                        for (i++; i < infos.Count; i++)
                        {
                            if (newReaders[i] != null)
                            {
                                try
                                {
                                    if (!readerShared[i])
                                    {
                                        // this is a new subReader that is not used by the old one,
                                        // we can close it
                                        newReaders[i].Dispose();
                                    }
                                    else
                                    {
                                        // this subReader is also used by the old reader, so instead
                                        // closing we must decRef it
                                        newReaders[i].DecRef();
                                    }
                                }
                                catch (Exception t) when (t.IsThrowable())
                                {
                                    if (prior is null)
                                    {
                                        prior = t;
                                    }
                                }
                            }
                        }
                    }
                    // throw the first exception
                    IOUtils.ReThrow(prior);
                }
            }
            return new StandardDirectoryReader(directory, newReaders, null, infos, termInfosIndexDivisor, false);
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append(this.GetType().Name);
            buffer.Append('(');
            string segmentsFile = segmentInfos.GetSegmentsFileName();
            if (segmentsFile != null)
            {
                buffer.Append(segmentsFile).Append(':').Append(segmentInfos.Version);
            }
            if (writer != null)
            {
                buffer.Append(":nrt");
            }
            foreach (AtomicReader r in GetSequentialSubReaders())
            {
                buffer.Append(' ');
                buffer.Append(r);
            }
            buffer.Append(')');
            return buffer.ToString();
        }

        protected internal override DirectoryReader DoOpenIfChanged()
        {
            return DoOpenIfChanged((IndexCommit)null);
        }

        protected internal override DirectoryReader DoOpenIfChanged(IndexCommit commit)
        {
            EnsureOpen();

            // If we were obtained by writer.getReader(), re-ask the
            // writer to get a new reader.
            if (writer != null)
            {
                return DoOpenFromWriter(commit);
            }
            else
            {
                return DoOpenNoWriter(commit);
            }
        }

        protected internal override DirectoryReader DoOpenIfChanged(IndexWriter writer, bool applyAllDeletes)
        {
            EnsureOpen();
            if (writer == this.writer && applyAllDeletes == this.applyAllDeletes)
            {
                return DoOpenFromWriter(null);
            }
            else
            {
                return writer.GetReader(applyAllDeletes);
            }
        }

        private DirectoryReader DoOpenFromWriter(IndexCommit commit)
        {
            if (commit != null)
            {
                return DoOpenFromCommit(commit);
            }

            if (writer.NrtIsCurrent(segmentInfos))
            {
                return null;
            }

            DirectoryReader reader = writer.GetReader(applyAllDeletes);

            // If in fact no changes took place, return null:
            if (reader.Version == segmentInfos.Version)
            {
                reader.DecRef();
                return null;
            }

            return reader;
        }

        private DirectoryReader DoOpenNoWriter(IndexCommit commit)
        {
            if (commit is null)
            {
                if (IsCurrent())
                {
                    return null;
                }
            }
            else
            {
                if (m_directory != commit.Directory)
                {
                    throw new IOException("the specified commit does not match the specified Directory");
                }
                if (segmentInfos != null && commit.SegmentsFileName.Equals(segmentInfos.GetSegmentsFileName(), StringComparison.Ordinal))
                {
                    return null;
                }
            }

            return DoOpenFromCommit(commit);
        }

        private DirectoryReader DoOpenFromCommit(IndexCommit commit)
        {
            return (DirectoryReader)new FindSegmentsFileAnonymousClass2(this, m_directory).Run(commit);
        }

        private sealed class FindSegmentsFileAnonymousClass2 : SegmentInfos.FindSegmentsFile
        {
            private readonly StandardDirectoryReader outerInstance;

            public FindSegmentsFileAnonymousClass2(StandardDirectoryReader outerInstance, Directory directory)
                : base(directory)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override object DoBody(string segmentFileName)
            {
                SegmentInfos infos = new SegmentInfos();
                infos.Read(outerInstance.m_directory, segmentFileName);
                return outerInstance.DoOpenIfChanged(infos);
            }
        }

        internal DirectoryReader DoOpenIfChanged(SegmentInfos infos)
        {
            return StandardDirectoryReader.Open(m_directory, infos, GetSequentialSubReaders(), termInfosIndexDivisor);
        }

        public override long Version
        {
            get
            {
                EnsureOpen();
                return segmentInfos.Version;
            }
        }

        public override bool IsCurrent()
        {
            EnsureOpen();
            if (writer is null || writer.IsClosed)
            {
                // Fully read the segments file: this ensures that it's
                // completely written so that if
                // IndexWriter.prepareCommit has been called (but not
                // yet commit), then the reader will still see itself as
                // current:
                SegmentInfos sis = new SegmentInfos();
                sis.Read(m_directory);

                // we loaded SegmentInfos from the directory
                return sis.Version == segmentInfos.Version;
            }
            else
            {
                return writer.NrtIsCurrent(segmentInfos);
            }
        }

        protected internal override void DoClose()
        {
            Exception firstExc = null;
            foreach (AtomicReader r in GetSequentialSubReaders())
            {
                // try to close each reader, even if an exception is thrown
                try
                {
                    r.DecRef();
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    if (firstExc is null)
                    {
                        firstExc = t;
                    }
                }
            }

            if (writer != null)
            {
                try
                {
                    writer.DecRefDeleter(segmentInfos);
                }
                catch (Exception ex) when (ex.IsAlreadyClosedException())
                {
                    // this is OK, it just means our original writer was
                    // closed before we were, and this may leave some
                    // un-referenced files in the index, which is
                    // harmless.  The next time IW is opened on the
                    // index, it will delete them.
                }
            }

            // throw the first exception
            IOUtils.ReThrow(firstExc);
        }

        public override IndexCommit IndexCommit
        {
            get
            {
                EnsureOpen();
                return new ReaderCommit(segmentInfos, m_directory);
            }
        }

        internal sealed class ReaderCommit : IndexCommit
        {
            internal string segmentsFileName;
            internal ICollection<string> files;
            internal Directory dir;
            internal long generation;
            internal readonly IDictionary<string, string> userData;
            internal readonly int segmentCount;

            internal ReaderCommit(SegmentInfos infos, Directory dir)
            {
                segmentsFileName = infos.GetSegmentsFileName();
                this.dir = dir;
                userData = infos.UserData;
                files = infos.GetFiles(dir, true);
                generation = infos.Generation;
                segmentCount = infos.Count;
            }

            public override string ToString()
            {
                return "DirectoryReader.ReaderCommit(" + segmentsFileName + ")";
            }

            public override int SegmentCount => segmentCount;

            public override string SegmentsFileName => segmentsFileName;

            public override ICollection<string> FileNames => files;

            public override Directory Directory => dir;

            public override long Generation => generation;

            public override bool IsDeleted => false;

            public override IDictionary<string, string> UserData => userData;

            public override void Delete()
            {
                throw UnsupportedOperationException.Create("this IndexCommit does not support deletions");
            }
        }
    }
}