using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Store
{
    public class MergeInfo
    {
        public readonly int totalDocCount;

        public readonly long estimatedMergeBytes;

        public readonly bool isExternal;

        public readonly int mergeMaxNumSegments;

        public MergeInfo(int totalDocCount, long estimatedMergeBytes, bool isExternal, int mergeMaxNumSegments)
        {
            this.totalDocCount = totalDocCount;
            this.estimatedMergeBytes = estimatedMergeBytes;
            this.isExternal = isExternal;
            this.mergeMaxNumSegments = mergeMaxNumSegments;
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result
                + (int)(estimatedMergeBytes ^ Number.URShift(estimatedMergeBytes, 32));
            result = prime * result + (isExternal ? 1231 : 1237);
            result = prime * result + mergeMaxNumSegments;
            result = prime * result + totalDocCount;
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
            MergeInfo other = (MergeInfo)obj;
            if (estimatedMergeBytes != other.estimatedMergeBytes)
                return false;
            if (isExternal != other.isExternal)
                return false;
            if (mergeMaxNumSegments != other.mergeMaxNumSegments)
                return false;
            if (totalDocCount != other.totalDocCount)
                return false;
            return true;
        }

        public override string ToString()
        {
            return "MergeInfo [totalDocCount=" + totalDocCount
                + ", estimatedMergeBytes=" + estimatedMergeBytes + ", isExternal="
                + isExternal + ", mergeMaxNumSegments=" + mergeMaxNumSegments + "]";
        }
    }
}
