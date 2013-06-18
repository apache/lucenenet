using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace Lucene.Net.Codecs
{
    public abstract class FieldInfosReader
    {
        protected FieldInfosReader()
        {
        }

        public abstract FieldInfos Read(Directory directory, string segmentName, IOContext context);
    }
}
