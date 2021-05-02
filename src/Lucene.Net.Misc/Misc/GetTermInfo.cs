using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
using System.IO;
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
    /// Utility to get document frequency and total number of occurrences (sum of the tf for each doc)  of a term. 
    /// </summary>
    public static class GetTermInfo // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        public static void Main(string[] args)
        {

            FSDirectory dir; // LUCENENET: IDE0059: Remove unnecessary value assignment
            string inputStr; // LUCENENET: IDE0059: Remove unnecessary value assignment
            string field; // LUCENENET: IDE0059: Remove unnecessary value assignment

            if (args.Length == 3)
            {
                dir = FSDirectory.Open(new DirectoryInfo(args[0]));
                field = args[1];
                inputStr = args[2];
            }
            else
            {
                // LUCENENET specific - our wrapper console shows the correct usage
                throw new ArgumentException();
                //Usage();
                //Environment.Exit(1);
            }

            TermInfo(dir, new Term(field, inputStr));
        }

        public static void TermInfo(Store.Directory dir, Term term)
        {
            IndexReader reader = DirectoryReader.Open(dir);
            Console.WriteLine("{0}:{1} \t totalTF = {2:#,##0} \t doc freq = {3:#,##0} \n", term.Field, term.Text, reader.TotalTermFreq(term), reader.DocFreq(term));
        }

        // LUCENENET specific - our wrapper console shows the correct usage
        //private static void Usage()
        //{
        //    Console.WriteLine("\n\nusage:\n\t" + "java " + typeof(GetTermInfo).FullName + " <index dir> field term \n\n");
        //}
    }
}