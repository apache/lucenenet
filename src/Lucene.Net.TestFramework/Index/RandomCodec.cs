using Lucene.Net.Codecs;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

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

    using AssertingDocValuesFormat = Lucene.Net.Codecs.Asserting.AssertingDocValuesFormat;
    using AssertingPostingsFormat = Lucene.Net.Codecs.Asserting.AssertingPostingsFormat;

    using DocValuesFormat = Lucene.Net.Codecs.DocValuesFormat;

    using TestBloomFilteredLucene41Postings = Lucene.Net.Codecs.Bloom.TestBloomFilteredLucene41Postings;
    using DiskDocValuesFormat = Lucene.Net.Codecs.DiskDV.DiskDocValuesFormat;
    using Lucene41PostingsFormat = Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat;

    using Lucene41WithOrds = Lucene.Net.Codecs.Lucene41Ords.Lucene41WithOrds;
    using Lucene45DocValuesFormat = Lucene.Net.Codecs.Lucene45.Lucene45DocValuesFormat;
    using Lucene46Codec = Lucene.Net.Codecs.Lucene46.Lucene46Codec;
    using PostingsFormat = Lucene.Net.Codecs.PostingsFormat;

    using DirectPostingsFormat = Lucene.Net.Codecs.Memory.DirectPostingsFormat;
    using MemoryDocValuesFormat = Lucene.Net.Codecs.Memory.MemoryDocValuesFormat;
    using MemoryPostingsFormat = Lucene.Net.Codecs.Memory.MemoryPostingsFormat;
    using MockFixedIntBlockPostingsFormat = Lucene.Net.Codecs.MockIntBlock.MockFixedIntBlockPostingsFormat;
    using MockVariableInt32BlockPostingsFormat = Lucene.Net.Codecs.MockIntBlock.MockVariableInt32BlockPostingsFormat;
    using MockRandomPostingsFormat = Lucene.Net.Codecs.MockRandom.MockRandomPostingsFormat;
    using MockSepPostingsFormat = Lucene.Net.Codecs.MockSep.MockSepPostingsFormat;
    using NestedPulsingPostingsFormat = Lucene.Net.Codecs.NestedPulsing.NestedPulsingPostingsFormat;
    using Pulsing41PostingsFormat = Lucene.Net.Codecs.Pulsing.Pulsing41PostingsFormat;
    using SimpleTextDocValuesFormat = Lucene.Net.Codecs.SimpleText.SimpleTextDocValuesFormat;
    using SimpleTextPostingsFormat = Lucene.Net.Codecs.SimpleText.SimpleTextPostingsFormat;
    using FSTOrdPostingsFormat = Lucene.Net.Codecs.Memory.FSTOrdPostingsFormat;
    using FSTOrdPulsing41PostingsFormat = Lucene.Net.Codecs.Memory.FSTOrdPulsing41PostingsFormat;
    using FSTPostingsFormat = Lucene.Net.Codecs.Memory.FSTPostingsFormat;
    using FSTPulsing41PostingsFormat = Lucene.Net.Codecs.Memory.FSTPulsing41PostingsFormat;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Codec that assigns per-field random postings formats.
    /// <para/>
    /// The same field/format assignment will happen regardless of order,
    /// a hash is computed up front that determines the mapping.
    /// this means fields can be put into things like HashSets and added to
    /// documents in different orders and the test will still be deterministic
    /// and reproducable.
    /// </summary>
    [ExcludeCodecFromScan] // LUCENENET specific - we don't want this codec to replace Lucene46Codec during testing - some of these codecs are read-only
    public class RandomCodec : Lucene46Codec
    {
        /// <summary>
        /// Shuffled list of postings formats to use for new mappings </summary>
        private IList<PostingsFormat> formats = new List<PostingsFormat>();

        /// <summary>
        /// Shuffled list of docvalues formats to use for new mappings </summary>
        private IList<DocValuesFormat> dvFormats = new List<DocValuesFormat>();

        /// <summary>
        /// unique set of format names this codec knows about </summary>
        public ISet<string> formatNames = new HashSet<string>();

        /// <summary>
        /// unique set of docvalues format names this codec knows about </summary>
        public ISet<string> dvFormatNames = new HashSet<string>();

        /// <summary>
        /// memorized field->postingsformat mappings </summary>
        // note: we have to sync this map even though its just for debugging/toString,
        // otherwise DWPT's .toString() calls that iterate over the map can
        // cause concurrentmodificationexception if indexwriter's infostream is on
        private readonly IDictionary<string, PostingsFormat> previousMappings = new ConcurrentDictionary<string, PostingsFormat>(StringComparer.Ordinal);

        private IDictionary<string, DocValuesFormat> previousDVMappings = new ConcurrentDictionary<string, DocValuesFormat>(StringComparer.Ordinal);
        private readonly int perFieldSeed;

        public override PostingsFormat GetPostingsFormatForField(string name)
        {
            PostingsFormat codec;
            if (!previousMappings.TryGetValue(name, out codec) || codec == null)
            {
                codec = formats[Math.Abs(perFieldSeed ^ name.GetHashCode()) % formats.Count];
                if (codec is SimpleTextPostingsFormat && perFieldSeed % 5 != 0)
                {
                  // make simpletext rarer, choose again
                  codec = formats[Math.Abs(perFieldSeed ^ name.ToUpperInvariant().GetHashCode()) % formats.Count];
                }
                previousMappings[name] = codec;
                // Safety:
                Debug.Assert(previousMappings.Count < 10000, "test went insane");
            }

            //if (LuceneTestCase.VERBOSE)
            //{
                Console.WriteLine("RandomCodec.GetPostingsFormatForField(\"" + name + "\") returned '" + codec.Name + "' with underlying type '" + codec.GetType().ToString() + "'.");
            //}

            return codec;
        }

        public override DocValuesFormat GetDocValuesFormatForField(string name)
        {
            DocValuesFormat codec;
            if (!previousDVMappings.TryGetValue(name, out codec) || codec == null)
            {
                codec = dvFormats[Math.Abs(perFieldSeed ^ name.GetHashCode()) % dvFormats.Count];
                if (codec is SimpleTextDocValuesFormat && perFieldSeed % 5 != 0)
                {
                  // make simpletext rarer, choose again
                  codec = dvFormats[Math.Abs(perFieldSeed ^ name.ToUpperInvariant().GetHashCode()) % dvFormats.Count];
                }
                previousDVMappings[name] = codec;
                // Safety:
                Debug.Assert(previousDVMappings.Count < 10000, "test went insane");
            }

            //if (LuceneTestCase.VERBOSE)
            //{
                Console.WriteLine("RandomCodec.GetDocValuesFormatForField(\"" + name + "\") returned '" + codec.Name + "' with underlying type '" + codec.GetType().ToString() + "'.");
            //}

            return codec;
        }

        public RandomCodec(Random random, ISet<string> avoidCodecs)
        {
            this.perFieldSeed = random.Next();
            // TODO: make it possible to specify min/max iterms per
            // block via CL:
            int minItemsPerBlock = TestUtil.NextInt(random, 2, 100);
            int maxItemsPerBlock = 2 * (Math.Max(2, minItemsPerBlock - 1)) + random.Next(100);
            int lowFreqCutoff = TestUtil.NextInt(random, 2, 100);

            Add(avoidCodecs,
                new Lucene41PostingsFormat(minItemsPerBlock, maxItemsPerBlock),
                new FSTPostingsFormat(),
                new FSTOrdPostingsFormat(),
                new FSTPulsing41PostingsFormat(1 + random.Next(20)), new FSTOrdPulsing41PostingsFormat(1 + random.Next(20)),
                new DirectPostingsFormat(LuceneTestCase.Rarely(random) ? 1 : (LuceneTestCase.Rarely(random) ? int.MaxValue : maxItemsPerBlock), 
                                        LuceneTestCase.Rarely(random) ? 1 : (LuceneTestCase.Rarely(random) ? int.MaxValue : lowFreqCutoff)),
                new Pulsing41PostingsFormat(1 + random.Next(20), minItemsPerBlock, maxItemsPerBlock),
                // add pulsing again with (usually) different parameters
                new Pulsing41PostingsFormat(1 + random.Next(20), minItemsPerBlock, maxItemsPerBlock),
                //TODO as a PostingsFormat which wraps others, we should allow TestBloomFilteredLucene41Postings to be constructed 
                //with a choice of concrete PostingsFormats. Maybe useful to have a generic means of marking and dealing 
                //with such "wrapper" classes?
                new TestBloomFilteredLucene41Postings(), 
                new MockSepPostingsFormat(), 
                new MockFixedIntBlockPostingsFormat(TestUtil.NextInt(random, 1, 2000)),
                new MockVariableInt32BlockPostingsFormat(TestUtil.NextInt(random, 1, 127)), 
                new MockRandomPostingsFormat(random),
                new NestedPulsingPostingsFormat(), 
                new Lucene41WithOrds(), 
                new SimpleTextPostingsFormat(),
                new AssertingPostingsFormat(),
                new MemoryPostingsFormat(true, random.nextFloat()), 
                new MemoryPostingsFormat(false, random.nextFloat())
            );

            AddDocValues(avoidCodecs, 
                new Lucene45DocValuesFormat(), 
                new DiskDocValuesFormat(), 
                new MemoryDocValuesFormat(), 
                new SimpleTextDocValuesFormat(), 
                new AssertingDocValuesFormat());

            Collections.Shuffle(formats);
            Collections.Shuffle(dvFormats);

            // Avoid too many open files:
            if (formats.Count > 4)
            {
                formats = formats.SubList(0, 4);
            }
            if (dvFormats.Count > 4)
            {
                dvFormats = dvFormats.SubList(0, 4);
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
                    formats.Add(p);
                    formatNames.Add(p.Name);
                }
            }
        }

        private void AddDocValues(ISet<string> avoidCodecs, params DocValuesFormat[] docvalues)
        {
            foreach (DocValuesFormat d in docvalues)
            {
                if (!avoidCodecs.Contains(d.Name))
                {
                    dvFormats.Add(d);
                    dvFormatNames.Add(d.Name);
                }
            }
        }

        public override string ToString()
        {
            // LUCENENET NOTE: using toString() extension method on dictionaries to print out their contents
            return base.ToString() + ": " + previousMappings.toString() + ", docValues:" + previousDVMappings.toString();
        }
    }
}