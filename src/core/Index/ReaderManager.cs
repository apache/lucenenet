using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace Lucene.Net.Index
{
    public sealed class ReaderManager : ReferenceManager<DirectoryReader>
    {
        public ReaderManager(IndexWriter writer, bool applyAllDeletes) 
        {
            current = DirectoryReader.Open(writer, applyAllDeletes);
        }

        public ReaderManager(Directory dir)
        {
            current = DirectoryReader.Open(dir);
        }

        protected override void DecRef(DirectoryReader reference) {
            reference.DecRef();
        }
  
        protected DirectoryReader RefreshIfNeeded(DirectoryReader referenceToRefresh) 
        {
            return DirectoryReader.OpenIfChanged(referenceToRefresh);
        }
  
        protected override bool TryIncRef(DirectoryReader reference) 
        {
            return reference.TryIncRef();
        }
    }
}
