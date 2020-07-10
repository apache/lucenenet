using Lucene.Net.Util;

namespace Lucene.Net.Search.Suggest.Tst
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
    /// The class creates a TST node.
    /// </summary>

    public class TernaryTreeNode
    {

        /// <summary>
        /// Creates a new empty node </summary>
        public TernaryTreeNode()
        {
        }
        /// <summary>
        /// the character stored by a node. </summary>
        internal char splitchar;
        /// <summary>
        /// a reference object to the node containing character smaller than this node's character. </summary>
        internal TernaryTreeNode loKid;
        /// <summary>
        ///  a reference object to the node containing character next to this node's character as 
        ///  occurring in the inserted token.
        /// </summary>
        internal TernaryTreeNode eqKid;
        /// <summary>
        /// a reference object to the node containing character higher than this node's character. </summary>
        internal TernaryTreeNode hiKid;
        /// <summary>
        /// used by leaf nodes to store the complete tokens to be added to suggest list while 
        /// auto-completing the prefix.
        /// </summary>
        internal string token;
        internal object val;

        internal virtual long GetSizeInBytes()
        {
            long mem = RamUsageEstimator.ShallowSizeOf(this);
            if (loKid != null)
            {
                mem += loKid.GetSizeInBytes();
            }
            if (eqKid != null)
            {
                mem += eqKid.GetSizeInBytes();
            }
            if (hiKid != null)
            {
                mem += hiKid.GetSizeInBytes();
            }
            if (token != null)
            {
                mem += RamUsageEstimator.ShallowSizeOf(token) + RamUsageEstimator.NUM_BYTES_ARRAY_HEADER + RamUsageEstimator.NUM_BYTES_CHAR * token.Length;
            }
            mem += RamUsageEstimator.ShallowSizeOf(val);
            return mem;
        }
    }
}