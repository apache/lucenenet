using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Misc
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
    /// Merges indices specified on the command line into the index
    /// specified as the first command line argument.
    /// </summary>
    public static class IndexMergeTool // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        public static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                // LUCENENET specific - our wrapper console shows the correct usage
                throw new ArgumentException();
                //Console.Error.WriteLine("Usage: IndexMergeTool <mergedIndex> <index1> <index2> [index3] ...");
                //Environment.Exit(1);
            }
            using FSDirectory mergedIndex = FSDirectory.Open(new System.IO.DirectoryInfo(args[0]));
            using (IndexWriter writer = new IndexWriter(mergedIndex,
#pragma warning disable 612, 618
                new IndexWriterConfig(LuceneVersion.LUCENE_CURRENT, null)
#pragma warning restore 612, 618
                { OpenMode = OpenMode.CREATE }))
            {
                Directory[] indexes = new Directory[args.Length - 1];
                try
                {
                    for (int i = 1; i < args.Length; i++)
                    {
                        indexes[i - 1] = FSDirectory.Open(new System.IO.DirectoryInfo(args[i]));
                    }

                    Console.WriteLine("Merging...");
                    writer.AddIndexes(indexes);

                    Console.WriteLine("Full merge...");
                    writer.ForceMerge(1);
                }
                finally
                {
                    // LUCENENET specific - dispose directories
                    IOUtils.Dispose(indexes);
                }
            }
            Console.WriteLine("Done.");
        }
    }
}