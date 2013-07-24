using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public abstract class FilterDirectoryReader : DirectoryReader
    {
        public abstract class SubReaderWrapper
        {
            internal AtomicReader[] Wrap<T>(IList<T> readers)
                where T : AtomicReader
            {
                AtomicReader[] wrapped = new AtomicReader[readers.Count];
                for (int i = 0; i < readers.Count; i++)
                {
                    wrapped[i] = Wrap(readers[i]);
                }
                return wrapped;
            }

            public SubReaderWrapper() { }

            public abstract AtomicReader Wrap(AtomicReader reader);
        }

        public class StandardReaderWrapper : SubReaderWrapper
        {
            public StandardReaderWrapper() { }

            public override AtomicReader Wrap(AtomicReader reader)
            {
                return reader;
            }
        }

        protected readonly DirectoryReader instance;

        public FilterDirectoryReader(DirectoryReader instance)
            : this(instance, new StandardReaderWrapper())
        {
        }

        public FilterDirectoryReader(DirectoryReader instance, SubReaderWrapper wrapper)
            : base(instance.Directory, wrapper.Wrap(instance.GetSequentialSubReaders()))
        {
            this.instance = instance;
        }

        protected abstract DirectoryReader DoWrapDirectoryReader(DirectoryReader instance);

        private DirectoryReader WrapDirectoryReader(DirectoryReader instance)
        {
            return instance == null ? null : DoWrapDirectoryReader(instance);
        }

        protected internal override DirectoryReader DoOpenIfChanged()
        {
            return WrapDirectoryReader(instance.DoOpenIfChanged());
        }

        protected internal override DirectoryReader DoOpenIfChanged(IndexCommit commit)
        {
            return WrapDirectoryReader(instance.DoOpenIfChanged(commit));
        }

        protected internal override DirectoryReader DoOpenIfChanged(IndexWriter writer, bool applyAllDeletes)
        {
            return WrapDirectoryReader(instance.DoOpenIfChanged(writer, applyAllDeletes));
        }

        public override long Version
        {
            get { return instance.Version; }
        }

        public override bool IsCurrent
        {
            get { return instance.IsCurrent; }
        }

        public override IndexCommit IndexCommit
        {
            get { return instance.IndexCommit; }
        }

        protected override void DoClose()
        {
            instance.DoClose();
        }
    }
}
