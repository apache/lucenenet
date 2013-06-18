using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;

namespace Lucene.Net.Index
{
    public class IndexFormatTooNewException : CorruptIndexException
    {
        public IndexFormatTooNewException(String resourceDesc, int version, int minVersion, int maxVersion)
            : base("Format version is not supported (resource: " + resourceDesc + "): "
              + version + " (needs to be between " + minVersion + " and " + maxVersion + ")")
        {
            //assert resourceDesc != null;
        }

        public IndexFormatTooNewException(DataInput input, int version, int minVersion, int maxVersion)
            : this(input.ToString(), version, minVersion, maxVersion)
        {
        }
    }
}
