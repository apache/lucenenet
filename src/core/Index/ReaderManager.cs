using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Search;

namespace Lucene.Net.Index
{
    using Lucene.Net.Store;

    public sealed class ReaderManager : ReferenceManager<DirectoryReader>
    {
        public ReaderManager(IndexWriter writer, bool applyAllDeletes) 
        {
            current = DirectoryReader.open(writer, applyAllDeletes);
        }

        public ReaderManager(Directory dir)
        {
            current = DirectoryReader.Open(dir);
        }

        protected override void decRef(DirectoryReader reference) {
            reference.decRef();
        }
  
        protected  DirectoryReader refreshIfNeeded(DirectoryReader referenceToRefresh) 
        {
            return DirectoryReader.OpenIfChanged(referenceToRefresh);
        }
  
        protected override bool tryIncRef(DirectoryReader reference) 
        {
            return reference.tryIncRef();
        }
    }
}
