using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
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

    internal class CrankyFieldInfosFormat : FieldInfosFormat
    {
        private readonly FieldInfosFormat @delegate;
        private readonly Random random;

        internal CrankyFieldInfosFormat(FieldInfosFormat @delegate, Random random)
        {
            this.@delegate = @delegate;
            this.random = random;
        }

        public override FieldInfosReader FieldInfosReader => @delegate.FieldInfosReader;

        public override FieldInfosWriter FieldInfosWriter
        {
            get
            {
                if (random.Next(100) == 0)
                {
                    throw new IOException("Fake IOException from FieldInfosFormat.FieldInfosWriter");
                }

                return new CrankyFieldInfosWriter(@delegate.FieldInfosWriter, random);
            }
        }

        private class CrankyFieldInfosWriter : FieldInfosWriter
        {
            private readonly FieldInfosWriter @delegate;
            private readonly Random random;

            public CrankyFieldInfosWriter(FieldInfosWriter @delegate, Random random)
            {
                this.@delegate = @delegate;
                this.random = random;
            }

            public override void Write(Directory directory, string segmentName, string segmentSuffix, FieldInfos infos,
                IOContext context)
            {
                if (random.Next(100) == 0)
                {
                    throw new IOException("Fake IOException from FieldInfosWriter.Write()");
                }

                @delegate.Write(directory, segmentName, segmentSuffix, infos, context);
            }
        }
    }
}
