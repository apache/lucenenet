// -----------------------------------------------------------------------
// <copyright company="Apache" file="RamUsageEstimator.cs">
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
// </copyright>
// -----------------------------------------------------------------------

namespace Lucene.Net.Util
{
    using System;

    /// <summary>
    ///     Estimates the size of a given object using a given <c>MemoryModel</c> 
    ///     for primitive size information.
    /// </summary>
    /// <remarks>
    ///     <note>
    ///          <para>
    ///             <b>Java File: </b> <a href="https://github.com/apache/lucene-solr/blob/trunk/lucene/src/java/org/apache/lucene/util/RamUsageEstimator.java">
    ///             lucene/src/java/org/apache/lucene/util/RamUseageEstimator.java
    ///         </a>
    ///         </para>
    ///         <para>
    ///             <b>C# File: </b> <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/src/Lucene.Net/Util/RamUsageEstimator.cs">
    ///              src/Lucene.Net/Util/RamUsageEstimator.cs
    ///             </a>
    ///         </para>
    ///         <para>
    ///             <b>C# Tests: </b>  <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/test/Lucene.Net.Test/Util/RamUsageEstimatorTest.cs">
    ///             test/Lucene.Net.Test/Util/RamUsageEstimatorTest.cs
    ///             </a>
    ///         </para>
    ///     </note>
    ///     <para>
    ///         Internally uses a Map to temporally hold a reference to every object seen.   
    ///     </para>
    ///     <para>
    ///         If checkInterned all strings checked will be interned, but those
    ///         that were not already interned will be released for GC when the 
    ///         estimate is complete.
    ///     </para>
    /// </remarks>
    public class RamUsageEstimator
    {
        /// <summary>
        /// The number of bytes for an object reference. This will either
        /// be 8 (64 bit) or 4 (32 bit).
        /// </summary>
        public static readonly int NumBytesObjectRef = IntPtr.Size;

        /// <summary>
        /// The number of bytes for a char.
        /// </summary>
        public const int NumBytesChar = 2;

        private string temp = string.Empty;

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.temp;
        }
    }
}