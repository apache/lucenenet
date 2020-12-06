using Lucene.Net.Util;
using System;
using System.IO;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Search.Suggest.Fst
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
    /// Try to build a suggester from a large data set. The input is a simple text
    /// file, newline-delimited.
    /// </summary>
    public static class LargeInputFST // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        // LUCENENET specific - renaming from Main() because we must only have 1 entry point.
        // Not sure why this utility is in a test project anyway - this seems like something that should
        // be in Lucene.Net.Suggest so we can put it into the lucene-cli tool.
        public static void Main2(string[] args) 
        {
            FileInfo input = new FileInfo("/home/dweiss/tmp/shuffled.dict");

            int buckets = 20;
            int shareMaxTail = 10;

            ExternalRefSorter sorter = new ExternalRefSorter(new OfflineSorter());
            FSTCompletionBuilder builder = new FSTCompletionBuilder(buckets, sorter, shareMaxTail);

            TextReader reader =
                new StreamReader(
                    new FileStream(input.FullName, FileMode.Open), Encoding.UTF8);

            BytesRef scratch = new BytesRef();
            string line;
            int count = 0;
            while ((line = reader.ReadLine()) != null)
            {
                scratch.CopyChars(line);
                builder.Add(scratch, count % buckets);
                if ((count++ % 100000) == 0)
                {
                    Console.WriteLine("Line: " + count);
                }
            }

            Console.WriteLine("Building FSTCompletion.");
            FSTCompletion completion = builder.Build();

            FileInfo fstFile = new FileInfo("completion.fst");
            Console.WriteLine("Done. Writing automaton: " + fstFile.FullName);
            completion.FST.Save(fstFile);
            sorter.Dispose();
        }
    }
}
