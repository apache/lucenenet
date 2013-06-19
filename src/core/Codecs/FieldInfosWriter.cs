using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace Lucene.Net.Codecs
{
    public abstract class FieldInfosWriter
    {
        protected FieldInfosWriter()
        {
        }

        public abstract void Write(Directory directory, string segmentName, FieldInfos infos, IOContext context);
    }
}
