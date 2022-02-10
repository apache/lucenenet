using J2N.Numerics;

namespace Lucene.Net.Store
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// <para>A MergeInfo provides information required for a MERGE context.
    /// It is used as part of an <see cref="IOContext"/> in case of MERGE context.</para>
    /// </summary>
    public class MergeInfo
    {
        public int TotalDocCount { get; private set; }

        public long EstimatedMergeBytes { get; private set; }

        public bool IsExternal { get; private set; }

        public int MergeMaxNumSegments { get; private set; }

        /// <summary>
        /// <para/>Creates a new <see cref="MergeInfo"/> instance from
        /// the values required for a MERGE <see cref="IOContext"/> context.
        /// <para/>
        /// These values are only estimates and are not the actual values.
        /// </summary>
        public MergeInfo(int totalDocCount, long estimatedMergeBytes, bool isExternal, int mergeMaxNumSegments)
        {
            this.TotalDocCount = totalDocCount;
            this.EstimatedMergeBytes = estimatedMergeBytes;
            this.IsExternal = isExternal;
            this.MergeMaxNumSegments = mergeMaxNumSegments;
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result 
                + (int)(EstimatedMergeBytes ^ (EstimatedMergeBytes.TripleShift(32)));
            result = prime * result + (IsExternal ? 1231 : 1237);
            result = prime * result + MergeMaxNumSegments;
            result = prime * result + TotalDocCount;
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj is null)
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            MergeInfo other = (MergeInfo)obj;
            if (EstimatedMergeBytes != other.EstimatedMergeBytes)
            {
                return false;
            }
            if (IsExternal != other.IsExternal)
            {
                return false;
            }
            if (MergeMaxNumSegments != other.MergeMaxNumSegments)
            {
                return false;
            }
            if (TotalDocCount != other.TotalDocCount)
            {
                return false;
            }
            return true;
        }

        public override string ToString()
        {
            return "MergeInfo [totalDocCount=" + TotalDocCount 
                + ", estimatedMergeBytes=" + EstimatedMergeBytes + ", isExternal=" 
                + IsExternal + ", mergeMaxNumSegments=" + MergeMaxNumSegments + "]";
        }
    }
}