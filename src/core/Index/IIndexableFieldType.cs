using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public interface IIndexableFieldType
    {
        bool Indexed { get; }

        bool Stored { get; }

        bool Tokenized { get; }

        bool StoreTermVectors { get; }

        bool StoreTermVectorOffsets { get; }

        bool StoreTermVectorPositions { get; }

        bool StoreTermVectorPayloads { get; }

        bool OmitNorms { get; }

        IndexOptions IndexOptions { get; }

        DocValuesType DocValueType { get; }
    }
}
