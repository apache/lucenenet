using Lucene.Net.Support;
using NUnit.Framework;
using System.Collections.Generic;

namespace Lucene.Net.Index
{
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;

    /// <summary>
    /// Common tests to all index formats.
    /// </summary>
    public abstract class BaseIndexFileFormatTestCase : LuceneTestCase
    {
        /// <summary>
        /// Returns the codec to run tests against </summary>
        protected abstract Codec Codec { get; }

        private Codec SavedCodec;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            // set the default codec, so adding test cases to this isn't fragile
            SavedCodec = Codec.Default;
            Codec.Default = Codec;
        }

        [TearDown]
        public override void TearDown()
        {
            Codec.Default = SavedCodec; // restore
            base.TearDown();
        }

        /// <summary>
        /// Add random fields to the provided document. </summary>
        protected internal abstract void AddRandomFields(Document doc);

        private IDictionary<string, long> BytesUsedByExtension(Directory d)
        {
            IDictionary<string, long> bytesUsedByExtension = new Dictionary<string, long>();
            foreach (string file in d.ListAll())
            {
				string ext = IndexFileNames.GetExtension(file) ?? string.Empty;
                long previousLength = bytesUsedByExtension.ContainsKey(ext) ? bytesUsedByExtension[ext] : 0;
                bytesUsedByExtension[ext] = previousLength + d.FileLength(file);
            }
			foreach (string item in ExcludedExtensionsFromByteCounts()) {
				bytesUsedByExtension.Remove(item);							
			}
            return bytesUsedByExtension;
        }

        /// <summary>
        /// Return the list of extensions that should be excluded from byte counts when
        /// comparing indices that store the same content.
        /// </summary>
        protected internal virtual ICollection<string> ExcludedExtensionsFromByteCounts()
        {
            return new HashSet<string>(Arrays.AsList(new string[] { "si", "lock" }));
            // segment infos store various pieces of information that don't solely depend
            // on the content of the index in the diagnostics (such as a timestamp) so we
            // exclude this file from the bytes counts
            // lock files are 0 bytes (one directory in the test could be RAMDir, the other FSDir)
        }

        /// <summary>
        /// The purpose of this test is to make sure that bulk merge doesn't accumulate useless data over runs.
        /// </summary>
        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestMergeStability()
        {
            Directory dir = NewDirectory();
            // do not use newMergePolicy that might return a MockMergePolicy that ignores the no-CFS ratio
            MergePolicy mp = NewTieredMergePolicy();
            mp.NoCFSRatio = 0;
            var cfg = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetUseCompoundFile(false).SetMergePolicy(mp);
            using (var w = new RandomIndexWriter(Random(), dir, cfg))
            {
                var numDocs = AtLeast(500);
                for (var i = 0; i < numDocs; ++i)
                {
                    var d = new Document();
                    AddRandomFields(d);
                    w.AddDocument(d);
                }
                w.ForceMerge(1);
                w.Commit();
            }
            IndexReader reader = DirectoryReader.Open(dir);

            Directory dir2 = NewDirectory();
            mp = NewTieredMergePolicy();
            mp.NoCFSRatio = 0;
            cfg = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetUseCompoundFile(false).SetMergePolicy(mp);

            using (var w = new RandomIndexWriter(Random(), dir2, cfg))
            {
                w.AddIndexes(reader);
                w.Commit();
            }

            assertEquals(BytesUsedByExtension(dir), BytesUsedByExtension(dir2));

            reader.Dispose();
            dir.Dispose();
            dir2.Dispose();
        }
    }
}