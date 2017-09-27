using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaDocToMarkdownConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length < 2)
            {
                Usage();
            }

            Console.WriteLine(string.Format("Converting '{0}' to '{1}'...", args[0], args[1]));

            //new DocConverter().ConvertDoc(@"F:\Projects\_Test\lucene-solr-4.8.0\lucene\demo\src\java\overview.html", @"F:\Projects\lucenenet\");
            new DocConverter().Convert(args[0], args[1]);

            Console.WriteLine("Conversion complete!");

#if DEBUG
            Console.ReadKey();
#endif
        }

        private static void Usage()
        {
            Console.WriteLine("Usage: JavaDocToMarkdownConverter[.exe] <LUCENE DIRECTORY> <LUCENENET DIRECTORY>");
            Console.WriteLine();
            Console.WriteLine(" Arguments:");
            Console.WriteLine(@"   LUCENE DIRECTORY: The root directory of the lucene project to convert (excluding SOLR). Example: F:\lucene-solr-4.8.0\lucene\");
            Console.WriteLine(@"   LUCENENET DIRECTORY: The root directory of Lucene.Net. Example: F:\Projects\lucenenet\");
        }
    }
}
