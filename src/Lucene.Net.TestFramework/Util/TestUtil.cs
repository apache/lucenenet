using System.Numerics;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Util
{
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;

    //using RandomInts = com.carrotsearch.randomizedtesting.generators.RandomInts;
    //using RandomPicks = com.carrotsearch.randomizedtesting.generators.RandomPicks;
    using NUnit.Framework;
    using System.IO;
    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using BinaryDocValuesField = BinaryDocValuesField;

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

    using Codec = Lucene.Net.Codecs.Codec;

    //using CheckIndex = Lucene.Net.Index.CheckIndex;
    using ConcurrentMergeScheduler = Lucene.Net.Index.ConcurrentMergeScheduler;
    using Directory = Lucene.Net.Store.Directory;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using Document = Documents.Document;
    using DocValuesFormat = Lucene.Net.Codecs.DocValuesFormat;
    using DocValuesType_e = Lucene.Net.Index.FieldInfo.DocValuesType_e;
    using DoubleField = DoubleField;
    using Field = Field;
    using FieldDoc = Lucene.Net.Search.FieldDoc;
    using FieldType = FieldType;
    using FilteredQuery = Lucene.Net.Search.FilteredQuery;
    using FloatField = FloatField;
    using IndexableField = Lucene.Net.Index.IndexableField;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IntField = IntField;
    using LogMergePolicy = Lucene.Net.Index.LogMergePolicy;
    using LongField = LongField;
    using Lucene46Codec = Lucene.Net.Codecs.Lucene46.Lucene46Codec;
    using MergePolicy = Lucene.Net.Index.MergePolicy;
    using MergeScheduler = Lucene.Net.Index.MergeScheduler;
    using MultiFields = Lucene.Net.Index.MultiFields;
    using NumericDocValuesField = NumericDocValuesField;
    using NumericType = FieldType.NumericType;
    using PerFieldDocValuesFormat = Lucene.Net.Codecs.Perfield.PerFieldDocValuesFormat;
    using PerFieldPostingsFormat = Lucene.Net.Codecs.Perfield.PerFieldPostingsFormat;
    using PostingsFormat = Lucene.Net.Codecs.PostingsFormat;
    using ScoreDoc = Lucene.Net.Search.ScoreDoc;
    using SortedDocValuesField = SortedDocValuesField;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TieredMergePolicy = Lucene.Net.Index.TieredMergePolicy;
    using TopDocs = Lucene.Net.Search.TopDocs;

    /// <summary>
    /// General utility methods for Lucene unit tests.
    /// </summary>
    public static class TestUtil
    {
        private static HashSet<FileSystemInfo> Rm(HashSet<FileSystemInfo> unremoved, params DirectoryInfo[] locations)
        {
            foreach (DirectoryInfo location in locations)
            {
                if (location.Exists)
                {
                    // Try to delete all of the files in the directory
                    Rm(unremoved, location.GetFiles());

                    //Delete will throw if not empty when deleted
                    try
                    {
                        location.Delete();
                    }
                    catch (Exception)
                    {
                        unremoved.Add(location);
                    }
                }
            }
            return unremoved;
        }

        private static HashSet<FileSystemInfo> Rm(HashSet<FileSystemInfo> unremoved, params FileInfo[] locations)
        {
            foreach (FileInfo file in locations)
            {
                if (file.Exists)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception delFailed)
                    {
                        Console.WriteLine(delFailed.Message);
                        unremoved.Add(file);
                    }
                }
            }

            return unremoved;
        }

        public static void SyncConcurrentMerges(IndexWriter writer)
        {
            SyncConcurrentMerges(writer.Config.MergeScheduler);
        }

        public static void SyncConcurrentMerges(IMergeScheduler ms)
        {
            if (ms is IConcurrentMergeScheduler)
            {
                ((IConcurrentMergeScheduler)ms).Sync();
            }
        }

        /// <summary>
        /// this runs the CheckIndex tool on the index in.  If any
        ///  issues are hit, a RuntimeException is thrown; else,
        ///  true is returned.
        /// </summary>
        public static CheckIndex.Status CheckIndex(Directory dir)
        {
            return CheckIndex(dir, true);
        }

        public static CheckIndex.Status CheckIndex(Directory dir, bool crossCheckTermVectors)
        {
            ByteArrayOutputStream bos = new ByteArrayOutputStream(1024);
            CheckIndex checker = new CheckIndex(dir);
            checker.CrossCheckTermVectors = crossCheckTermVectors;
            checker.InfoStream = new StreamWriter(bos, Encoding.UTF8);
            CheckIndex.Status indexStatus = checker.DoCheckIndex(null);
            if (indexStatus == null || indexStatus.Clean == false)
            {
                Console.WriteLine("CheckIndex failed");
                checker.FlushInfoStream();
                Console.WriteLine(bos.ToString());
                throw new Exception("CheckIndex failed");
            }
            else
            {
                if (LuceneTestCase.INFOSTREAM)
                {
                    checker.FlushInfoStream(); 
                    Console.WriteLine(bos.ToString());
                }
                return indexStatus;
            }
        }

        /// <summary>
        /// this runs the CheckIndex tool on the Reader.  If any
        ///  issues are hit, a RuntimeException is thrown
        /// </summary>
        public static void CheckReader(IndexReader reader)
        {
            foreach (AtomicReaderContext context in reader.Leaves)
            {
                CheckReader(context.AtomicReader, true);
            }
        }

        public static void CheckReader(AtomicReader reader, bool crossCheckTermVectors)
        {
            ByteArrayOutputStream bos = new ByteArrayOutputStream(1024);
            StreamWriter infoStream = new StreamWriter(bos, Encoding.UTF8);

            reader.CheckIntegrity();
            CheckIndex.Status.FieldNormStatus fieldNormStatus = Index.CheckIndex.TestFieldNorms(reader, infoStream);
            CheckIndex.Status.TermIndexStatus termIndexStatus = Index.CheckIndex.TestPostings(reader, infoStream);
            CheckIndex.Status.StoredFieldStatus storedFieldStatus = Index.CheckIndex.TestStoredFields(reader, infoStream);
            CheckIndex.Status.TermVectorStatus termVectorStatus = Index.CheckIndex.TestTermVectors(reader, infoStream, false, crossCheckTermVectors);
            CheckIndex.Status.DocValuesStatus docValuesStatus = Index.CheckIndex.TestDocValues(reader, infoStream);

            if (fieldNormStatus.Error != null || termIndexStatus.Error != null || storedFieldStatus.Error != null || termVectorStatus.Error != null || docValuesStatus.Error != null)
            {
                Console.WriteLine("CheckReader failed");
                infoStream.Flush();
                Console.WriteLine(bos.ToString());
                throw new Exception("CheckReader failed");
            }
            else
            {
                if (LuceneTestCase.INFOSTREAM)
                {
                    Console.WriteLine(bos.ToString());
                }
            }
        }

        /// <summary>
        /// start and end are BOTH inclusive </summary>
        public static int NextInt(Random r, int start, int end)
        {
            return RandomInts.NextIntBetween(r, start, end);
        }

        /// <summary>
        /// start and end are BOTH inclusive </summary>
        public static long NextLong(Random r, long start, long end)
        {
            Assert.True(end >= start);
            BigInteger range = (BigInteger)end + (BigInteger)1 - (BigInteger)start;
            if (range.CompareTo((BigInteger)int.MaxValue) <= 0)
            {
                return start + r.Next((int)range);
            }
            else
            {
                // probably not evenly distributed when range is large, but OK for tests
                BigInteger augend = new BigInteger(end + 1 - start) * (BigInteger)(r.NextDouble());
                long result = start + (long)augend;
                Assert.True(result >= start);
                Assert.True(result <= end);
                return result;
            }
        }

        public static string RandomSimpleString(Random r, int maxLength)
        {
            return RandomSimpleString(r, 0, maxLength);
        }

        public static string RandomSimpleString(Random r, int minLength, int maxLength)
        {
            int end = NextInt(r, minLength, maxLength);
            if (end == 0)
            {
                // allow 0 length
                return "";
            }
            char[] buffer = new char[end];
            for (int i = 0; i < end; i++)
            {
                buffer[i] = (char)TestUtil.NextInt(r, 'a', 'z');
            }
            return new string(buffer, 0, end);
        }

        public static string RandomSimpleStringRange(Random r, char minChar, char maxChar, int maxLength)
        {
            int end = NextInt(r, 0, maxLength);
            if (end == 0)
            {
                // allow 0 length
                return "";
            }
            char[] buffer = new char[end];
            for (int i = 0; i < end; i++)
            {
                buffer[i] = (char)TestUtil.NextInt(r, minChar, maxChar);
            }
            return new string(buffer, 0, end);
        }

        public static string RandomSimpleString(Random r)
        {
            return RandomSimpleString(r, 0, 20);
        }

        /// <summary>
        /// Returns random string, including full unicode range. </summary>
        public static string RandomUnicodeString(Random r)
        {
            return RandomUnicodeString(r, 20);
        }

        /// <summary>
        /// Returns a random string up to a certain length.
        /// </summary>
        public static string RandomUnicodeString(Random r, int maxLength)
        {
            int end = NextInt(r, 0, maxLength);
            if (end == 0)
            {
                // allow 0 length
                return "";
            }
            char[] buffer = new char[end];
            RandomFixedLengthUnicodeString(r, buffer, 0, buffer.Length);
            return new string(buffer, 0, end);
        }

        /// <summary>
        /// Fills provided char[] with valid random unicode code
        /// unit sequence.
        /// </summary>
        public static void RandomFixedLengthUnicodeString(Random random, char[] chars, int offset, int length)
        {
            int i = offset;
            int end = offset + length;
            while (i < end)
            {
                int t = random.Next(5);
                if (0 == t && i < length - 1)
                {
                    // Make a surrogate pair
                    // High surrogate
                    chars[i++] = (char)NextInt(random, 0xd800, 0xdbff);
                    // Low surrogate
                    chars[i++] = (char)NextInt(random, 0xdc00, 0xdfff);
                }
                else if (t <= 1)
                {
                    chars[i++] = (char)random.Next(0x80);
                }
                else if (2 == t)
                {
                    chars[i++] = (char)NextInt(random, 0x80, 0x7ff);
                }
                else if (3 == t)
                {
                    chars[i++] = (char)NextInt(random, 0x800, 0xd7ff);
                }
                else if (4 == t)
                {
                    chars[i++] = (char)NextInt(random, 0xe000, 0xffff);
                }
            }
        }

        /// <summary>
        /// Returns a String thats "regexpish" (contains lots of operators typically found in regular expressions)
        /// If you call this enough times, you might get a valid regex!
        /// </summary>
        public static string RandomRegexpishString(Random r)
        {
            return RandomRegexpishString(r, 20);
        }

        /// <summary>
        /// Maximum recursion bound for '+' and '*' replacements in
        /// <seealso cref="#randomRegexpishString(Random, int)"/>.
        /// </summary>
        private const int MaxRecursionBound = 5;

        /// <summary>
        /// Operators for <seealso cref="#randomRegexpishString(Random, int)"/>.
        /// </summary>
        private static readonly IList<string> Ops = Arrays.AsList(".", "?", "{0," + MaxRecursionBound + "}", "{1," + MaxRecursionBound + "}", "(", ")", "-", "[", "]", "|"); // bounded replacement for '+' -  bounded replacement for '*'

        /// <summary>
        /// Returns a String thats "regexpish" (contains lots of operators typically found in regular expressions)
        /// If you call this enough times, you might get a valid regex!
        ///
        /// <P>Note: to avoid practically endless backtracking patterns we replace asterisk and plus
        /// operators with bounded repetitions. See LUCENE-4111 for more info.
        /// </summary>
        /// <param name="maxLength"> A hint about maximum length of the regexpish string. It may be exceeded by a few characters. </param>
        public static string RandomRegexpishString(Random r, int maxLength)
        {
            StringBuilder regexp = new StringBuilder(maxLength);
            for (int i = NextInt(r, 0, maxLength); i > 0; i--)
            {
                if (r.NextBoolean())
                {
                    regexp.Append((char)RandomInts.NextIntBetween(r, 'a', 'z'));
                }
                else
                {
                    regexp.Append(RandomInts.RandomFrom(r, Ops));
                }
            }
            return regexp.ToString();
        }

        private static readonly string[] HTML_CHAR_ENTITIES = new string[]
        {
            "AElig", "Aacute", "Acirc", "Agrave", "Alpha", "AMP", "Aring", "Atilde", "Auml", "Beta", "COPY", "Ccedil", "Chi",
            "Dagger", "Delta", "ETH", "Eacute", "Ecirc", "Egrave", "Epsilon", "Eta", "Euml", "Gamma", "GT", "Iacute", "Icirc",
            "Igrave", "Iota", "Iuml", "Kappa", "Lambda", "LT", "Mu", "Ntilde", "Nu", "OElig", "Oacute", "Ocirc", "Ograve",
            "Omega", "Omicron", "Oslash", "Otilde", "Ouml", "Phi", "Pi", "Prime", "Psi", "QUOT", "REG", "Rho", "Scaron",
            "Sigma", "THORN", "Tau", "Theta", "Uacute", "Ucirc", "Ugrave", "Upsilon", "Uuml", "Xi", "Yacute", "Yuml", "Zeta",
            "aacute", "acirc", "acute", "aelig", "agrave", "alefsym", "alpha", "amp", "and", "ang", "apos", "aring", "asymp",
            "atilde", "auml", "bdquo", "beta", "brvbar", "bull", "cap", "ccedil", "cedil", "cent", "chi", "circ", "clubs",
            "cong", "copy", "crarr", "cup", "curren", "dArr", "dagger", "darr", "deg", "delta", "diams", "divide", "eacute",
            "ecirc", "egrave", "empty", "emsp", "ensp", "epsilon", "equiv", "eta", "eth", "euml", "euro", "exist", "fnof",
            "forall", "frac12", "frac14", "frac34", "frasl", "gamma", "ge", "gt", "hArr", "harr", "hearts", "hellip", "iacute",
            "icirc", "iexcl", "igrave", "image", "infin", "int", "iota", "iquest", "isin", "iuml", "kappa", "lArr", "lambda",
            "lang", "laquo", "larr", "lceil", "ldquo", "le", "lfloor", "lowast", "loz", "lrm", "lsaquo", "lsquo", "lt", "macr",
            "mdash", "micro", "middot", "minus", "mu", "nabla", "nbsp", "ndash", "ne", "ni", "not", "notin", "nsub", "ntilde",
            "nu", "oacute", "ocirc", "oelig", "ograve", "oline", "omega", "omicron", "oplus", "or", "ordf", "ordm", "oslash",
            "otilde", "otimes", "ouml", "para", "part", "permil", "perp", "phi", "pi", "piv", "plusmn", "pound", "prime", "prod",
            "prop", "psi", "quot", "rArr", "radic", "rang", "raquo", "rarr", "rceil", "rdquo", "real", "reg", "rfloor", "rho",
            "rlm", "rsaquo", "rsquo", "sbquo", "scaron", "sdot", "sect", "shy", "sigma", "sigmaf", "sim", "spades", "sub", "sube",
            "sum", "sup", "sup1", "sup2", "sup3", "supe", "szlig", "tau", "there4", "theta", "thetasym", "thinsp", "thorn", "tilde",
            "times", "trade", "uArr", "uacute", "uarr", "ucirc", "ugrave", "uml", "upsih", "upsilon", "uuml", "weierp", "xi",
            "yacute", "yen", "yuml", "zeta", "zwj", "zwnj"
        };

        public static string RandomHtmlishString(Random random, int numElements)
        {
            int end = NextInt(random, 0, numElements);
            if (end == 0)
            {
                // allow 0 length
                return "";
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < end; i++)
            {
                int val = random.Next(25);
                switch (val)
                {
                    case 0:
                        sb.Append("<p>");
                        break;

                    case 1:
                        {
                            sb.Append("<");
                            sb.Append("    ".Substring(NextInt(random, 0, 4)));
                            sb.Append(RandomSimpleString(random));
                            for (int j = 0; j < NextInt(random, 0, 10); ++j)
                            {
                                sb.Append(' ');
                                sb.Append(RandomSimpleString(random));
                                sb.Append(" ".Substring(NextInt(random, 0, 1)));
                                sb.Append('=');
                                sb.Append(" ".Substring(NextInt(random, 0, 1)));
                                sb.Append("\"".Substring(NextInt(random, 0, 1)));
                                sb.Append(RandomSimpleString(random));
                                sb.Append("\"".Substring(NextInt(random, 0, 1)));
                            }
                            sb.Append("    ".Substring(NextInt(random, 0, 4)));
                            sb.Append("/".Substring(NextInt(random, 0, 1)));
                            sb.Append(">".Substring(NextInt(random, 0, 1)));
                            break;
                        }
                    case 2:
                        {
                            sb.Append("</");
                            sb.Append("    ".Substring(NextInt(random, 0, 4)));
                            sb.Append(RandomSimpleString(random));
                            sb.Append("    ".Substring(NextInt(random, 0, 4)));
                            sb.Append(">".Substring(NextInt(random, 0, 1)));
                            break;
                        }
                    case 3:
                        sb.Append(">");
                        break;

                    case 4:
                        sb.Append("</p>");
                        break;

                    case 5:
                        sb.Append("<!--");
                        break;

                    case 6:
                        sb.Append("<!--#");
                        break;

                    case 7:
                        sb.Append("<script><!-- f('");
                        break;

                    case 8:
                        sb.Append("</script>");
                        break;

                    case 9:
                        sb.Append("<?");
                        break;

                    case 10:
                        sb.Append("?>");
                        break;

                    case 11:
                        sb.Append("\"");
                        break;

                    case 12:
                        sb.Append("\\\"");
                        break;

                    case 13:
                        sb.Append("'");
                        break;

                    case 14:
                        sb.Append("\\'");
                        break;

                    case 15:
                        sb.Append("-->");
                        break;

                    case 16:
                        {
                            sb.Append("&");
                            switch (NextInt(random, 0, 2))
                            {
                                case 0:
                                    sb.Append(RandomSimpleString(random));
                                    break;

                                case 1:
                                    sb.Append(HTML_CHAR_ENTITIES[random.Next(HTML_CHAR_ENTITIES.Length)]);
                                    break;
                            }
                            sb.Append(";".Substring(NextInt(random, 0, 1)));
                            break;
                        }
                    case 17:
                        {
                            sb.Append("&#");
                            if (0 == NextInt(random, 0, 1))
                            {
                                sb.Append(NextInt(random, 0, int.MaxValue - 1));
                                sb.Append(";".Substring(NextInt(random, 0, 1)));
                            }
                            break;
                        }
                    case 18:
                        {
                            sb.Append("&#x");
                            if (0 == NextInt(random, 0, 1))
                            {
                                sb.Append(Convert.ToString(NextInt(random, 0, int.MaxValue - 1), 16));
                                sb.Append(";".Substring(NextInt(random, 0, 1)));
                            }
                            break;
                        }

                    case 19:
                        sb.Append(";");
                        break;

                    case 20:
                        sb.Append(NextInt(random, 0, int.MaxValue - 1));
                        break;

                    case 21:
                        sb.Append("\n");
                        break;

                    case 22:
                        sb.Append("          ".Substring(NextInt(random, 0, 10)));
                        break;

                    case 23:
                        {
                            sb.Append("<");
                            if (0 == NextInt(random, 0, 3))
                            {
                                sb.Append("          ".Substring(NextInt(random, 1, 10)));
                            }
                            if (0 == NextInt(random, 0, 1))
                            {
                                sb.Append("/");
                                if (0 == NextInt(random, 0, 3))
                                {
                                    sb.Append("          ".Substring(NextInt(random, 1, 10)));
                                }
                            }
                            switch (NextInt(random, 0, 3))
                            {
                                case 0:
                                    sb.Append(RandomlyRecaseString(random, "script"));
                                    break;

                                case 1:
                                    sb.Append(RandomlyRecaseString(random, "style"));
                                    break;

                                case 2:
                                    sb.Append(RandomlyRecaseString(random, "br"));
                                    break;
                                // default: append nothing
                            }
                            sb.Append(">".Substring(NextInt(random, 0, 1)));
                            break;
                        }
                    default:
                        sb.Append(RandomSimpleString(random));
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Randomly upcases, downcases, or leaves intact each code point in the given string
        /// </summary>
        public static string RandomlyRecaseString(Random random, string str)
        {
            var builder = new StringBuilder();
            int pos = 0;
            while (pos < str.Length)
            {
                string toRecase;

                // check if the next char sequence is a surrogate pair
                if (pos + 1 < str.Length && char.IsSurrogatePair(str[pos], str[pos+1]))
                {
                    toRecase = str.Substring(pos, 2);
                }
                else
                {
                    toRecase = str.Substring(pos, 1);
                }

                pos += toRecase.Length;

                switch (NextInt(random, 0, 2))
                {
                    case 0:
                        builder.Append(toRecase.ToUpper());
                        break;
                    case 1:
                        builder.Append(toRecase.ToLower());
                        break;
                    case 2:
                        builder.Append(toRecase);
                        break;
                }
            }
            return builder.ToString();
        }

        private static readonly int[] BlockStarts = new int[]
        {
            0x0000, 0x0080, 0x0100, 0x0180, 0x0250, 0x02B0, 0x0300, 0x0370, 0x0400, 0x0500, 0x0530, 0x0590, 0x0600, 0x0700,
            0x0750, 0x0780, 0x07C0, 0x0800, 0x0900, 0x0980, 0x0A00, 0x0A80, 0x0B00, 0x0B80, 0x0C00, 0x0C80, 0x0D00, 0x0D80,
            0x0E00, 0x0E80, 0x0F00, 0x1000, 0x10A0, 0x1100, 0x1200, 0x1380, 0x13A0, 0x1400, 0x1680, 0x16A0, 0x1700, 0x1720,
            0x1740, 0x1760, 0x1780, 0x1800, 0x18B0, 0x1900, 0x1950, 0x1980, 0x19E0, 0x1A00, 0x1A20, 0x1B00, 0x1B80, 0x1C00,
            0x1C50, 0x1CD0, 0x1D00, 0x1D80, 0x1DC0, 0x1E00, 0x1F00, 0x2000, 0x2070, 0x20A0, 0x20D0, 0x2100, 0x2150, 0x2190,
            0x2200, 0x2300, 0x2400, 0x2440, 0x2460, 0x2500, 0x2580, 0x25A0, 0x2600, 0x2700, 0x27C0, 0x27F0, 0x2800, 0x2900,
            0x2980, 0x2A00, 0x2B00, 0x2C00, 0x2C60, 0x2C80, 0x2D00, 0x2D30, 0x2D80, 0x2DE0, 0x2E00, 0x2E80, 0x2F00, 0x2FF0,
            0x3000, 0x3040, 0x30A0, 0x3100, 0x3130, 0x3190, 0x31A0, 0x31C0, 0x31F0, 0x3200, 0x3300, 0x3400, 0x4DC0, 0x4E00,
            0xA000, 0xA490, 0xA4D0, 0xA500, 0xA640, 0xA6A0, 0xA700, 0xA720, 0xA800, 0xA830, 0xA840, 0xA880, 0xA8E0, 0xA900,
            0xA930, 0xA960, 0xA980, 0xAA00, 0xAA60, 0xAA80, 0xABC0, 0xAC00, 0xD7B0, 0xE000, 0xF900, 0xFB00, 0xFB50, 0xFE00,
            0xFE10, 0xFE20, 0xFE30, 0xFE50, 0xFE70, 0xFF00, 0xFFF0, 0x10000, 0x10080, 0x10100, 0x10140, 0x10190, 0x101D0,
            0x10280, 0x102A0, 0x10300, 0x10330, 0x10380, 0x103A0, 0x10400, 0x10450, 0x10480, 0x10800, 0x10840, 0x10900,
            0x10920, 0x10A00, 0x10A60, 0x10B00, 0x10B40, 0x10B60, 0x10C00, 0x10E60, 0x11080, 0x12000, 0x12400, 0x13000,
            0x1D000, 0x1D100, 0x1D200, 0x1D300, 0x1D360, 0x1D400, 0x1F000, 0x1F030, 0x1F100, 0x1F200, 0x20000, 0x2A700,
            0x2F800, 0xE0000, 0xE0100, 0xF0000, 0x100000
        };

        private static readonly int[] BlockEnds = new int[]
        {
            0x007F, 0x00FF, 0x017F, 0x024F, 0x02AF, 0x02FF, 0x036F, 0x03FF, 0x04FF, 0x052F, 0x058F, 0x05FF, 0x06FF, 0x074F,
            0x077F, 0x07BF, 0x07FF, 0x083F, 0x097F, 0x09FF, 0x0A7F, 0x0AFF, 0x0B7F, 0x0BFF, 0x0C7F, 0x0CFF, 0x0D7F, 0x0DFF,
            0x0E7F, 0x0EFF, 0x0FFF, 0x109F, 0x10FF, 0x11FF, 0x137F, 0x139F, 0x13FF, 0x167F, 0x169F, 0x16FF, 0x171F, 0x173F,
            0x175F, 0x177F, 0x17FF, 0x18AF, 0x18FF, 0x194F, 0x197F, 0x19DF, 0x19FF, 0x1A1F, 0x1AAF, 0x1B7F, 0x1BBF, 0x1C4F,
            0x1C7F, 0x1CFF, 0x1D7F, 0x1DBF, 0x1DFF, 0x1EFF, 0x1FFF, 0x206F, 0x209F, 0x20CF, 0x20FF, 0x214F, 0x218F, 0x21FF,
            0x22FF, 0x23FF, 0x243F, 0x245F, 0x24FF, 0x257F, 0x259F, 0x25FF, 0x26FF, 0x27BF, 0x27EF, 0x27FF, 0x28FF, 0x297F,
            0x29FF, 0x2AFF, 0x2BFF, 0x2C5F, 0x2C7F, 0x2CFF, 0x2D2F, 0x2D7F, 0x2DDF, 0x2DFF, 0x2E7F, 0x2EFF, 0x2FDF, 0x2FFF,
            0x303F, 0x309F, 0x30FF, 0x312F, 0x318F, 0x319F, 0x31BF, 0x31EF, 0x31FF, 0x32FF, 0x33FF, 0x4DBF, 0x4DFF, 0x9FFF,
            0xA48F, 0xA4CF, 0xA4FF, 0xA63F, 0xA69F, 0xA6FF, 0xA71F, 0xA7FF, 0xA82F, 0xA83F, 0xA87F, 0xA8DF, 0xA8FF, 0xA92F,
            0xA95F, 0xA97F, 0xA9DF, 0xAA5F, 0xAA7F, 0xAADF, 0xABFF, 0xD7AF, 0xD7FF, 0xF8FF, 0xFAFF, 0xFB4F, 0xFDFF, 0xFE0F,
            0xFE1F, 0xFE2F, 0xFE4F, 0xFE6F, 0xFEFF, 0xFFEF, 0xFFFF, 0x1007F, 0x100FF, 0x1013F, 0x1018F, 0x101CF, 0x101FF,
            0x1029F, 0x102DF, 0x1032F, 0x1034F, 0x1039F, 0x103DF, 0x1044F, 0x1047F, 0x104AF, 0x1083F, 0x1085F, 0x1091F,
            0x1093F, 0x10A5F, 0x10A7F, 0x10B3F, 0x10B5F, 0x10B7F, 0x10C4F, 0x10E7F, 0x110CF, 0x123FF, 0x1247F, 0x1342F,
            0x1D0FF, 0x1D1FF, 0x1D24F, 0x1D35F, 0x1D37F, 0x1D7FF, 0x1F02F, 0x1F09F, 0x1F1FF, 0x1F2FF, 0x2A6DF, 0x2B73F,
            0x2FA1F, 0xE007F, 0xE01EF, 0xFFFFF, 0x10FFFF
        };

        /// <summary>
        /// Returns random string of length between 0-20 codepoints, all codepoints within the same unicode block. </summary>
        public static string RandomRealisticUnicodeString(Random r)
        {
            return RandomRealisticUnicodeString(r, 20);
        }

        /// <summary>
        /// Returns random string of length up to maxLength codepoints , all codepoints within the same unicode block. </summary>
        public static string RandomRealisticUnicodeString(Random r, int maxLength)
        {
            return RandomRealisticUnicodeString(r, 0, maxLength);
        }

        /// <summary>
        /// Returns random string of length between min and max codepoints, all codepoints within the same unicode block. </summary>
        public static string RandomRealisticUnicodeString(Random r, int minLength, int maxLength)
        {
            int end = NextInt(r, minLength, maxLength);
            int block = r.Next(BlockStarts.Length);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < end; i++)
            {
                sb.Append(NextInt(r, BlockStarts[block], BlockEnds[block]));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns random string, with a given UTF-8 byte length </summary>
        public static string RandomFixedByteLengthUnicodeString(Random r, int length)
        {
            char[] buffer = new char[length * 3];
            int bytes = length;
            int i = 0;
            for (; i < buffer.Length && bytes != 0; i++)
            {
                int t;
                if (bytes >= 4)
                {
                    t = r.Next(5);
                }
                else if (bytes >= 3)
                {
                    t = r.Next(4);
                }
                else if (bytes >= 2)
                {
                    t = r.Next(2);
                }
                else
                {
                    t = 0;
                }
                if (t == 0)
                {
                    buffer[i] = (char)r.Next(0x80);
                    bytes--;
                }
                else if (1 == t)
                {
                    buffer[i] = (char)NextInt(r, 0x80, 0x7ff);
                    bytes -= 2;
                }
                else if (2 == t)
                {
                    buffer[i] = (char)NextInt(r, 0x800, 0xd7ff);
                    bytes -= 3;
                }
                else if (3 == t)
                {
                    buffer[i] = (char)NextInt(r, 0xe000, 0xffff);
                    bytes -= 3;
                }
                else if (4 == t)
                {
                    // Make a surrogate pair
                    // High surrogate
                    buffer[i++] = (char)NextInt(r, 0xd800, 0xdbff);
                    // Low surrogate
                    buffer[i] = (char)NextInt(r, 0xdc00, 0xdfff);
                    bytes -= 4;
                }
            }
            return new string(buffer, 0, i);
        }

        /// <summary>
        /// Return a Codec that can read any of the
        ///  default codecs and formats, but always writes in the specified
        ///  format.
        /// </summary>
        public static Codec AlwaysPostingsFormat(PostingsFormat format)
        {
            // TODO: we really need for postings impls etc to announce themselves
            // (and maybe their params, too) to infostream on flush and merge.
            // otherwise in a real debugging situation we won't know whats going on!
            if (LuceneTestCase.VERBOSE)
            {
                Console.WriteLine("forcing postings format to:" + format);
            }
            return new Lucene46CodecAnonymousInnerClassHelper(format);
        }

        private class Lucene46CodecAnonymousInnerClassHelper : Lucene46Codec
        {
            private PostingsFormat Format;

            public Lucene46CodecAnonymousInnerClassHelper(PostingsFormat format)
            {
                this.Format = format;
            }

            public override PostingsFormat GetPostingsFormatForField(string field)
            {
                return Format;
            }
        }

        /// <summary>
        /// Return a Codec that can read any of the
        ///  default codecs and formats, but always writes in the specified
        ///  format.
        /// </summary>
        public static Codec AlwaysDocValuesFormat(DocValuesFormat format)
        {
            // TODO: we really need for docvalues impls etc to announce themselves
            // (and maybe their params, too) to infostream on flush and merge.
            // otherwise in a real debugging situation we won't know whats going on!
            if (LuceneTestCase.VERBOSE)
            {
                Console.WriteLine("forcing docvalues format to:" + format);
            }
            return new Lucene46CodecAnonymousInnerClassHelper2(format);
        }

        private class Lucene46CodecAnonymousInnerClassHelper2 : Lucene46Codec
        {
            private DocValuesFormat Format;

            public Lucene46CodecAnonymousInnerClassHelper2(DocValuesFormat format)
            {
                this.Format = format;
            }

            public override DocValuesFormat GetDocValuesFormatForField(string field)
            {
                return Format;
            }
        }

        // TODO: generalize all 'test-checks-for-crazy-codecs' to
        // annotations (LUCENE-3489)
        public static string GetPostingsFormat(string field)
        {
            return GetPostingsFormat(Codec.Default, field);
        }

        public static string GetPostingsFormat(Codec codec, string field)
        {
            PostingsFormat p = codec.PostingsFormat();
            if (p is PerFieldPostingsFormat)
            {
                return ((PerFieldPostingsFormat)p).GetPostingsFormatForField(field).Name;
            }
            else
            {
                return p.Name;
            }
        }

        public static string GetDocValuesFormat(string field)
        {
            return GetDocValuesFormat(Codec.Default, field);
        }

        public static string GetDocValuesFormat(Codec codec, string field)
        {
            DocValuesFormat f = codec.DocValuesFormat();
            if (f is PerFieldDocValuesFormat)
            {
                return ((PerFieldDocValuesFormat)f).GetDocValuesFormatForField(field).Name;
            }
            else
            {
                return f.Name;
            }
        }

        // TODO: remove this, push this test to Lucene40/Lucene42 codec tests
        public static bool FieldSupportsHugeBinaryDocValues(string field)
        {
            string dvFormat = GetDocValuesFormat(field);
            if (dvFormat.Equals("Lucene40") || dvFormat.Equals("Lucene42") || dvFormat.Equals("Memory"))
            {
                return false;
            }
            return true;
        }

        public static bool AnyFilesExceptWriteLock(Directory dir)
        {
            string[] files = dir.ListAll();
            if (files.Length > 1 || (files.Length == 1 && !files[0].Equals("write.lock")))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// just tries to configure things to keep the open file
        /// count lowish
        /// </summary>
        public static void ReduceOpenFiles(IndexWriter w)
        {
            // keep number of open files lowish
            MergePolicy mp = w.Config.MergePolicy;
            if (mp is LogMergePolicy)
            {
                LogMergePolicy lmp = (LogMergePolicy)mp;
                lmp.MergeFactor = Math.Min(5, lmp.MergeFactor);
                lmp.NoCFSRatio = 1.0;
            }
            else if (mp is TieredMergePolicy)
            {
                TieredMergePolicy tmp = (TieredMergePolicy)mp;
                tmp.MaxMergeAtOnce = Math.Min(5, tmp.MaxMergeAtOnce);
                tmp.SegmentsPerTier = Math.Min(5, tmp.SegmentsPerTier);
                tmp.NoCFSRatio = 1.0;
            }
            IMergeScheduler ms = w.Config.MergeScheduler;
            if (ms is IConcurrentMergeScheduler)
            {
                // wtf... shouldnt it be even lower since its 1 by default?!?!
                ((IConcurrentMergeScheduler)ms).SetMaxMergesAndThreads(3, 2);
            }
        }

        /// <summary>
        /// Checks some basic behaviour of an AttributeImpl </summary>
        /// <param name="att">Attribute to reflect</param>
        /// <param name="reflectedValues"> contains a map with "AttributeClass#key" as values </param>
        public static void AssertAttributeReflection(Attribute att, IDictionary<string, object> reflectedValues)
        {
            IDictionary<string, object> map = new Dictionary<string, object>();
            att.ReflectWith(new AttributeReflectorAnonymousInnerClassHelper(map));
            IDictionary<string, object> newReflectedObjects = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> de in reflectedValues)
                newReflectedObjects.Add(de.Key, (object)de.Value);
            Assert.IsTrue(CollectionsHelper.DictEquals(newReflectedObjects, map), "Reflection does not produce same map");
        }

        private class AttributeReflectorAnonymousInnerClassHelper : IAttributeReflector
        {
            private IDictionary<string, object> Map;

            public AttributeReflectorAnonymousInnerClassHelper(IDictionary<string, object> map)
            {
                this.Map = map;
            }

            public void Reflect<T>(string key, object value)
                where T : IAttribute
            {
                Reflect(typeof(T), key, value);
            }

            public void Reflect(Type attClass, string key, object value)
            {
                Map[attClass.Name + '#' + key] = value;
            }
        }

        public static void AssertEquals(TopDocs expected, TopDocs actual)
        {
            Assert.AreEqual(expected.TotalHits, actual.TotalHits, "wrong total hits");
            Assert.AreEqual(expected.MaxScore, actual.MaxScore, "wrong maxScore");
            Assert.AreEqual(expected.ScoreDocs.Length, actual.ScoreDocs.Length, "wrong hit count");
            for (int hitIDX = 0; hitIDX < expected.ScoreDocs.Length; hitIDX++)
            {
                ScoreDoc expectedSD = expected.ScoreDocs[hitIDX];
                ScoreDoc actualSD = actual.ScoreDocs[hitIDX];
                Assert.AreEqual(expectedSD.Doc, actualSD.Doc, "wrong hit docID");
                Assert.AreEqual(expectedSD.Score, actualSD.Score, "wrong hit score");
                if (expectedSD is FieldDoc)
                {
                    Assert.IsTrue(actualSD is FieldDoc);
                    Assert.AreEqual(((FieldDoc)expectedSD).Fields, ((FieldDoc)actualSD).Fields, "wrong sort field values");
                }
                else
                {
                    Assert.IsFalse(actualSD is FieldDoc);
                }
            }
        }

        // NOTE: this is likely buggy, and cannot clone fields
        // with tokenStreamValues, etc.  Use at your own risk!!

        // TODO: is there a pre-existing way to do this!!!
        public static Document CloneDocument(Document doc1)
        {
            Document doc2 = new Document();
            foreach (IndexableField f in doc1.Fields)
            {
                Field field1 = (Field)f;
                Field field2;
                DocValuesType_e? dvType = field1.FieldType().DocValueType;
                NumericType? numType = field1.FieldType().NumericTypeValue;
                if (dvType != null)
                {
                    switch (dvType)
                    {
                        case DocValuesType_e.NUMERIC:
                            field2 = new NumericDocValuesField(field1.Name(), (long)field1.NumericValue);
                            break;

                        case DocValuesType_e.BINARY:
                            field2 = new BinaryDocValuesField(field1.Name(), field1.BinaryValue());
                            break;

                        case DocValuesType_e.SORTED:
                            field2 = new SortedDocValuesField(field1.Name(), field1.BinaryValue());
                            break;

                        default:
                            throw new InvalidOperationException("unknown Type: " + dvType);
                    }
                }
                else if (numType != null)
                {
                    switch (numType)
                    {
                        case NumericType.INT:
                            field2 = new IntField(field1.Name(), (int)field1.NumericValue, (FieldType)field1.FieldType());
                            break;

                        case NumericType.FLOAT:
                            field2 = new FloatField(field1.Name(), (int)field1.NumericValue, (FieldType)field1.FieldType());
                            break;

                        case NumericType.LONG:
                            field2 = new LongField(field1.Name(), (int)field1.NumericValue, (FieldType)field1.FieldType());
                            break;

                        case NumericType.DOUBLE:
                            field2 = new DoubleField(field1.Name(), (int)field1.NumericValue, (FieldType)field1.FieldType());
                            break;

                        default:
                            throw new InvalidOperationException("unknown Type: " + numType);
                    }
                }
                else
                {
                    field2 = new Field(field1.Name(), field1.StringValue, (FieldType)field1.FieldType());
                }
                doc2.Add(field2);
            }

            return doc2;
        }

        // Returns a DocsEnum, but randomly sometimes uses a
        // DocsAndFreqsEnum, DocsAndPositionsEnum.  Returns null
        // if field/term doesn't exist:
        public static DocsEnum Docs(Random random, IndexReader r, string field, BytesRef term, Bits liveDocs, DocsEnum reuse, int flags)
        {
            Terms terms = MultiFields.GetTerms(r, field);
            if (terms == null)
            {
                return null;
            }
            TermsEnum termsEnum = terms.Iterator(null);
            if (!termsEnum.SeekExact(term))
            {
                return null;
            }
            return Docs(random, termsEnum, liveDocs, reuse, flags);
        }

        // Returns a DocsEnum from a positioned TermsEnum, but
        // randomly sometimes uses a DocsAndFreqsEnum, DocsAndPositionsEnum.
        public static DocsEnum Docs(Random random, TermsEnum termsEnum, Bits liveDocs, DocsEnum reuse, int flags)
        {
            if (random.NextBoolean())
            {
                if (random.NextBoolean())
                {
                    int posFlags;
                    switch (random.Next(4))
                    {
                        case 0:
                            posFlags = 0;
                            break;

                        case 1:
                            posFlags = DocsAndPositionsEnum.FLAG_OFFSETS;
                            break;

                        case 2:
                            posFlags = DocsAndPositionsEnum.FLAG_PAYLOADS;
                            break;

                        default:
                            posFlags = DocsAndPositionsEnum.FLAG_OFFSETS | DocsAndPositionsEnum.FLAG_PAYLOADS;
                            break;
                    }
                    // TODO: cast to DocsAndPositionsEnum?
                    DocsAndPositionsEnum docsAndPositions = termsEnum.DocsAndPositions(liveDocs, null, posFlags);
                    if (docsAndPositions != null)
                    {
                        return docsAndPositions;
                    }
                }
                flags |= DocsEnum.FLAG_FREQS;
            }
            return termsEnum.Docs(liveDocs, reuse, flags);
        }

        public static ICharSequence StringToCharSequence(string @string, Random random)
        {
            return BytesToCharSequence(new BytesRef(@string), random);
        }

        public static ICharSequence BytesToCharSequence(BytesRef @ref, Random random)
        {
            switch (random.Next(5))
            {
                case 4:
                    CharsRef chars = new CharsRef(@ref.Length);
                    UnicodeUtil.UTF8toUTF16(@ref.Bytes, @ref.Offset, @ref.Length, chars);
                    return chars;
                //case 3:
                //return CharBuffer.wrap(@ref.Utf8ToString());
                default:
                    return new StringCharSequenceWrapper(@ref.Utf8ToString());
            }
        }

        /// <summary>
        /// Shutdown <seealso cref="ExecutorService"/> and wait for its.
        /// </summary>
        public static void ShutdownExecutorService(TaskScheduler ex)
        {
            /*if (ex != null)
            {
              try
              {
                ex.shutdown();
                ex.awaitTermination(1, TimeUnit.SECONDS);
              }
              catch (ThreadInterruptedException e)
              {
                // Just report it on the syserr.
                Console.Error.WriteLine("Could not properly shutdown executor service.");
                Console.Error.WriteLine(e.StackTrace);
              }
            }*/
        }

        /// <summary>
        /// Returns a valid (compiling) Pattern instance with random stuff inside. Be careful
        /// when applying random patterns to longer strings as certain types of patterns
        /// may explode into exponential times in backtracking implementations (such as Java's).
        /// </summary>
        /* LUCENE TODO: not called as of now
        public static Pattern RandomPattern(Random random)
        {
          const string nonBmpString = "AB\uD840\uDC00C";
          while (true)
          {
            try
            {
              Pattern p = Pattern.compile(TestUtil.RandomRegexpishString(random));
              string replacement = null;
              // ignore bugs in Sun's regex impl
              try
              {
                replacement = p.matcher(nonBmpString).replaceAll("_");
              }
              catch (IndexOutOfRangeException jdkBug)
              {
                Console.WriteLine("WARNING: your jdk is buggy!");
                Console.WriteLine("Pattern.compile(\"" + p.pattern() + "\").matcher(\"AB\\uD840\\uDC00C\").replaceAll(\"_\"); should not throw IndexOutOfBounds!");
              }
              // Make sure the result of applying the pattern to a string with extended
              // unicode characters is a valid utf16 string. See LUCENE-4078 for discussion.
              if (replacement != null && UnicodeUtil.ValidUTF16String(replacement.ToCharArray()))
              {
                return p;
              }
            }
            catch (PatternSyntaxException ignored)
            {
              // Loop trying until we hit something that compiles.
            }
          }
        }*/

        public static FilteredQuery.FilterStrategy RandomFilterStrategy(Random random)
        {
            switch (random.Next(6))
            {
                case 5:
                case 4:
                    return new RandomAccessFilterStrategyAnonymousInnerClassHelper();

                case 3:
                    return FilteredQuery.RANDOM_ACCESS_FILTER_STRATEGY;

                case 2:
                    return FilteredQuery.LEAP_FROG_FILTER_FIRST_STRATEGY;

                case 1:
                    return FilteredQuery.LEAP_FROG_QUERY_FIRST_STRATEGY;

                case 0:
                    return FilteredQuery.QUERY_FIRST_FILTER_STRATEGY;

                default:
                    return FilteredQuery.RANDOM_ACCESS_FILTER_STRATEGY;
            }
        }

        private class RandomAccessFilterStrategyAnonymousInnerClassHelper : FilteredQuery.RandomAccessFilterStrategy
        {
            public RandomAccessFilterStrategyAnonymousInnerClassHelper()
            {
            }

            protected override bool UseRandomAccess(Bits bits, int firstFilterDoc)
            {
                return LuceneTestCase.Random().NextBoolean();
            }
        }

        /// <summary>
        /// Returns a random string in the specified length range consisting
        /// entirely of whitespace characters </summary>
        /// <seealso cref= #WHITESPACE_CHARACTERS </seealso>
        public static string RandomWhitespace(Random r, int minLength, int maxLength)
        {
            int end = NextInt(r, minLength, maxLength);
            StringBuilder @out = new StringBuilder();
            for (int i = 0; i < end; i++)
            {
                int offset = NextInt(r, 0, WHITESPACE_CHARACTERS.Length - 1);
                char c = WHITESPACE_CHARACTERS[offset];
                // sanity check
                Assert.IsTrue(char.IsWhiteSpace(c), "Not really whitespace? (@" + offset + "): " + c);
                @out.Append(c);
            }
            return @out.ToString();
        }

        public static string RandomAnalysisString(Random random, int maxLength, bool simple)
        {
            Assert.True(maxLength >= 0);

            // sometimes just a purely random string
            if (random.Next(31) == 0)
            {
                return RandomSubString(random, random.Next(maxLength), simple);
            }

            // otherwise, try to make it more realistic with 'words' since most tests use MockTokenizer
            // first decide how big the string will really be: 0..n
            maxLength = random.Next(maxLength);
            int avgWordLength = TestUtil.NextInt(random, 3, 8);
            StringBuilder sb = new StringBuilder();
            while (sb.Length < maxLength)
            {
                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }
                int wordLength = -1;
                while (wordLength < 0)
                {
                    wordLength = (int)(random.NextDouble() * 3 + avgWordLength);
                }
                wordLength = Math.Min(wordLength, maxLength - sb.Length);
                sb.Append(RandomSubString(random, wordLength, simple));
            }
            return sb.ToString();
        }

        public static string RandomSubString(Random random, int wordLength, bool simple)
        {
            if (wordLength == 0)
            {
                return "";
            }

            int evilness = TestUtil.NextInt(random, 0, 20);

            StringBuilder sb = new StringBuilder();
            while (sb.Length < wordLength)
            {
                ;
                if (simple)
                {
                    sb.Append(random.NextBoolean() ? TestUtil.RandomSimpleString(random, wordLength) : TestUtil.RandomHtmlishString(random, wordLength));
                }
                else
                {
                    if (evilness < 10)
                    {
                        sb.Append(TestUtil.RandomSimpleString(random, wordLength));
                    }
                    else if (evilness < 15)
                    {
                        Assert.AreEqual(0, sb.Length); // we should always get wordLength back!
                        sb.Append(TestUtil.RandomRealisticUnicodeString(random, wordLength, wordLength));
                    }
                    else if (evilness == 16)
                    {
                        sb.Append(TestUtil.RandomHtmlishString(random, wordLength));
                    }
                    else if (evilness == 17)
                    {
                        // gives a lot of punctuation
                        sb.Append(TestUtil.RandomRegexpishString(random, wordLength));
                    }
                    else
                    {
                        sb.Append(TestUtil.RandomUnicodeString(random, wordLength));
                    }
                }
            }
            if (sb.Length > wordLength)
            {
                sb.Length = wordLength;
                if (char.IsHighSurrogate(sb[wordLength - 1]))
                {
                    sb.Length = wordLength - 1;
                }
            }

            if (random.Next(17) == 0)
            {
                // mix up case
                string mixedUp = TestUtil.RandomlyRecaseString(random, sb.ToString());
                Assert.True(mixedUp.Length == sb.Length, "Lengths are not the same: mixedUp = " + mixedUp + ", length = " + mixedUp.Length + ", sb = " + sb + ", length = " + sb.Length);
                return mixedUp;
            }
            else
            {
                return sb.ToString();
            }
        }

        /// <summary>
        /// List of characters that match <seealso cref="Character#isWhitespace"/> </summary>
        public static readonly char[] WHITESPACE_CHARACTERS = new char[]
        {
            '\u0009', '\n', '\u000B', '\u000C', '\r', '\u001C', '\u001D', '\u001E', '\u001F', '\u0020', '\u1680', '\u180E', '\u2000',
            '\u2001', '\u2002', '\u2003', '\u2004', '\u2005', '\u2006', '\u2008', '\u2009', '\u200A', '\u2028', '\u2029', '\u205F', '\u3000'
        };

        public static byte[] ToByteArray(this sbyte[] arr)
        {
            var unsigned = new byte[arr.Length];
            System.Buffer.BlockCopy(arr, 0, unsigned, 0, arr.Length);
            return unsigned;
        }
    }
}