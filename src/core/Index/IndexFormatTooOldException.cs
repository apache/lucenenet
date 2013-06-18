using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;

namespace Lucene.Net.Index
{
    public class IndexFormatTooOldException : CorruptIndexException
    {
        public IndexFormatTooOldException(String resourceDesc, String version)
            : base("Format version is not supported (resource: " + resourceDesc + "): " +
                version + ". This version of Lucene only supports indexes created with release 3.0 and later.")
        {
            //assert resourceDesc != null;
        }

        public IndexFormatTooOldException(DataInput input, String version)
            : this(input.ToString(), version)
        {
        }

        public IndexFormatTooOldException(String resourceDesc, int version, int minVersion, int maxVersion)
            : base("Format version is not supported (resource: " + resourceDesc + "): " +
                version + " (needs to be between " + minVersion + " and " + maxVersion +
            "). This version of Lucene only supports indexes created with release 3.0 and later.")
        {
            //assert resourceDesc != null;
        }

        public IndexFormatTooOldException(DataInput input, int version, int minVersion, int maxVersion)
            : this(input.ToString(), version, minVersion, maxVersion)
        {
        }
    }
}
