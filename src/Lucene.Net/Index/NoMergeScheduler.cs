using System.Runtime.CompilerServices;

namespace Lucene.Net.Index
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
    /// A <see cref="MergeScheduler"/> which never executes any merges. It is also a
    /// singleton and can be accessed through <see cref="NoMergeScheduler.INSTANCE"/>. Use
    /// it if you want to prevent an <see cref="IndexWriter"/> from ever executing merges,
    /// regardless of the <see cref="MergePolicy"/> used. Note that you can achieve the
    /// same thing by using <see cref="NoMergePolicy"/>, however with
    /// <see cref="NoMergeScheduler"/> you also ensure that no unnecessary code of any
    /// <see cref="MergeScheduler"/> implementation is ever executed. Hence it is
    /// recommended to use both if you want to disable merges from ever happening.
    /// </summary>
    public sealed class NoMergeScheduler : MergeScheduler
    {
        /// <summary>
        /// The single instance of <see cref="NoMergeScheduler"/> </summary>
        public static readonly MergeScheduler INSTANCE = new NoMergeScheduler();

        private NoMergeScheduler()
        {
            // prevent instantiation
        }

        protected override void Dispose(bool disposing)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Merge(IndexWriter writer, MergeTrigger trigger, bool newMergesFound)
        {
        }

        public override object Clone()
        {
            return this;
        }
    }
}