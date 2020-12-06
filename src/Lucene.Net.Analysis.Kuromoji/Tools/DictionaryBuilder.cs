using System;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Analysis.Ja.Util
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

    public static class DictionaryBuilder // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        public enum DictionaryFormat { IPADIC, UNIDIC };

        static DictionaryBuilder()
        {
#if FEATURE_ENCODINGPROVIDERS
            // Support for EUC-JP encoding. See: https://docs.microsoft.com/en-us/dotnet/api/system.text.codepagesencodingprovider?view=netcore-2.0
            var encodingProvider = System.Text.CodePagesEncodingProvider.Instance;
            System.Text.Encoding.RegisterProvider(encodingProvider);
#endif
        }

        public static void Build(DictionaryFormat format,
            string inputDirname,
            string outputDirname,
            string encoding,
            bool normalizeEntry)
        {
            Console.WriteLine("building tokeninfo dict...");
            TokenInfoDictionaryBuilder tokenInfoBuilder = new TokenInfoDictionaryBuilder(format, encoding, normalizeEntry);
            TokenInfoDictionaryWriter tokenInfoDictionary = tokenInfoBuilder.Build(inputDirname);
            tokenInfoDictionary.Write(outputDirname);
            //tokenInfoDictionary = null; // LUCENENET: IDE0059: Remove unnecessary value assignment
            //tokenInfoBuilder = null; // LUCENENET: IDE0059: Remove unnecessary value assignment
            Console.WriteLine("done");

            Console.WriteLine("building unknown word dict...");
            UnknownDictionaryBuilder unkBuilder = new UnknownDictionaryBuilder(encoding);
            UnknownDictionaryWriter unkDictionary = unkBuilder.Build(inputDirname);
            unkDictionary.Write(outputDirname);
            //unkDictionary = null; // LUCENENET: IDE0059: Remove unnecessary value assignment
            //unkBuilder = null; // LUCENENET: IDE0059: Remove unnecessary value assignment
            Console.WriteLine("done");

            Console.WriteLine("building connection costs...");
            ConnectionCostsWriter connectionCosts
                = ConnectionCostsBuilder.Build(inputDirname + System.IO.Path.DirectorySeparatorChar + "matrix.def");
            connectionCosts.Write(outputDirname);
            Console.WriteLine("done");
        }

        public static void Main(string[] args)
        {
            DictionaryFormat format;
            if (args[0].Equals("ipadic", StringComparison.OrdinalIgnoreCase))
            {
                format = DictionaryFormat.IPADIC;
            }
            else if (args[0].Equals("unidic", StringComparison.OrdinalIgnoreCase))
            {
                format = DictionaryFormat.UNIDIC;
            }
            else
            {
                Console.Error.WriteLine("Illegal format " + args[0] + " using unidic instead");
                format = DictionaryFormat.IPADIC;
            }

            string inputDirname = args[1];
            string outputDirname = args[2];
            string inputEncoding = args[3];
            bool normalizeEntries = bool.Parse(args[4]);

            Console.WriteLine("dictionary builder");
            Console.WriteLine();
            Console.WriteLine("dictionary format: " + format);
            Console.WriteLine("input directory: " + inputDirname);
            Console.WriteLine("output directory: " + outputDirname);
            Console.WriteLine("input encoding: " + inputEncoding);
            Console.WriteLine("normalize entries: " + normalizeEntries);
            Console.WriteLine();
            DictionaryBuilder.Build(format, inputDirname, outputDirname, inputEncoding, normalizeEntries);
        }
    }
}
