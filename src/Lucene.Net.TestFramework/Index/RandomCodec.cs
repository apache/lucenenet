using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Asserting;
using Lucene.Net.Codecs.Bloom;
using Lucene.Net.Codecs.DiskDV;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Codecs.Lucene41Ords;
using Lucene.Net.Codecs.Lucene45;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Codecs.Memory;
using Lucene.Net.Codecs.MockIntBlock;
using Lucene.Net.Codecs.MockRandom;
using Lucene.Net.Codecs.MockSep;
using Lucene.Net.Codecs.NestedPulsing;
using Lucene.Net.Codecs.Pulsing;
using Lucene.Net.Codecs.SimpleText;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Util;
using RandomizedTesting.Generators;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;
using J2N.Collections.Generic.Extensions;

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
    /// <see cref="Codec"/> that assigns per-field random <see cref="Codecs.PostingsFormat"/>s.
    /// <para/>
    /// The same field/format assignment will happen regardless of order,
    /// a hash is computed up front that determines the mapping.
    /// This means fields can be put into things like <see cref="HashSet{T}"/>s and added to
    /// documents in different orders and the test will still be deterministic
    /// and reproducable.
    /// </summary>
    [ExcludeCodecFromScan] // LUCENENET specific - we don't want this codec to replace Lucene46Codec during testing - some of these codecs are read-only
    public class RandomCodec : Lucene46Codec
    {
        /// <summary>
        /// Shuffled list of postings formats to use for new mappings </summary>
        private readonly JCG.List<PostingsFormat> formats = new JCG.List<PostingsFormat>(); // LUCENENET: marked readonly

        /// <summary>
        /// Shuffled list of docvalues formats to use for new mappings </summary>
        private readonly JCG.List<DocValuesFormat> dvFormats = new JCG.List<DocValuesFormat>(); // LUCENENET: marked readonly

        /// <summary>
        /// unique set of format names this codec knows about </summary>
        public ISet<string> FormatNames { get; set; } = new JCG.HashSet<string>();

        /// <summary>
        /// unique set of docvalues format names this codec knows about </summary>
        public ISet<string> DvFormatNames { get; set; } = new JCG.HashSet<string>();

        /// <summary>
        /// memorized field->postingsformat mappings </summary>
        // note: we have to sync this map even though its just for debugging/toString,
        // otherwise DWPT's .toString() calls that iterate over the map can
        // cause concurrentmodificationexception if indexwriter's infostream is on
        private readonly IDictionary<string, PostingsFormat> previousMappings = new ConcurrentDictionary<string, PostingsFormat>(StringComparer.Ordinal);

        private readonly IDictionary<string, DocValuesFormat> previousDVMappings = new ConcurrentDictionary<string, DocValuesFormat>(StringComparer.Ordinal); // LUCENENET: marked readonly
        private readonly int perFieldSeed;

        public override PostingsFormat GetPostingsFormatForField(string name)
        {
            if (!previousMappings.TryGetValue(name, out PostingsFormat codec) || codec is null)
            {
                codec = formats[Math.Abs(perFieldSeed ^ name.GetHashCode()) % formats.Count];
                if (codec is SimpleTextPostingsFormat && perFieldSeed % 5 != 0)
                {
                    // make simpletext rarer, choose again
                    codec = formats[Math.Abs(perFieldSeed ^ name.ToUpperInvariant().GetHashCode()) % formats.Count];
                }
                previousMappings[name] = codec;
                // Safety:
                if (Debugging.AssertsEnabled) Debugging.Assert(previousMappings.Count < 10000, "test went insane");
            }

            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("RandomCodec.GetPostingsFormatForField(\"" + name + "\") returned '" + codec.Name + "' with underlying type '" + codec.GetType().ToString() + "'.");
            }

            return codec;
        }

        public override DocValuesFormat GetDocValuesFormatForField(string name)
        {
            if (!previousDVMappings.TryGetValue(name, out DocValuesFormat codec) || codec is null)
            {
                codec = dvFormats[Math.Abs(perFieldSeed ^ name.GetHashCode()) % dvFormats.Count];
                if (codec is SimpleTextDocValuesFormat && perFieldSeed % 5 != 0)
                {
                    // make simpletext rarer, choose again
                    codec = dvFormats[Math.Abs(perFieldSeed ^ name.ToUpperInvariant().GetHashCode()) % dvFormats.Count];
                }
                previousDVMappings[name] = codec;
                // Safety:
                if (Debugging.AssertsEnabled) Debugging.Assert(previousDVMappings.Count < 10000, "test went insane");
            }

            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("RandomCodec.GetDocValuesFormatForField(\"" + name + "\") returned '" + codec.Name + "' with underlying type '" + codec.GetType().ToString() + "'.");
            }

            return codec;
        }

        public RandomCodec(Random random, ISet<string> avoidCodecs)
        {
            this.perFieldSeed = random.Next();
            // TODO: make it possible to specify min/max iterms per
            // block via CL:
            int minItemsPerBlock = TestUtil.NextInt32(random, 2, 100);
            int maxItemsPerBlock = 2 * (Math.Max(2, minItemsPerBlock - 1)) + random.Next(100);
            int lowFreqCutoff = TestUtil.NextInt32(random, 2, 100);

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
                new MockFixedInt32BlockPostingsFormat(TestUtil.NextInt32(random, 1, 2000)),
                new MockVariableInt32BlockPostingsFormat(TestUtil.NextInt32(random, 1, 127)), 
                new MockRandomPostingsFormat(random),
                new NestedPulsingPostingsFormat(), 
                new Lucene41WithOrds(), 
                new SimpleTextPostingsFormat(),
                new AssertingPostingsFormat(),
                new MemoryPostingsFormat(true, random.NextSingle()), 
                new MemoryPostingsFormat(false, random.NextSingle())
            );

            AddDocValues(avoidCodecs, 
                new Lucene45DocValuesFormat(), 
                new DiskDocValuesFormat(), 
                new MemoryDocValuesFormat(), 
                new SimpleTextDocValuesFormat(), 
                new AssertingDocValuesFormat());

            formats.Shuffle(random);
            dvFormats.Shuffle(random);

            // Avoid too many open files:
            if (formats.Count > 4)
            {
                formats = formats.GetView(0, 4); // LUCENENET: Checked length for correctness
            }
            if (dvFormats.Count > 4)
            {
                dvFormats = dvFormats.GetView(0, 4); // LUCENENET: Checked length for correctness
            }
        }

        public RandomCodec(Random random)
            : this(random, Collections.EmptySet<string>())
        {
        }

        private void Add(ISet<string> avoidCodecs, params PostingsFormat[] postings)
        {
            foreach (PostingsFormat p in postings)
            {
                if (!avoidCodecs.Contains(p.Name))
                {
                    formats.Add(p);
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
                    dvFormats.Add(d);
                    DvFormatNames.Add(d.Name);
                }
            }
        }

        public override string ToString()
        {
            // LUCENENET NOTE: using StringFormatter on dictionaries to print out their contents
            return string.Format(J2N.Text.StringFormatter.InvariantCulture, "{0}: {1}, docValues:{2}", base.ToString(), previousMappings, previousDVMappings);
        }
    }
}