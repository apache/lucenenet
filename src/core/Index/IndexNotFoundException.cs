using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public sealed class IndexNotFoundException : FileNotFoundException
    {
        public IndexNotFoundException(string msg)
            : base(msg)
        {
        }
    }
}
