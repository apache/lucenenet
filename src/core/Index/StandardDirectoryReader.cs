using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IOException = System.IO.IOException;

namespace Lucene.Net.Index
{
    internal sealed class StandardDirectoryReader : DirectoryReader
    {
        private readonly IndexWriter writer;
        private readonly SegmentInfos segmentInfos;
        private readonly int termInfosIndexDivisor;
        private readonly bool applyAllDeletes;

        internal StandardDirectoryReader(Directory directory, AtomicReader[] readers, IndexWriter writer,
            SegmentInfos sis, int termInfosIndexDivisor, bool applyAllDeletes)
            : base(directory, readers)
        {
            this.writer = writer;
            this.segmentInfos = sis;
            this.termInfosIndexDivisor = termInfosIndexDivisor;
            this.applyAllDeletes = applyAllDeletes;
        }

        private sealed class AnonymousOpenFindSegmentsFile : SegmentInfos.FindSegmentsFile
        {
            public AnonymousOpenFindSegmentsFile(Directory dir)
                : base(dir)
            {
            }

            public override object DoBody(string segmentFileName)
            {
                SegmentInfos sis = new SegmentInfos();
                sis.Read(directory, segmentFileName);
                SegmentReader[] readers = new SegmentReader[sis.Count];
                for (int i = sis.Count - 1; i >= 0; i--)
                {
                    IOException prior = null;
                    bool success = false;
                    try
                    {
                        readers[i] = new SegmentReader(sis.Info(i), termInfosIndexDivisor, IOContext.READ);
                        success = true;
                    }
                    catch (IOException ex)
                    {
                        prior = ex;
                    }
                    finally
                    {
                        if (!success)
                            IOUtils.CloseWhileHandlingException(prior, readers);
                    }
                }
                return new StandardDirectoryReader(directory, readers, null, sis, termInfosIndexDivisor, false);
            }
        }

        internal static DirectoryReader Open(Directory directory, IndexCommit commit, int termInfosIndexDivisor)
        {
            return (DirectoryReader)new AnonymousOpenFindSegmentsFile(directory).Run(commit);
        }

        internal static DirectoryReader Open(IndexWriter writer, SegmentInfos infos, bool applyAllDeletes)
        {
            // IndexWriter synchronizes externally before calling
            // us, which ensures infos will not change; so there's
            // no need to process segments in reverse order
            int numSegments = infos.Count;

            List<SegmentReader> readers = new List<SegmentReader>();
            Directory dir = writer.Directory;

            SegmentInfos segmentInfos = (SegmentInfos)infos.Clone();
            int infosUpto = 0;
            for (int i = 0; i < numSegments; i++)
            {
                IOException prior = null;
                bool success = false;
                try
                {
                    // NOTE: important that we use infos not
                    // segmentInfos here, so that we are passing the
                    // actual instance of SegmentInfoPerCommit in
                    // IndexWriter's segmentInfos:
                    SegmentInfoPerCommit info = infos.Info(i);
                    //assert info.info.dir == dir;
                    ReadersAndLiveDocs rld = writer.readerPool.Get(info, true);
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
                            reader.Dispose();
                            segmentInfos.Remove(infosUpto);
                        }
                    }
                    finally
                    {
                        writer.readerPool.Release(rld);
                    }
                    success = true;
                }
                catch (IOException ex)
                {
                    prior = ex;
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.CloseWhileHandlingException(prior, readers);
                    }
                }
            }
            return new StandardDirectoryReader(dir, readers.ToArray(),
              writer, segmentInfos, writer.Config.ReaderTermsIndexDivisor, applyAllDeletes);
        }

        private static DirectoryReader Open(Directory directory, SegmentInfos infos, IList<AtomicReader> oldReaders,
            int termInfosIndexDivisor)
        {

            // we put the old SegmentReaders in a map, that allows us
            // to lookup a reader using its segment name
            IDictionary<String, int> segmentReaders = new HashMap<String, int>();

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
                int oldReaderIndex = segmentReaders[infos.Info(i).info.Name];
                if (oldReaderIndex == null)
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
                    if (newReaders[i] == null || infos.Info(i).info.UseCompoundFile != newReaders[i].SegmentInfo.info.UseCompoundFile)
                    {

                        // this is a new reader; in case we hit an exception we can close it safely
                        newReader = new SegmentReader(infos.Info(i), termInfosIndexDivisor, IOContext.READ);
                        readerShared[i] = false;
                        newReaders[i] = newReader;
                    }
                    else
                    {
                        if (newReaders[i].SegmentInfo.DelGen == infos.Info(i).DelGen)
                        {
                            // No change; this reader will be shared between
                            // the old and the new one, so we must incRef
                            // it:
                            readerShared[i] = true;
                            newReaders[i].IncRef();
                        }
                        else
                        {
                            readerShared[i] = false;
                            // Steal the ref returned by SegmentReader ctor:
                            //assert infos.info(i).info.dir == newReaders[i].getSegmentInfo().info.dir;
                            //assert infos.info(i).hasDeletions();
                            newReaders[i] = new SegmentReader(infos.Info(i), newReaders[i].core, IOContext.READ);
                        }
                    }
                    success = true;
                }
                catch (Exception ex)
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
                                catch (Exception t)
                                {
                                    if (prior == null) prior = t;
                                }
                            }
                        }
                    }
                    // throw the first exception
                    if (prior != null)
                    {
                        //if (prior is IOException) throw (IOException)prior;
                        //if (prior is SystemException) throw (SystemException)prior;
                        //if (prior is Error) throw (Error)prior;
                        throw prior;
                    }
                }
            }
            return new StandardDirectoryReader(directory, newReaders, null, infos, termInfosIndexDivisor, false);
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append(GetType().Name);
            buffer.Append('(');
            String segmentsFile = segmentInfos.SegmentsFileName;
            if (segmentsFile != null)
            {
                buffer.Append(segmentsFile).Append(":").Append(segmentInfos.Version);
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

        protected override DirectoryReader DoOpenIfChanged()
        {
            return DoOpenIfChanged((IndexCommit)null);
        }

        protected override DirectoryReader DoOpenIfChanged(IndexCommit commit)
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

        protected override DirectoryReader DoOpenIfChanged(IndexWriter writer, bool applyAllDeletes)
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
            if (commit == null)
            {
                if (IsCurrent)
                {
                    return null;
                }
            }
            else
            {
                if (directory != commit.Directory)
                {
                    throw new IOException("the specified commit does not match the specified Directory");
                }
                if (segmentInfos != null && commit.SegmentsFileName.Equals(segmentInfos.SegmentsFileName))
                {
                    return null;
                }
            }

            return DoOpenFromCommit(commit);
        }

        private sealed class AnonymousDoOpenFromCommitFindSegmentsFile : SegmentInfos.FindSegmentsFile
        {
            private readonly StandardDirectoryReader parent;

            public AnonymousDoOpenFromCommitFindSegmentsFile(StandardDirectoryReader parent, Directory dir)
                : base(dir)
            {
                this.parent = parent;
            }

            public override object DoBody(string segmentFileName)
            {
                SegmentInfos infos = new SegmentInfos();
                infos.Read(directory, segmentFileName);
                return parent.DoOpenIfChanged(infos);
            }
        }

        private DirectoryReader DoOpenFromCommit(IndexCommit commit)
        {
            return (DirectoryReader)new AnonymousDoOpenFromCommitFindSegmentsFile(this, directory).Run(commit);
        }

        internal DirectoryReader DoOpenIfChanged(SegmentInfos infos)
        {
            return StandardDirectoryReader.Open(directory, infos, GetSequentialSubReaders().OfType<AtomicReader>().ToList(), termInfosIndexDivisor);
        }

        public override long Version
        {
            get
            {
                EnsureOpen();
                return segmentInfos.Version;
            }
        }

        public override bool IsCurrent
        {
            get
            {
                EnsureOpen();
                if (writer == null || writer.IsClosed())
                {
                    // Fully read the segments file: this ensures that it's
                    // completely written so that if
                    // IndexWriter.prepareCommit has been called (but not
                    // yet commit), then the reader will still see itself as
                    // current:
                    SegmentInfos sis = new SegmentInfos();
                    sis.Read(directory);

                    // we loaded SegmentInfos from the directory
                    return sis.Version == segmentInfos.Version;
                }
                else
                {
                    return writer.NrtIsCurrent(segmentInfos);
                }
            }
        }

        protected override void DoClose()
        {
            Exception firstExc = null;
            foreach (AtomicReader r in GetSequentialSubReaders())
            {
                // try to close each reader, even if an exception is thrown
                try
                {
                    r.DecRef();
                }
                catch (Exception t)
                {
                    if (firstExc == null) firstExc = t;
                }
            }

            if (writer != null)
            {
                // Since we just closed, writer may now be able to
                // delete unused files:
                writer.DeletePendingFiles();
            }

            // throw the first exception
            if (firstExc != null)
            {
                //if (firstExc instanceof IOException) throw (IOException) firstExc;
                //if (firstExc instanceof RuntimeException) throw (RuntimeException) firstExc;
                //if (firstExc instanceof Error) throw (Error) firstExc;
                throw firstExc;
            }
        }

        public override IndexCommit IndexCommit
        {
            get
            {
                EnsureOpen();
                return new ReaderCommit(segmentInfos, directory);
            }
        }

        internal sealed class ReaderCommit : IndexCommit
        {
            private String segmentsFileName;
            internal ICollection<String> files;
            internal Directory dir;
            internal long generation;
            internal IDictionary<String, String> userData;
            private int segmentCount;

            internal ReaderCommit(SegmentInfos infos, Directory dir)
            {
                segmentsFileName = infos.SegmentsFileName;
                this.dir = dir;
                userData = infos.UserData;
                files = infos.Files(dir, true);
                generation = infos.Generation;
                segmentCount = infos.Count;
            }

            public override string ToString()
            {
                return "DirectoryReader.ReaderCommit(" + segmentsFileName + ")";
            }

            public override int SegmentCount
            {
                get { return segmentCount; }
            }

            public override string SegmentsFileName
            {
                get { return segmentsFileName; }
            }

            public override ICollection<string> FileNames
            {
                get { return files; }
            }

            public override Directory Directory
            {
                get { return dir; }
            }

            public override long Generation
            {
                get { return generation; }
            }

            public override bool IsDeleted
            {
                get { return false; }
            }

            public override IDictionary<string, string> UserData
            {
                get { return userData; }
            }

            public override void Delete()
            {
                throw new NotSupportedException("This IndexCommit does not support deletions");
            }
        }
    }
}
