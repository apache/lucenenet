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

    internal class CrankySegmentInfoFormat : SegmentInfoFormat
    {
        private readonly SegmentInfoFormat @delegate;
        private readonly Random random;

        internal CrankySegmentInfoFormat(SegmentInfoFormat @delegate, Random random)
        {
            this.@delegate = @delegate;
            this.random = random;
        }

        public override SegmentInfoReader SegmentInfoReader => @delegate.SegmentInfoReader;

        public override SegmentInfoWriter SegmentInfoWriter
            => new CrankySegmentInfoWriter(@delegate.SegmentInfoWriter, random);

        private class CrankySegmentInfoWriter : SegmentInfoWriter
        {
            private readonly SegmentInfoWriter @delegate;
            private readonly Random random;

            public CrankySegmentInfoWriter(SegmentInfoWriter @delegate, Random random)
            {
                this.@delegate = @delegate;
                this.random = random;
            }

            public override void Write(Directory dir, SegmentInfo info, FieldInfos fis, IOContext ioContext)
            {
                if (random.Next(100) == 0)
                {
                    throw new IOException("Fake IOException from SegmentInfoWriter.Write()");
                }

                @delegate.Write(dir, info, fis, ioContext);
            }
        }
    }
}
