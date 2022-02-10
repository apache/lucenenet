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
    /// <para>A FlushInfo provides information required for a FLUSH context.
    /// It is used as part of an <see cref="IOContext"/> in case of FLUSH context.</para>
    /// </summary>
    public class FlushInfo
    {
        public int NumDocs { get; private set; }

        public long EstimatedSegmentSize { get; private set; }

        /// <summary>
        /// <para/>Creates a new <see cref="FlushInfo"/> instance from
        /// the values required for a FLUSH <see cref="IOContext"/> context.
        /// <para/>
        /// These values are only estimates and are not the actual values.
        /// </summary>
        public FlushInfo(int numDocs, long estimatedSegmentSize)
        {
            this.NumDocs = numDocs;
            this.EstimatedSegmentSize = estimatedSegmentSize;
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + (int)(EstimatedSegmentSize ^ (EstimatedSegmentSize.TripleShift(32)));
            result = prime * result + NumDocs;
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
            FlushInfo other = (FlushInfo)obj;
            if (EstimatedSegmentSize != other.EstimatedSegmentSize)
            {
                return false;
            }
            if (NumDocs != other.NumDocs)
            {
                return false;
            }
            return true;
        }

        public override string ToString()
        {
            return "FlushInfo [numDocs=" + NumDocs + ", estimatedSegmentSize=" + EstimatedSegmentSize + "]";
        }
    }
}