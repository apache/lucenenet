using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
using System.IO;

namespace Lucene.Net.Benchmarks.ByTask.Tasks
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
    /// Adds an input index to an existing index, using
    /// <see cref="IndexWriter.AddIndexes(Store.Directory[])"/> or
    /// <see cref="IndexWriter.AddIndexes(IndexReader[])"/>. The location of the input
    /// index is specified by the parameter <see cref="ADDINDEXES_INPUT_DIR"/> and is
    /// assumed to be a directory on the file system.
    /// <para/>
    /// Takes optional parameter <see cref="useAddIndexesDir"/> which specifies which
    /// AddIndexes variant to use (defaults to <c>true</c>, to use <c>AddIndexes(Directory)</c>).
    /// </summary>
    public class AddIndexesTask : PerfTask
    {
        public static readonly string ADDINDEXES_INPUT_DIR = "addindexes.input.dir";

        public AddIndexesTask(PerfRunData runData)
            : base(runData)
        {
        }

        private bool useAddIndexesDir = true;
        private FSDirectory inputDir;

        public override void Setup()
        {
            base.Setup();
            string inputDirProp = RunData.Config.Get(ADDINDEXES_INPUT_DIR, null);
            if (inputDirProp is null)
            {
                throw new ArgumentException("config parameter " + ADDINDEXES_INPUT_DIR + " not specified in configuration");
            }
            inputDir = FSDirectory.Open(new DirectoryInfo(inputDirProp));
        }

        public override int DoLogic()
        {
            IndexWriter writer = RunData.IndexWriter;
            if (useAddIndexesDir)
            {
                writer.AddIndexes(inputDir);
            }
            else
            {
                IndexReader r = DirectoryReader.Open(inputDir);
                try
                {
                    writer.AddIndexes(r);
                }
                finally
                {
                    r.Dispose();
                }
            }
            return 1;
        }

        /// <summary>
        /// Set the params (useAddIndexesDir only)
        /// </summary>
        /// <param name="params">
        /// <c>useAddIndexesDir=true</c> for using <see cref="IndexWriter.AddIndexes(Store.Directory[])"/> or <c>false</c> for
        /// using <see cref="IndexWriter.AddIndexes(IndexReader[])"/>. Defaults to <c>true</c>.
        /// </param>
        public override void SetParams(string @params)
        {
            base.SetParams(@params);
            useAddIndexesDir = bool.Parse(@params);
        }

        public override bool SupportsParams => true;

        public override void TearDown()
        {
            inputDir?.Dispose();
            inputDir = null; // LUCENENET specific
            base.TearDown();
        }

        /// <summary>
        /// Releases resources used by the <see cref="AddIndexesTask"/> and
        /// if overridden in a derived class, optionally releases unmanaged resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.</param>

        // LUCENENET specific
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    inputDir?.Dispose(); // LUCENENET specific - dispose tokens and set to null
                    inputDir = null;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
