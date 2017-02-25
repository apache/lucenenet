using Lucene.Net.Support;
using Lucene.Net.Codecs;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Index
{
    
    using AssertingDocValuesFormat = Lucene.Net.Codecs.Asserting.AssertingDocValuesFormat;
    using AssertingPostingsFormat = Lucene.Net.Codecs.Asserting.AssertingPostingsFormat;

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

    using DocValuesFormat = Lucene.Net.Codecs.DocValuesFormat;

    //using TestBloomFilteredLucene41Postings = Lucene.Net.Codecs.bloom.TestBloomFilteredLucene41Postings;
    //using DiskDocValuesFormat = Lucene.Net.Codecs.diskdv.DiskDocValuesFormat;
    using Lucene41PostingsFormat = Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat;

    //using Lucene41WithOrds = Lucene.Net.Codecs.Lucene41ords.Lucene41WithOrds;
    using Lucene45DocValuesFormat = Lucene.Net.Codecs.Lucene45.Lucene45DocValuesFormat;
    using Lucene46Codec = Lucene.Net.Codecs.Lucene46.Lucene46Codec;
    using PostingsFormat = Lucene.Net.Codecs.PostingsFormat;

    //using DirectPostingsFormat = Lucene.Net.Codecs.memory.DirectPostingsFormat;
    //using MemoryDocValuesFormat = Lucene.Net.Codecs.memory.MemoryDocValuesFormat;
    //using MemoryPostingsFormat = Lucene.Net.Codecs.memory.MemoryPostingsFormat;
    //using MockFixedIntBlockPostingsFormat = Lucene.Net.Codecs.mockintblock.MockFixedIntBlockPostingsFormat;
    //using MockVariableIntBlockPostingsFormat = Lucene.Net.Codecs.mockintblock.MockVariableIntBlockPostingsFormat;
    //using MockRandomPostingsFormat = Lucene.Net.Codecs.mockrandom.MockRandomPostingsFormat;
    //using MockSepPostingsFormat = Lucene.Net.Codecs.mocksep.MockSepPostingsFormat;
    //using NestedPulsingPostingsFormat = Lucene.Net.Codecs.nestedpulsing.NestedPulsingPostingsFormat;
    //using Pulsing41PostingsFormat = Lucene.Net.Codecs.pulsing.Pulsing41PostingsFormat;
    //using SimpleTextDocValuesFormat = Lucene.Net.Codecs.simpletext.SimpleTextDocValuesFormat;
    //using SimpleTextPostingsFormat = Lucene.Net.Codecs.simpletext.SimpleTextPostingsFormat;
    //using FSTOrdPostingsFormat = Lucene.Net.Codecs.memory.FSTOrdPostingsFormat;
    //using FSTOrdPulsing41PostingsFormat = Lucene.Net.Codecs.memory.FSTOrdPulsing41PostingsFormat;
    //using FSTPostingsFormat = Lucene.Net.Codecs.memory.FSTPostingsFormat;
    //using FSTPulsing41PostingsFormat = Lucene.Net.Codecs.memory.FSTPulsing41PostingsFormat;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Codec that assigns per-field random postings formats.
    /// <p>
    /// The same field/format assignment will happen regardless of order,
    /// a hash is computed up front that determines the mapping.
    /// this means fields can be put into things like HashSets and added to
    /// documents in different orders and the test will still be deterministic
    /// and reproducable.
    /// </summary>
    [IgnoreCodec] // LUCENENET TODO: I believe this codec should be not ignored in the test environment and should be used in place of Lucene46 codec.
    public class RandomCodec : Lucene46Codec
    {
        /// <summary>
        /// Shuffled list of postings formats to use for new mappings </summary>
        private IList<PostingsFormat> Formats = new List<PostingsFormat>();

        /// <summary>
        /// Shuffled list of docvalues formats to use for new mappings </summary>
        private IList<DocValuesFormat> DvFormats = new List<DocValuesFormat>();

        /// <summary>
        /// unique set of format names this codec knows about </summary>
        public HashSet<string> FormatNames = new HashSet<string>();

        /// <summary>
        /// unique set of docvalues format names this codec knows about </summary>
        public HashSet<string> DvFormatNames = new HashSet<string>();

        /// <summary>
        /// memorized field->postingsformat mappings </summary>
        // note: we have to sync this map even though its just for debugging/toString,
        // otherwise DWPT's .toString() calls that iterate over the map can
        // cause concurrentmodificationexception if indexwriter's infostream is on
        private readonly IDictionary<string, PostingsFormat> PreviousMappings = new ConcurrentHashMapWrapper<string, PostingsFormat>(new Dictionary<string, PostingsFormat>());

        private IDictionary<string, DocValuesFormat> PreviousDVMappings = new ConcurrentHashMapWrapper<string, DocValuesFormat>(new Dictionary<string, DocValuesFormat>());
        private readonly int PerFieldSeed;

        public override PostingsFormat GetPostingsFormatForField(string name)
        {
            PostingsFormat codec = PreviousMappings[name];
            if (codec == null)
            {
                codec = Formats[Math.Abs(PerFieldSeed ^ name.GetHashCode()) % Formats.Count];
                /*if (codec is SimpleTextPostingsFormat && PerFieldSeed % 5 != 0)
                {
                  // make simpletext rarer, choose again
                  codec = Formats[Math.Abs(PerFieldSeed ^ name.ToUpper(CultureInfo.InvariantCulture).GetHashCode()) % Formats.Count];
                }*/
                PreviousMappings[name] = codec;
                // Safety:
                Debug.Assert(PreviousMappings.Count < 10000, "test went insane");
            }
            return codec;
        }

        public override DocValuesFormat GetDocValuesFormatForField(string name)
        {
            DocValuesFormat codec = PreviousDVMappings[name];
            if (codec == null)
            {
                codec = DvFormats[Math.Abs(PerFieldSeed ^ name.GetHashCode()) % DvFormats.Count];
                /*if (codec is SimpleTextDocValuesFormat && PerFieldSeed % 5 != 0)
                {
                  // make simpletext rarer, choose again
                  codec = DvFormats[Math.Abs(PerFieldSeed ^ name.ToUpper(CultureInfo.InvariantCulture).GetHashCode()) % DvFormats.Count];
                }*/
                PreviousDVMappings[name] = codec;
                // Safety:
                Debug.Assert(PreviousDVMappings.Count < 10000, "test went insane");
            }
            return codec;
        }

        public RandomCodec(Random random, ISet<string> avoidCodecs)
        {
            this.PerFieldSeed = random.Next();
            // TODO: make it possible to specify min/max iterms per
            // block via CL:
            int minItemsPerBlock = TestUtil.NextInt(random, 2, 100);
            int maxItemsPerBlock = 2 * (Math.Max(2, minItemsPerBlock - 1)) + random.Next(100);
            int lowFreqCutoff = TestUtil.NextInt(random, 2, 100);

            // LUCENENET TODO: Finish RandomCodec implementation
            Add(avoidCodecs,
                new Lucene41PostingsFormat(minItemsPerBlock, maxItemsPerBlock),
                /*
                new FSTPostingsFormat(),
                new FSTOrdPostingsFormat(),
                new FSTPulsing41PostingsFormat(1 + random.Next(20)), new FSTOrdPulsing41PostingsFormat(1 + random.Next(20)),
                new DirectPostingsFormat(LuceneTestCase.Rarely(random) ? 1 : (LuceneTestCase.Rarely(random) ? int.MaxValue : maxItemsPerBlock), LuceneTestCase.Rarely(random) ? 1 : (LuceneTestCase.Rarely(random) ? int.MaxValue : lowFreqCutoff)),
                new Pulsing41PostingsFormat(1 + random.Next(20), minItemsPerBlock, maxItemsPerBlock), new Pulsing41PostingsFormat(1 + random.Next(20), minItemsPerBlock, maxItemsPerBlock),
                new TestBloomFilteredLucene41Postings(), new MockSepPostingsFormat(), new MockFixedIntBlockPostingsFormat(TestUtil.NextInt(random, 1, 2000)),
                new MockVariableIntBlockPostingsFormat(TestUtil.NextInt(random, 1, 127)), new MockRandomPostingsFormat(random),
                new NestedPulsingPostingsFormat(), new Lucene41WithOrds(), new SimpleTextPostingsFormat(),
                */
                new AssertingPostingsFormat()
                /*new MemoryPostingsFormat(true, random.nextFloat()), new MemoryPostingsFormat(false, random.nextFloat())*/
                );

            // add pulsing again with (usually) different parameters
            //TODO as a PostingsFormat which wraps others, we should allow TestBloomFilteredLucene41Postings to be constructed
            //with a choice of concrete PostingsFormats. Maybe useful to have a generic means of marking and dealing
            //with such "wrapper" classes?

            AddDocValues(avoidCodecs, new Lucene45DocValuesFormat(), /*new DiskDocValuesFormat(), new MemoryDocValuesFormat(), new SimpleTextDocValuesFormat(),*/ new AssertingDocValuesFormat());

            Collections.Shuffle(Formats);
            Collections.Shuffle(DvFormats);

            // Avoid too many open files:
            if (Formats.Count > 4)
            {
                Formats = Formats.SubList(0, 4);
            }
            if (DvFormats.Count > 4)
            {
                DvFormats = DvFormats.SubList(0, 4);
            }
        }

        public RandomCodec(Random random)
            : this(random, new HashSet<string>())
        {
        }

        private void Add(ISet<string> avoidCodecs, params PostingsFormat[] postings)
        {
            foreach (PostingsFormat p in postings)
            {
                if (!avoidCodecs.Contains(p.Name))
                {
                    Formats.Add(p);
                    FormatNames.Add(p.Name);
                }
            }
        }

        private void AddDocValues(ISet<string> avoidCodecs, params DocValuesFormat[] docvalues)
        {
            foreach (DocValuesFormat d in docvalues)
            {
                if (!avoidCodecs.Contains(d.Name))
                {
                    DvFormats.Add(d);
                    DvFormatNames.Add(d.Name);
                }
            }
        }

        public override string ToString()
        {
            return base.ToString() + ": " + PreviousMappings.ToString() + ", docValues:" + PreviousDVMappings.ToString();
        }
    }
}