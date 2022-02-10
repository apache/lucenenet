// lucene version compatibility level: 4.8.1
using J2N.Numerics;
using System;

namespace Lucene.Net.Analysis.Cn.Smart.Hhmm
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
    /// SmartChineseAnalyzer internal node representation
    /// <para>
    /// Used by <see cref="BiSegGraph"/> to maximize the segmentation with the Viterbi algorithm.
    /// </para>
    /// @lucene.experimental
    /// </summary>
    internal class PathNode : IComparable<PathNode>
    {
        public double Weight { get; set; }

        public int PreNode { get; set; }

        public virtual int CompareTo(PathNode pn)
        {
            if (Weight < pn.Weight)
                return -1;
            else if (Weight == pn.Weight)
                return 0;
            else
                return 1;
        }

        /// <summary>
        /// <see cref="object.GetHashCode()"/>
        /// </summary>
        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + PreNode;
            long temp;
            temp = J2N.BitConversion.DoubleToInt64Bits(Weight);
            result = prime * result + (int)(temp ^ temp.TripleShift(32));
            return result;
        }

        /// <summary>
        /// <see cref="object.Equals(object)"/>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj is null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            PathNode other = (PathNode)obj;
            if (PreNode != other.PreNode)
                return false;
            if (J2N.BitConversion.DoubleToInt64Bits(Weight) != J2N.BitConversion
                .DoubleToInt64Bits(other.Weight))
                return false;
            return true;
        }
    }
}
