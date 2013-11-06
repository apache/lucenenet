using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Taxonomy.Directory
{
    internal static class Consts
    {
        internal const string FULL = @"$full_path$";
        internal const string FIELD_PAYLOADS = @"$payloads$";
        internal const string PAYLOAD_PARENT = @"p";
        internal static readonly BytesRef PAYLOAD_PARENT_BYTES_REF = new BytesRef(PAYLOAD_PARENT);
        internal const char DEFAULT_DELIMITER = (char)31;
    }
}
