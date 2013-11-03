using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Util
{
    public class PrintTaxonomyStats
    {
        public static void Main(String[] args)
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
                Console.Out.WriteLine("\nUsage: java -classpath ... org.apache.lucene.facet.util.PrintTaxonomyStats [-printTree] /path/to/taxononmy/index\n");
                Environment.Exit(1);
            }

            Directory dir = FSDirectory.Open(path);
            TaxonomyReader r = new DirectoryTaxonomyReader(dir);
            PrintStats(r, Console.Out, printTree);
            r.Dispose();
            dir.Dispose();
        }

        public static void PrintStats(TaxonomyReader r, System.IO.TextWriter out_renamed, bool printTree)
        {
            out_renamed.WriteLine(r.Size + @" total categories.");
            TaxonomyReader.ChildrenIterator it = r.GetChildren(TaxonomyReader.ROOT_ORDINAL);
            int child;
            while ((child = it.Next()) != TaxonomyReader.INVALID_ORDINAL)
            {
                TaxonomyReader.ChildrenIterator chilrenIt = r.GetChildren(child);
                int numImmediateChildren = 0;
                while (chilrenIt.Next() != TaxonomyReader.INVALID_ORDINAL)
                {
                    numImmediateChildren++;
                }

                CategoryPath cp = r.GetPath(child);
                out_renamed.WriteLine(@"/" + cp + @": " + numImmediateChildren + @" immediate children; " + (1 + CountAllChildren(r, child)) + @" total categories");
                if (printTree)
                {
                    PrintAllChildren(out_renamed, r, child, @"  ", 1);
                }
            }
        }

        private static int CountAllChildren(TaxonomyReader r, int ord)
        {
            int count = 0;
            TaxonomyReader.ChildrenIterator it = r.GetChildren(ord);
            int child;
            while ((child = it.Next()) != TaxonomyReader.INVALID_ORDINAL)
            {
                count += 1 + CountAllChildren(r, child);
            }

            return count;
        }

        private static void PrintAllChildren(System.IO.TextWriter out_renamed, TaxonomyReader r, int ord, string indent, int depth)
        {
            TaxonomyReader.ChildrenIterator it = r.GetChildren(ord);
            int child;
            while ((child = it.Next()) != TaxonomyReader.INVALID_ORDINAL)
            {
                out_renamed.WriteLine(indent + @"/" + r.GetPath(child).components[depth]);
                PrintAllChildren(out_renamed, r, child, indent + @"  ", depth + 1);
            }
        }
    }
}
