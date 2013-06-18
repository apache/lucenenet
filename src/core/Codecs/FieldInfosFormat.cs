using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs
{
    public abstract class FieldInfosFormat
    {
        protected FieldInfosFormat()
        {
        }

        public abstract FieldInfosReader FieldInfosReader { get; }

        public abstract FieldInfosWriter FieldInfosWriter { get; }
    }
}
