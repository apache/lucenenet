using System;
using System.IO;

namespace Lucene.Net.Facet.Taxonomy
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


    using ChildrenIterator = Lucene.Net.Facet.Taxonomy.TaxonomyReader.ChildrenIterator;
    using DirectoryTaxonomyReader = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader;
    using Directory = Lucene.Net.Store.Directory;
    using FSDirectory = Lucene.Net.Store.FSDirectory;

    /// <summary>
    /// Prints how many ords are under each dimension. </summary>

    // java -cp ../build/core/classes/java:../build/facet/classes/java org.apache.lucene.facet.util.PrintTaxonomyStats -printTree /s2/scratch/indices/wikibig.trunk.noparents.facets.Lucene41.nd1M/facets
    public class PrintTaxonomyStats
    {

        /// <summary>
        /// Sole constructor. </summary>
        public PrintTaxonomyStats()
        {
        }

        /// <summary>
        /// Command-line tool. </summary>
        public static int Main(string[] args)
        {
            bool printTree = false;
            string path = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-printTree"))
                {
                    printTree = true;
                }
                else
                {
                    path = args[i];
                }
            }
            if (args.Length != (printTree ? 2 : 1))
            {
                Console.WriteLine("\nUsage: java -classpath ... org.apache.lucene.facet.util.PrintTaxonomyStats [-printTree] /path/to/taxononmy/index\n");
                return 1;
            }
            Store.Directory dir = FSDirectory.Open(new DirectoryInfo(path));
            var r = new DirectoryTaxonomyReader(dir);
            PrintStats(r, System.Console.Out, printTree);
            r.Dispose();
  
            return 0;
        }

        /// <summary>
        /// Recursively prints stats for all ordinals. </summary>
        public static void PrintStats(TaxonomyReader r, TextWriter @out, bool printTree)
        {
            @out.WriteLine(r.Size + " total categories.");

            ChildrenIterator it = r.GetChildren(TaxonomyReader.ROOT_ORDINAL);
            int child;
            while ((child = it.Next()) != TaxonomyReader.INVALID_ORDINAL)
            {
                ChildrenIterator chilrenIt = r.GetChildren(child);
                int numImmediateChildren = 0;
                while (chilrenIt.Next() != TaxonomyReader.INVALID_ORDINAL)
                {
                    numImmediateChildren++;
                }
                FacetLabel cp = r.GetPath(child);
                @out.WriteLine("/" + cp.Components[0] + ": " + numImmediateChildren + " immediate children; " + (1 + CountAllChildren(r, child)) + " total categories");
                if (printTree)
                {
                    PrintAllChildren(@out, r, child, "  ", 1);
                }
            }
        }

        private static int CountAllChildren(TaxonomyReader r, int ord)
        {
            int count = 0;
            ChildrenIterator it = r.GetChildren(ord);
            int child;
            while ((child = it.Next()) != TaxonomyReader.INVALID_ORDINAL)
            {
                count += 1 + CountAllChildren(r, child);
            }
            return count;
        }

        private static void PrintAllChildren(TextWriter @out, TaxonomyReader r, int ord, string indent, int depth)
        {
            ChildrenIterator it = r.GetChildren(ord);
            int child;
            while ((child = it.Next()) != TaxonomyReader.INVALID_ORDINAL)
            {
                @out.WriteLine(indent + "/" + r.GetPath(child).Components[depth]);
                PrintAllChildren(@out, r, child, indent + "  ", depth + 1);
            }
        }
    }

}