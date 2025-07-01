using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
using System.IO;

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
    /// <para />
    /// LUCENENET specific: In the Java implementation, this class' Main method
    /// was intended to be called from the command line. However, in .NET a
    /// method within a DLL can't be directly called from the command line so we
    /// provide a <see href="https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools">.NET tool</see>,
    /// <see href="https://www.nuget.org/packages/lucene-cli">lucene-cli</see>,
    /// with a command that maps to that method:
    /// index list-term-info
    /// </summary>
    public static class GetTermInfo // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {

        /// <summary>
        /// LUCENENET specific: In the Java implementation, this Main method
        /// was intended to be called from the command line. However, in .NET a
        /// method within a DLL can't be directly called from the command line so we
        /// provide a <see href="https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools">.NET tool</see>,
        /// <see href="https://www.nuget.org/packages/lucene-cli">lucene-cli</see>,
        /// with a command that maps to this method:
        /// index list-term-info
        /// </summary>
        /// <param name="args">The command line arguments</param>
        /// <exception cref="ArgumentException">Thrown if the incorrect number of arguments are provided</exception>
        public static void Main(string[] args)
        {
            // LUCENENET specific - CA2000: dispose of directory when finished
            FSDirectory dir = null;
            try
            {
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
                    throw new ArgumentException("GetTermInfo requires 3 arguments", nameof(args));
                    //Usage();
                    //Environment.Exit(1);
                }

                TermInfo(dir, new Term(field, inputStr));
            }
            finally
            {
                dir?.Dispose();
            }
        }

        public static void TermInfo(Store.Directory dir, Term term)
        {
            using IndexReader reader = DirectoryReader.Open(dir);
            Console.WriteLine("{0}:{1} \t totalTF = {2:#,##0} \t doc freq = {3:#,##0} \n", term.Field, term.Text, reader.TotalTermFreq(term), reader.DocFreq(term));
        }

        // LUCENENET specific - our wrapper console shows the correct usage
        //private static void Usage()
        //{
        //    Console.WriteLine("\n\nusage:\n\t" + "java " + typeof(GetTermInfo).FullName + " <index dir> field term \n\n");
        //}
    }
}
