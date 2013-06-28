using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene3x
{
    [Obsolete]
    internal class Lucene3xFieldInfosFormat : FieldInfosFormat
    {
        private readonly FieldInfosReader reader = new Lucene3xFieldInfosReader();

        public override FieldInfosReader FieldInfosReader
        {
            get { return reader; }
        }

        public override FieldInfosWriter FieldInfosWriter
        {
            get { throw new NotSupportedException("this codec can only be used for reading"); }
        }
    }
}
