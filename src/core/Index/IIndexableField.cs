using Lucene.Net.Analysis;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public interface IIndexableField
    {
        string Name { get; }

        IIndexableFieldType FieldTypeValue { get; }

        float Boost { get; }

        BytesRef BinaryValue { get; }

        string StringValue { get; }

        TextReader ReaderValue { get; }

        object NumericValue { get; }

        TokenStream TokenStream(Analyzer analyzer);
    }
}
