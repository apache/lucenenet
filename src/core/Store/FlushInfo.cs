using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Store
{
    public class FlushInfo
    {
        public readonly int numDocs;

        public readonly long estimatedSegmentSize;

        public FlushInfo(int numDocs, long estimatedSegmentSize)
        {
            this.numDocs = numDocs;
            this.estimatedSegmentSize = estimatedSegmentSize;
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result
                + (int)(estimatedSegmentSize ^ Number.URShift(estimatedSegmentSize, 32));
            result = prime * result + numDocs;
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            FlushInfo other = (FlushInfo)obj;
            if (estimatedSegmentSize != other.estimatedSegmentSize)
                return false;
            if (numDocs != other.numDocs)
                return false;
            return true;
        }

        public override string ToString()
        {
            return "FlushInfo [numDocs=" + numDocs + ", estimatedSegmentSize="
                + estimatedSegmentSize + "]";
        }
    }
}
