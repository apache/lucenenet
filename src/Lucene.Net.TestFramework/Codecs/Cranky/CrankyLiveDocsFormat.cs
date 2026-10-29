using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Codecs.Cranky
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

    internal class CrankyLiveDocsFormat : LiveDocsFormat
    {
        private readonly LiveDocsFormat @delegate;
        private readonly Random random;

        internal CrankyLiveDocsFormat(LiveDocsFormat @delegate, Random random)
        {
            this.@delegate = @delegate;
            this.random = random;
        }

        public override IMutableBits NewLiveDocs(int size) => @delegate.NewLiveDocs(size);

        public override IMutableBits NewLiveDocs(IBits existing) => @delegate.NewLiveDocs(existing);

        public override IBits ReadLiveDocs(Directory dir, SegmentCommitInfo info, IOContext context)
            => @delegate.ReadLiveDocs(dir, info, context);

        public override void WriteLiveDocs(IMutableBits bits, Directory dir, SegmentCommitInfo info, int newDelCount,
            IOContext context)
        {
            if (random.Next(100) == 0)
            {
                throw new IOException("Fake IOException from LiveDocsFormat.WriteLiveDocs()");
            }

            @delegate.WriteLiveDocs(bits, dir, info, newDelCount, context);
        }

        public override void Files(SegmentCommitInfo info, ICollection<string> files)
        {
            // TODO: is this called only from write? if so we should throw exception!
            @delegate.Files(info, files);
        }
    }
}
